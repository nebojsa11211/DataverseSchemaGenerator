using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using DataverseSchemaGenerator.Core.Models;

namespace DataverseSchemaGenerator.Core.Output;

/// <summary>
/// Handles writing JSON Schema documents to the file system.
/// </summary>
public sealed class SchemaWriter
{
    private readonly GeneratorOptions _options;
    private readonly JsonWriterOptions _writerOptions;

    public SchemaWriter(GeneratorOptions options)
    {
        _options = options;
        _writerOptions = new JsonWriterOptions
        {
            Indented = options.PrettyPrint,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    /// <summary>
    /// Write a schema to the output directory.
    /// </summary>
    public async Task WriteSchemaAsync(string fileName, JsonObject schema)
    {
        // Ensure output directory exists
        Directory.CreateDirectory(_options.OutputPath);

        var filePath = Path.Combine(_options.OutputPath, fileName);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, _writerOptions))
        {
            schema.WriteTo(writer);
        }

        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Write entity schema to file.
    /// </summary>
    public async Task WriteEntitySchemaAsync(EntityMetadata entity, JsonObject schema)
    {
        var fileName = $"{entity.LogicalName}.json";
        await WriteSchemaAsync(fileName, schema);
    }

    /// <summary>
    /// Write event envelope schema to file.
    /// </summary>
    public async Task WriteEventSchemaAsync(EntityMetadata entity, JsonObject schema)
    {
        // Ensure events subdirectory exists
        var eventsPath = Path.Combine(_options.OutputPath, "events");
        Directory.CreateDirectory(eventsPath);

        var fileName = Path.Combine("events", $"{entity.LogicalName}-event.json");
        await WriteSchemaAsync(fileName, schema);
    }
}
