using System.CommandLine;
using DataverseSchemaGenerator.Core.Models;
using DataverseSchemaGenerator.Core.Services;

namespace DataverseSchemaGenerator;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var inputOption = new Option<FileInfo?>(
            name: "--input",
            description: "Path to a specific customizations.xml file. If not specified, scans the Input folder for *.xml files.")
        {
            IsRequired = false
        };
        inputOption.AddAlias("-i");

        var outputOption = new Option<DirectoryInfo?>(
            name: "--out",
            description: "Output directory for generated JSON Schema files. Defaults to current directory.")
        {
            IsRequired = false
        };
        outputOption.AddAlias("-o");

        var baseIdOption = new Option<string>(
            name: "--base-id",
            description: "Base URI for the $id property in generated schemas",
            getDefaultValue: () => "https://schemas.example.com/dataverse/")
        {
            IsRequired = false
        };
        baseIdOption.AddAlias("-b");

        var entitiesOption = new Option<string[]>(
            name: "--entities",
            description: "Filter to specific entities (comma-separated logical names). If not specified, all entities are processed.")
        {
            IsRequired = false,
            AllowMultipleArgumentsPerToken = true
        };
        entitiesOption.AddAlias("-e");

        var skipReadApiFilterOption = new Option<bool>(
            name: "--include-non-readable",
            description: "Include attributes where ValidForReadApi = false",
            getDefaultValue: () => false);

        var skipRetrievableFilterOption = new Option<bool>(
            name: "--include-non-retrievable",
            description: "Include attributes where IsRetrievable = false",
            getDefaultValue: () => false);

        var generateEventsOption = new Option<bool>(
            name: "--generate-events",
            description: "Generate EventBus envelope schemas for each entity",
            getDefaultValue: () => false);

        var compactOption = new Option<bool>(
            name: "--compact",
            description: "Output compact JSON (no indentation)",
            getDefaultValue: () => false);

        var batchOption = new Option<bool>(
            name: "--batch",
            description: "Enable batch mode features: add timestamps to output filenames and archive processed files.",
            getDefaultValue: () => false);

        var rootCommand = new RootCommand("Generate JSON Schema documents from Dataverse customizations.xml")
        {
            inputOption,
            outputOption,
            baseIdOption,
            entitiesOption,
            skipReadApiFilterOption,
            skipRetrievableFilterOption,
            generateEventsOption,
            compactOption,
            batchOption
        };

        rootCommand.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForOption(inputOption);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var baseId = context.ParseResult.GetValueForOption(baseIdOption)!;
            var entities = context.ParseResult.GetValueForOption(entitiesOption) ?? [];
            var includeNonReadable = context.ParseResult.GetValueForOption(skipReadApiFilterOption);
            var includeNonRetrievable = context.ParseResult.GetValueForOption(skipRetrievableFilterOption);
            var generateEvents = context.ParseResult.GetValueForOption(generateEventsOption);
            var compact = context.ParseResult.GetValueForOption(compactOption);
            var batchMode = context.ParseResult.GetValueForOption(batchOption);

            var entityFilter = entities.SelectMany(e => e.Split(',', StringSplitOptions.RemoveEmptyEntries)).ToList();

            // Default output to current directory
            var outputPath = output?.FullName ?? Directory.GetCurrentDirectory();

            int exitCode;
            if (input is not null)
            {
                // Single file mode - process the specified file
                var options = new GeneratorOptions
                {
                    InputPath = input.FullName,
                    OutputPath = outputPath,
                    BaseId = baseId,
                    EntityFilter = entityFilter,
                    FilterValidForReadApi = !includeNonReadable,
                    FilterIsRetrievable = !includeNonRetrievable,
                    GenerateEventEnvelopes = generateEvents,
                    PrettyPrint = !compact
                };

                exitCode = await RunGeneratorAsync(options, context.GetCancellationToken());
            }
            else
            {
                // Scan Input folder for XML files
                // --batch enables timestamps and archiving
                exitCode = await RunInputFolderModeAsync(
                    outputPath,
                    baseId,
                    !includeNonReadable,
                    !includeNonRetrievable,
                    entityFilter,
                    generateEvents,
                    !compact,
                    batchMode,
                    context.GetCancellationToken());
            }

            context.ExitCode = exitCode;
        });

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<int> RunInputFolderModeAsync(
        string outputPath,
        string baseId,
        bool filterValidForReadApi,
        bool filterIsRetrievable,
        List<string> entityFilter,
        bool generateEvents,
        bool prettyPrint,
        bool useBatchFeatures,
        CancellationToken cancellationToken)
    {
        try
        {
            var inputFolder = BatchProcessor.GetInputFolderPath();
            Console.WriteLine($"Scanning folder: {inputFolder}");

            // Ensure folders exist
            var wasCreated = BatchProcessor.EnsureInputFolderExists();
            if (wasCreated)
            {
                Console.WriteLine($"Created Input folder: {inputFolder}");
            }

            Directory.CreateDirectory(outputPath);

            var inputFiles = BatchProcessor.GetInputFiles();
            if (inputFiles.Count == 0)
            {
                Console.WriteLine("No XML files found in Input folder. Place customizations.xml files in this folder and run again.");
                return 0;
            }

            Console.WriteLine($"Found {inputFiles.Count} XML file(s) to process.");

            if (useBatchFeatures)
            {
                // Batch mode: use timestamps and archiving
                var processor = new BatchProcessor();
                processor.ProgressChanged += message => Console.WriteLine($"  {message}");

                var result = await processor.ProcessBatchAsync(
                    outputPath,
                    baseId,
                    filterValidForReadApi,
                    filterIsRetrievable,
                    entityFilter.Count > 0 ? entityFilter : null,
                    generateEvents,
                    prettyPrint,
                    cancellationToken: cancellationToken);

                Console.WriteLine();
                Console.WriteLine("Batch processing complete:");
                Console.WriteLine($"  Files processed successfully: {result.TotalFilesProcessed}");
                Console.WriteLine($"  Files failed: {result.TotalFilesFailed}");
                Console.WriteLine($"  Total entity schemas generated: {result.TotalEntitySchemas}");
                if (generateEvents)
                {
                    Console.WriteLine($"  Total event schemas generated: {result.TotalEventSchemas}");
                }
                Console.WriteLine($"  Output directory: {outputPath}");

                return result.TotalFilesFailed > 0 ? 1 : 0;
            }
            else
            {
                // Simple mode: process files without timestamps, archive to Input/Archive
                var service = new SchemaGeneratorService();
                service.ProgressChanged += message => Console.WriteLine($"  {message}");

                int totalEntities = 0;
                int totalEvents = 0;
                int filesProcessed = 0;
                int filesFailed = 0;

                // Prepare archive folder
                var archiveFolder = Path.Combine(inputFolder, "Archive");

                foreach (var inputFile in inputFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fileName = Path.GetFileName(inputFile);
                    Console.WriteLine($"Processing: {fileName}");

                    try
                    {
                        var options = new GeneratorOptions
                        {
                            InputPath = inputFile,
                            OutputPath = outputPath,
                            BaseId = baseId,
                            EntityFilter = entityFilter,
                            FilterValidForReadApi = filterValidForReadApi,
                            FilterIsRetrievable = filterIsRetrievable,
                            GenerateEventEnvelopes = generateEvents,
                            PrettyPrint = prettyPrint
                        };

                        var entities = service.ParseEntities(inputFile);
                        if (entities.Count == 0)
                        {
                            Console.WriteLine($"  No entities found in {fileName}.");
                            filesFailed++;
                            continue;
                        }

                        if (entityFilter.Count > 0)
                        {
                            entities = service.FilterEntities(entities, entityFilter);
                        }

                        var result = await service.GenerateAsync(options, entities, cancellationToken);
                        totalEntities += result.EntitySchemasGenerated;
                        totalEvents += result.EventSchemasGenerated;
                        filesProcessed++;

                        // Archive the processed file
                        Directory.CreateDirectory(archiveFolder);
                        var archivePath = Path.Combine(archiveFolder, fileName);
                        // If file already exists in archive, overwrite it
                        if (File.Exists(archivePath))
                        {
                            File.Delete(archivePath);
                        }
                        File.Move(inputFile, archivePath);
                        Console.WriteLine($"  Archived to: {archivePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Error: {ex.Message}");
                        filesFailed++;
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Processing complete:");
                Console.WriteLine($"  Files processed successfully: {filesProcessed}");
                if (filesFailed > 0)
                {
                    Console.WriteLine($"  Files failed: {filesFailed}");
                }
                Console.WriteLine($"  Total entity schemas generated: {totalEntities}");
                if (generateEvents)
                {
                    Console.WriteLine($"  Total event schemas generated: {totalEvents}");
                }
                Console.WriteLine($"  Output directory: {outputPath}");

                return filesFailed > 0 ? 1 : 0;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Processing cancelled.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunGeneratorAsync(GeneratorOptions options, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"Reading customizations from: {options.InputPath}");

            if (!File.Exists(options.InputPath))
            {
                Console.Error.WriteLine($"Error: Input file not found: {options.InputPath}");
                return 1;
            }

            var service = new SchemaGeneratorService();
            service.ProgressChanged += message => Console.WriteLine($"  {message}");

            // Parse the customizations.xml
            var entities = service.ParseEntities(options.InputPath);

            if (entities.Count == 0)
            {
                Console.WriteLine("No entities found in the customizations file.");
                return 0;
            }

            Console.WriteLine($"Found {entities.Count} entities in customizations file.");

            // Apply entity filter if specified
            if (options.EntityFilter.Count > 0)
            {
                entities = service.FilterEntities(entities, options.EntityFilter);
                Console.WriteLine($"Filtered to {entities.Count} entities based on --entities filter.");
            }

            // Generate schemas
            var result = await service.GenerateAsync(options, entities, cancellationToken);

            Console.WriteLine();
            Console.WriteLine($"Successfully generated {result.EntitySchemasGenerated} entity schemas to: {options.OutputPath}");
            if (options.GenerateEventEnvelopes)
            {
                Console.WriteLine($"Successfully generated {result.EventSchemasGenerated} event envelope schemas to: {Path.Combine(options.OutputPath, "events")}");
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation cancelled.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
