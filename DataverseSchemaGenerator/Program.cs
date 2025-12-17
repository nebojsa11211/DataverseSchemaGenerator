using System.CommandLine;
using DataverseSchemaGenerator.Core.Models;
using DataverseSchemaGenerator.Core.Services;

namespace DataverseSchemaGenerator;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var inputOption = new Option<FileInfo>(
            name: "--input",
            description: "Path to the customizations.xml file from a Dataverse solution export")
        {
            IsRequired = true
        };
        inputOption.AddAlias("-i");

        var outputOption = new Option<DirectoryInfo>(
            name: "--out",
            description: "Output directory for generated JSON Schema files",
            getDefaultValue: () => new DirectoryInfo("./schemas"))
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

        var rootCommand = new RootCommand("Generate JSON Schema documents from Dataverse customizations.xml")
        {
            inputOption,
            outputOption,
            baseIdOption,
            entitiesOption,
            skipReadApiFilterOption,
            skipRetrievableFilterOption,
            generateEventsOption,
            compactOption
        };

        rootCommand.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForOption(inputOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption)!;
            var baseId = context.ParseResult.GetValueForOption(baseIdOption)!;
            var entities = context.ParseResult.GetValueForOption(entitiesOption) ?? [];
            var includeNonReadable = context.ParseResult.GetValueForOption(skipReadApiFilterOption);
            var includeNonRetrievable = context.ParseResult.GetValueForOption(skipRetrievableFilterOption);
            var generateEvents = context.ParseResult.GetValueForOption(generateEventsOption);
            var compact = context.ParseResult.GetValueForOption(compactOption);

            var options = new GeneratorOptions
            {
                InputPath = input.FullName,
                OutputPath = output.FullName,
                BaseId = baseId,
                EntityFilter = entities.SelectMany(e => e.Split(',', StringSplitOptions.RemoveEmptyEntries)).ToList(),
                FilterValidForReadApi = !includeNonReadable,
                FilterIsRetrievable = !includeNonRetrievable,
                GenerateEventEnvelopes = generateEvents,
                PrettyPrint = !compact
            };

            var exitCode = await RunGeneratorAsync(options, context.GetCancellationToken());
            context.ExitCode = exitCode;
        });

        return await rootCommand.InvokeAsync(args);
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
