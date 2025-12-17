using System.Text.Json.Nodes;
using DataverseSchemaGenerator.Core.Models;

namespace DataverseSchemaGenerator.Core.Schema;

/// <summary>
/// Maps Dataverse attribute types to JSON Schema type definitions.
/// </summary>
public static class TypeMapper
{
    /// <summary>
    /// Convert a Dataverse attribute to a JSON Schema property definition.
    /// </summary>
    public static JsonObject MapToJsonSchemaProperty(AttributeMetadata attribute)
    {
        var property = new JsonObject();

        // Add description if available
        if (!string.IsNullOrEmpty(attribute.Description))
        {
            property["description"] = attribute.Description;
        }
        else if (!string.IsNullOrEmpty(attribute.DisplayName))
        {
            property["description"] = attribute.DisplayName;
        }

        // Map based on attribute type
        switch (attribute.AttributeType)
        {
            case DataverseAttributeType.Uniqueidentifier:
                MapUniqueIdentifier(property);
                break;

            case DataverseAttributeType.String:
            case DataverseAttributeType.Nvarchar:
                MapString(property, attribute.MaxLength);
                break;

            case DataverseAttributeType.Ntext:
            case DataverseAttributeType.Memo:
                MapMemo(property, attribute.MaxLength);
                break;

            case DataverseAttributeType.DateTime:
                MapDateTime(property);
                break;

            case DataverseAttributeType.Integer:
                MapInteger(property, attribute.MinValue, attribute.MaxValue);
                break;

            case DataverseAttributeType.BigInt:
                MapBigInt(property);
                break;

            case DataverseAttributeType.Decimal:
            case DataverseAttributeType.Double:
            case DataverseAttributeType.Money:
                MapNumber(property, attribute.Precision);
                break;

            case DataverseAttributeType.Boolean:
                MapBoolean(property);
                break;

            case DataverseAttributeType.Picklist:
            case DataverseAttributeType.State:
            case DataverseAttributeType.Status:
                MapPicklist(property, attribute.OptionSetValues);
                break;

            case DataverseAttributeType.MultiSelectPicklist:
                MapMultiSelectPicklist(property, attribute.OptionSetValues);
                break;

            case DataverseAttributeType.Lookup:
            case DataverseAttributeType.Owner:
            case DataverseAttributeType.Customer:
                MapLookup(property, attribute.LookupTargets);
                break;

            case DataverseAttributeType.EntityName:
                MapEntityName(property);
                break;

            case DataverseAttributeType.Image:
            case DataverseAttributeType.File:
                MapBinaryData(property);
                break;

            default:
                // Default to string for unknown types
                property["type"] = "string";
                break;
        }

        return property;
    }

    private static void MapUniqueIdentifier(JsonObject property)
    {
        property["type"] = "string";
        property["format"] = "uuid";
    }

    private static void MapString(JsonObject property, int? maxLength)
    {
        property["type"] = "string";
        if (maxLength.HasValue && maxLength.Value > 0)
        {
            property["maxLength"] = maxLength.Value;
        }
    }

    private static void MapMemo(JsonObject property, int? maxLength)
    {
        property["type"] = "string";
        if (maxLength.HasValue && maxLength.Value > 0)
        {
            property["maxLength"] = maxLength.Value;
        }
    }

    private static void MapDateTime(JsonObject property)
    {
        property["type"] = "string";
        property["format"] = "date-time";
    }

    private static void MapInteger(JsonObject property, int? minValue, int? maxValue)
    {
        property["type"] = "integer";
        if (minValue.HasValue)
        {
            property["minimum"] = minValue.Value;
        }
        if (maxValue.HasValue)
        {
            property["maximum"] = maxValue.Value;
        }
    }

    private static void MapBigInt(JsonObject property)
    {
        property["type"] = "integer";
        // JSON Schema doesn't have native int64, but we can add format hint
        property["format"] = "int64";
    }

    private static void MapNumber(JsonObject property, int? precision)
    {
        property["type"] = "number";
        // Note: JSON Schema doesn't directly support precision, but we document it
        if (precision.HasValue && precision.Value > 0)
        {
            // Use multipleOf for precision (e.g., precision 2 = 0.01)
            var multipleOf = Math.Pow(10, -precision.Value);
            property["multipleOf"] = multipleOf;
        }
    }

    private static void MapBoolean(JsonObject property)
    {
        property["type"] = "boolean";
    }

    private static void MapPicklist(JsonObject property, List<OptionSetValue>? optionSetValues)
    {
        if (optionSetValues is { Count: > 0 })
        {
            // Use oneOf with const and title for each option
            var oneOf = new JsonArray();
            foreach (var option in optionSetValues)
            {
                var optionDef = new JsonObject
                {
                    ["const"] = option.Value,
                    ["title"] = option.Label
                };
                if (!string.IsNullOrEmpty(option.Description))
                {
                    optionDef["description"] = option.Description;
                }
                oneOf.Add(optionDef);
            }
            property["oneOf"] = oneOf;
        }
        else
        {
            // No inline optionset, fall back to integer
            property["type"] = "integer";
        }
    }

    private static void MapMultiSelectPicklist(JsonObject property, List<OptionSetValue>? optionSetValues)
    {
        property["type"] = "array";

        var items = new JsonObject();
        if (optionSetValues is { Count: > 0 })
        {
            var oneOf = new JsonArray();
            foreach (var option in optionSetValues)
            {
                var optionDef = new JsonObject
                {
                    ["const"] = option.Value,
                    ["title"] = option.Label
                };
                if (!string.IsNullOrEmpty(option.Description))
                {
                    optionDef["description"] = option.Description;
                }
                oneOf.Add(optionDef);
            }
            items["oneOf"] = oneOf;
        }
        else
        {
            items["type"] = "integer";
        }

        property["items"] = items;
        property["uniqueItems"] = true;
    }

    private static void MapLookup(JsonObject property, List<string>? lookupTargets)
    {
        property["type"] = "string";
        property["format"] = "uuid";

        // Add lookup target information as a custom extension if available
        if (lookupTargets is { Count: > 0 })
        {
            if (lookupTargets.Count == 1)
            {
                property["x-lookup-target"] = lookupTargets[0];
            }
            else
            {
                var targets = new JsonArray();
                foreach (var target in lookupTargets)
                {
                    targets.Add(target);
                }
                property["x-lookup-targets"] = targets;
            }
        }
    }

    private static void MapEntityName(JsonObject property)
    {
        property["type"] = "string";
        // Entity name is typically a logical name reference
        property["pattern"] = "^[a-z_][a-z0-9_]*$";
    }

    private static void MapBinaryData(JsonObject property)
    {
        // Binary data is typically base64 encoded or a URL reference
        property["type"] = "string";
        property["contentEncoding"] = "base64";
    }
}
