using System.Text.Json.Nodes;
using DataverseSchemaGenerator.Core.Models;

namespace DataverseSchemaGenerator.Core.Schema;

/// <summary>
/// Builds JSON Schema documents from Dataverse entity metadata.
/// </summary>
public sealed class JsonSchemaBuilder
{
    private const string JsonSchemaDraft7 = "http://json-schema.org/draft-07/schema#";

    private readonly GeneratorOptions _options;

    public JsonSchemaBuilder(GeneratorOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Build a JSON Schema document for the given entity.
    /// </summary>
    public JsonObject BuildEntitySchema(EntityMetadata entity)
    {
        var schema = new JsonObject
        {
            ["$schema"] = JsonSchemaDraft7,
            ["$id"] = BuildSchemaId(entity.LogicalName),
            ["title"] = entity.DisplayName ?? entity.LogicalName,
            ["type"] = "object",
            ["additionalProperties"] = false
        };

        if (!string.IsNullOrEmpty(entity.Description))
        {
            schema["description"] = entity.Description;
        }
        else
        {
            schema["description"] = $"Schema for Dataverse entity: {entity.LogicalName}";
        }

        // Filter attributes based on options
        var filteredAttributes = FilterAttributes(entity.Attributes);

        // Build properties
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var attribute in filteredAttributes)
        {
            var propertySchema = TypeMapper.MapToJsonSchemaProperty(attribute);
            properties[attribute.LogicalName] = propertySchema;

            // Add to required if RequiredLevel is Required or SystemRequired
            if (attribute.RequiredLevel is RequiredLevel.Required or RequiredLevel.SystemRequired)
            {
                required.Add(attribute.LogicalName);
            }
        }

        schema["properties"] = properties;

        if (required.Count > 0)
        {
            // Sort required array for deterministic output
            var sortedRequired = new JsonArray();
            foreach (var item in required.OrderBy(r => r?.GetValue<string>()))
            {
                sortedRequired.Add(item?.GetValue<string>());
            }
            schema["required"] = sortedRequired;
        }

        return schema;
    }

    /// <summary>
    /// Build an EventBus envelope schema that references an entity schema.
    /// </summary>
    public JsonObject BuildEventEnvelopeSchema(EntityMetadata entity)
    {
        var schemaId = BuildSchemaId($"events/{entity.LogicalName}-event");

        var schema = new JsonObject
        {
            ["$schema"] = JsonSchemaDraft7,
            ["$id"] = schemaId,
            ["title"] = $"{entity.DisplayName} Event",
            ["description"] = $"EventBus envelope for {entity.LogicalName} entity events",
            ["type"] = "object",
            ["additionalProperties"] = false
        };

        var properties = new JsonObject
        {
            ["eventId"] = new JsonObject
            {
                ["type"] = "string",
                ["format"] = "uuid",
                ["description"] = "Unique identifier for this event"
            },
            ["eventType"] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray { "Create", "Update", "Delete" },
                ["description"] = "Type of entity operation"
            },
            ["eventTime"] = new JsonObject
            {
                ["type"] = "string",
                ["format"] = "date-time",
                ["description"] = "Timestamp when the event occurred"
            },
            ["entityName"] = new JsonObject
            {
                ["type"] = "string",
                ["const"] = entity.LogicalName,
                ["description"] = "Logical name of the entity"
            },
            ["correlationId"] = new JsonObject
            {
                ["type"] = "string",
                ["format"] = "uuid",
                ["description"] = "Correlation ID for tracking related events"
            },
            ["userId"] = new JsonObject
            {
                ["type"] = "string",
                ["format"] = "uuid",
                ["description"] = "ID of the user who triggered the event"
            },
            ["organizationId"] = new JsonObject
            {
                ["type"] = "string",
                ["format"] = "uuid",
                ["description"] = "ID of the Dataverse organization"
            },
            ["data"] = new JsonObject
            {
                ["$ref"] = BuildSchemaId(entity.LogicalName),
                ["description"] = "The entity data payload"
            },
            ["previousData"] = new JsonObject
            {
                ["oneOf"] = new JsonArray
                {
                    new JsonObject { ["$ref"] = BuildSchemaId(entity.LogicalName) },
                    new JsonObject { ["type"] = "null" }
                },
                ["description"] = "Previous state of the entity (for Update events)"
            }
        };

        schema["properties"] = properties;
        schema["required"] = new JsonArray
        {
            "eventId",
            "eventType",
            "eventTime",
            "entityName",
            "data"
        };

        return schema;
    }

    private string BuildSchemaId(string entityName)
    {
        var baseId = _options.BaseId.TrimEnd('/');
        return $"{baseId}/{entityName}.json";
    }

    private List<AttributeMetadata> FilterAttributes(List<AttributeMetadata> attributes)
    {
        return attributes
            .Where(a => !ShouldSkipAttribute(a))
            .Where(a => !_options.FilterValidForReadApi || a.ValidForReadApi)
            .Where(a => !_options.FilterIsRetrievable || a.IsRetrievable)
            .OrderBy(a => a.LogicalName)
            .ToList();
    }

    private static bool ShouldSkipAttribute(AttributeMetadata attribute)
    {
        // Skip virtual and managed property attributes
        return attribute.AttributeType is
            DataverseAttributeType.Virtual or
            DataverseAttributeType.ManagedProperty;
    }
}
