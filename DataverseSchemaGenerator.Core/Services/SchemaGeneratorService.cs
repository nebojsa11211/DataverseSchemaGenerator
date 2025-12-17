using DataverseSchemaGenerator.Core.Models;
using DataverseSchemaGenerator.Core.Output;
using DataverseSchemaGenerator.Core.Parsing;
using DataverseSchemaGenerator.Core.Schema;

namespace DataverseSchemaGenerator.Core.Services;

/// <summary>
/// Service that orchestrates the schema generation process.
/// </summary>
public sealed class SchemaGeneratorService
{
    /// <summary>
    /// Event raised when progress is made during generation.
    /// </summary>
    public event Action<string>? ProgressChanged;

    /// <summary>
    /// Parse entities from a customizations.xml file.
    /// </summary>
    public IReadOnlyList<EntityMetadata> ParseEntities(string filePath)
    {
        var parser = new CustomizationsParser();
        return parser.Parse(filePath);
    }

    /// <summary>
    /// Generate schemas for the specified entities.
    /// </summary>
    public async Task<GenerationResult> GenerateAsync(
        GeneratorOptions options,
        IReadOnlyList<EntityMetadata> entities,
        CancellationToken cancellationToken = default)
    {
        var result = new GenerationResult();

        var schemaBuilder = new JsonSchemaBuilder(options);
        var schemaWriter = new SchemaWriter(options);

        foreach (var entity in entities)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Build and write entity schema
            var entitySchema = schemaBuilder.BuildEntitySchema(entity);
            await schemaWriter.WriteEntitySchemaAsync(entity, entitySchema);
            result.EntitySchemasGenerated++;

            ReportProgress($"Generated: {entity.LogicalName}.json ({entity.Attributes.Count} attributes)");

            // Build and write event envelope schema if requested
            if (options.GenerateEventEnvelopes)
            {
                var eventSchema = schemaBuilder.BuildEventEnvelopeSchema(entity);
                await schemaWriter.WriteEventSchemaAsync(entity, eventSchema);
                result.EventSchemasGenerated++;
            }
        }

        return result;
    }

    /// <summary>
    /// Filter entities based on options.
    /// </summary>
    public IReadOnlyList<EntityMetadata> FilterEntities(
        IReadOnlyList<EntityMetadata> entities,
        IReadOnlyList<string> entityFilter)
    {
        if (entityFilter.Count == 0)
        {
            return entities;
        }

        var filterSet = new HashSet<string>(entityFilter, StringComparer.OrdinalIgnoreCase);
        return entities.Where(e => filterSet.Contains(e.LogicalName)).ToList();
    }

    private void ReportProgress(string message)
    {
        ProgressChanged?.Invoke(message);
    }
}

/// <summary>
/// Result of schema generation.
/// </summary>
public sealed class GenerationResult
{
    public int EntitySchemasGenerated { get; set; }
    public int EventSchemasGenerated { get; set; }
}
