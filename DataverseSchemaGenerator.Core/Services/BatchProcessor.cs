namespace DataverseSchemaGenerator.Core.Services;

/// <summary>
/// Result of processing a single batch file.
/// </summary>
public sealed class BatchFileResult
{
    public required string SourceFile { get; init; }
    public required string Timestamp { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int EntitySchemasGenerated { get; init; }
    public int EventSchemasGenerated { get; init; }
    public string? ArchivePath { get; init; }
    public string? BadXmlPath { get; init; }
}

/// <summary>
/// Result of batch processing operation.
/// </summary>
public sealed class BatchResult
{
    public List<BatchFileResult> ProcessedFiles { get; init; } = [];
    public int TotalFilesProcessed => ProcessedFiles.Count(f => f.Success);
    public int TotalFilesFailed => ProcessedFiles.Count(f => !f.Success);
    public int TotalEntitySchemas => ProcessedFiles.Sum(f => f.EntitySchemasGenerated);
    public int TotalEventSchemas => ProcessedFiles.Sum(f => f.EventSchemasGenerated);
}

/// <summary>
/// Handles batch processing of XML files from an input folder.
/// </summary>
public sealed class BatchProcessor
{
    private const string InputFolderName = "Input";
    private const string ArchiveFolderName = "Archive";
    private const string BadXmlFolderName = "BadXml";
    private const string TimestampFormat = "ddMMyy_HHmmss";

    private readonly SchemaGeneratorService _generatorService;

    public event Action<string>? ProgressChanged;

    public BatchProcessor()
    {
        _generatorService = new SchemaGeneratorService();
        _generatorService.ProgressChanged += message => ProgressChanged?.Invoke(message);
    }

    /// <summary>
    /// Gets the input folder path relative to the application directory.
    /// </summary>
    public static string GetInputFolderPath(string? basePath = null)
    {
        basePath ??= Directory.GetCurrentDirectory();
        return Path.Combine(basePath, InputFolderName);
    }

    /// <summary>
    /// Ensures the input folder exists, creating it if necessary.
    /// </summary>
    /// <returns>True if the folder was created, false if it already existed.</returns>
    public static bool EnsureInputFolderExists(string? basePath = null)
    {
        var inputPath = GetInputFolderPath(basePath);
        if (Directory.Exists(inputPath))
        {
            return false;
        }

        Directory.CreateDirectory(inputPath);
        return true;
    }

    /// <summary>
    /// Gets all XML files from the input folder. Creates the folder if it doesn't exist.
    /// </summary>
    public static IReadOnlyList<string> GetInputFiles(string? basePath = null)
    {
        var inputPath = GetInputFolderPath(basePath);

        // Create Input folder if it doesn't exist
        Directory.CreateDirectory(inputPath);

        return Directory.GetFiles(inputPath, "*.xml", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToList();
    }

    /// <summary>
    /// Generates a timestamp string in the required format (ddMMyy_HHmmss).
    /// </summary>
    public static string GenerateTimestamp(DateTime? dateTime = null)
    {
        return (dateTime ?? DateTime.Now).ToString(TimestampFormat);
    }

    /// <summary>
    /// Process all XML files in the input folder.
    /// </summary>
    public async Task<BatchResult> ProcessBatchAsync(
        string outputPath,
        string baseId,
        bool filterValidForReadApi = true,
        bool filterIsRetrievable = true,
        List<string>? entityFilter = null,
        bool generateEventEnvelopes = false,
        bool prettyPrint = true,
        string? basePath = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BatchResult();
        var inputFiles = GetInputFiles(basePath);

        if (inputFiles.Count == 0)
        {
            ReportProgress("No XML files found in Input folder.");
            return result;
        }

        ReportProgress($"Found {inputFiles.Count} XML file(s) to process.");

        foreach (var inputFile in inputFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                ReportProgress("Batch processing cancelled.");
                break;
            }

            var fileResult = await ProcessSingleFileAsync(
                inputFile,
                outputPath,
                baseId,
                filterValidForReadApi,
                filterIsRetrievable,
                entityFilter,
                generateEventEnvelopes,
                prettyPrint,
                cancellationToken);

            result.ProcessedFiles.Add(fileResult);
        }

        return result;
    }

    /// <summary>
    /// Process a single XML file with timestamp suffix and archiving.
    /// </summary>
    private async Task<BatchFileResult> ProcessSingleFileAsync(
        string inputFile,
        string outputPath,
        string baseId,
        bool filterValidForReadApi,
        bool filterIsRetrievable,
        List<string>? entityFilter,
        bool generateEventEnvelopes,
        bool prettyPrint,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(inputFile);
        var timestamp = GenerateTimestamp();

        ReportProgress($"Processing: {fileName} (timestamp: {timestamp})");

        try
        {
            var options = new Models.GeneratorOptions
            {
                InputPath = inputFile,
                OutputPath = outputPath,
                BaseId = baseId,
                FilterValidForReadApi = filterValidForReadApi,
                FilterIsRetrievable = filterIsRetrievable,
                EntityFilter = entityFilter ?? [],
                GenerateEventEnvelopes = generateEventEnvelopes,
                PrettyPrint = prettyPrint,
                TimestampSuffix = timestamp
            };

            // Parse entities
            var entities = _generatorService.ParseEntities(inputFile);

            if (entities.Count == 0)
            {
                ReportProgress($"  No entities found in {fileName}.");

                // Move to BadXml since it can't be processed
                var badXmlPath = MoveToBadXml(inputFile, timestamp);
                ReportProgress($"  Moved to: {badXmlPath}");

                return new BatchFileResult
                {
                    SourceFile = inputFile,
                    Timestamp = timestamp,
                    Success = false,
                    ErrorMessage = "No entities found in file",
                    BadXmlPath = badXmlPath
                };
            }

            ReportProgress($"  Found {entities.Count} entities.");

            // Apply entity filter if specified
            if (entityFilter is { Count: > 0 })
            {
                entities = _generatorService.FilterEntities(entities, entityFilter);
                ReportProgress($"  Filtered to {entities.Count} entities.");
            }

            // Generate schemas
            var generationResult = await _generatorService.GenerateAsync(options, entities, cancellationToken);

            // Archive the file
            var archivePath = ArchiveFile(inputFile, timestamp);

            ReportProgress($"  Generated {generationResult.EntitySchemasGenerated} entity schemas.");
            if (generateEventEnvelopes)
            {
                ReportProgress($"  Generated {generationResult.EventSchemasGenerated} event schemas.");
            }
            ReportProgress($"  Archived to: {archivePath}");

            return new BatchFileResult
            {
                SourceFile = inputFile,
                Timestamp = timestamp,
                Success = true,
                EntitySchemasGenerated = generationResult.EntitySchemasGenerated,
                EventSchemasGenerated = generationResult.EventSchemasGenerated,
                ArchivePath = archivePath
            };
        }
        catch (Exception ex)
        {
            ReportProgress($"  Error processing {fileName}: {ex.Message}");

            // Move failed file to BadXml folder
            string? badXmlPath = null;
            try
            {
                badXmlPath = MoveToBadXml(inputFile, timestamp);
                ReportProgress($"  Moved to: {badXmlPath}");
            }
            catch (Exception moveEx)
            {
                ReportProgress($"  Failed to move file to BadXml: {moveEx.Message}");
            }

            return new BatchFileResult
            {
                SourceFile = inputFile,
                Timestamp = timestamp,
                Success = false,
                ErrorMessage = ex.Message,
                BadXmlPath = badXmlPath
            };
        }
    }

    /// <summary>
    /// Archives a successfully processed file to Input/Archive/{timestamp}/ subdirectory.
    /// </summary>
    private static string ArchiveFile(string sourceFile, string timestamp)
    {
        var inputFolder = Path.GetDirectoryName(sourceFile)!;
        var fileName = Path.GetFileName(sourceFile);
        var archiveFolder = Path.Combine(inputFolder, ArchiveFolderName, timestamp);

        Directory.CreateDirectory(archiveFolder);

        var destinationPath = Path.Combine(archiveFolder, fileName);
        File.Move(sourceFile, destinationPath);

        return destinationPath;
    }

    /// <summary>
    /// Moves a failed file to Input/BadXml/{timestamp}/ subdirectory.
    /// </summary>
    private static string MoveToBadXml(string sourceFile, string timestamp)
    {
        var inputFolder = Path.GetDirectoryName(sourceFile)!;
        var fileName = Path.GetFileName(sourceFile);
        var badXmlFolder = Path.Combine(inputFolder, BadXmlFolderName, timestamp);

        Directory.CreateDirectory(badXmlFolder);

        var destinationPath = Path.Combine(badXmlFolder, fileName);
        File.Move(sourceFile, destinationPath);

        return destinationPath;
    }

    private void ReportProgress(string message)
    {
        ProgressChanged?.Invoke(message);
    }
}
