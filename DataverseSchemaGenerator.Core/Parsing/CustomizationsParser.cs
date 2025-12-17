using System.Xml.Linq;
using DataverseSchemaGenerator.Core.Models;

namespace DataverseSchemaGenerator.Core.Parsing;

/// <summary>
/// Parses Dataverse customizations.xml files to extract entity and attribute metadata.
/// </summary>
public sealed class CustomizationsParser
{
    /// <summary>
    /// Parse a customizations.xml file and extract all entities with their attributes.
    /// </summary>
    public IReadOnlyList<EntityMetadata> Parse(string filePath)
    {
        var document = XDocument.Load(filePath);
        var entities = new List<EntityMetadata>();

        var entitiesElement = document.Root?.Element("Entities");
        if (entitiesElement is null)
        {
            return entities;
        }

        foreach (var entityElement in entitiesElement.Elements("Entity"))
        {
            var entity = ParseEntity(entityElement);
            if (entity is not null)
            {
                entities.Add(entity);
            }
        }

        return entities.OrderBy(e => e.LogicalName).ToList();
    }

    private EntityMetadata? ParseEntity(XElement entityElement)
    {
        var nameElement = entityElement.Element("Name");
        if (nameElement is null)
        {
            return null;
        }

        var name = nameElement.Value;
        var localizedNames = entityElement.Element("EntityInfo")?.Element("entity")?.Element("LocalizedNames");
        var displayName = GetLocalizedLabel(localizedNames);

        var descriptions = entityElement.Element("EntityInfo")?.Element("entity")?.Element("Descriptions");
        var description = GetLocalizedLabel(descriptions);

        var entityInfo = entityElement.Element("EntityInfo")?.Element("entity");

        var attributes = ParseAttributes(entityElement);

        return new EntityMetadata
        {
            Name = name,
            LogicalName = name.ToLowerInvariant(),
            DisplayName = displayName ?? name,
            Description = description,
            PrimaryIdAttribute = entityInfo?.Attribute("primaryidattribute")?.Value,
            PrimaryNameAttribute = entityInfo?.Attribute("primaryattribute")?.Value,
            Attributes = attributes
        };
    }

    private List<AttributeMetadata> ParseAttributes(XElement entityElement)
    {
        var attributes = new List<AttributeMetadata>();

        var attributesElement = entityElement.Element("EntityInfo")?.Element("entity")?.Element("attributes");
        if (attributesElement is null)
        {
            return attributes;
        }

        foreach (var attrElement in attributesElement.Elements("attribute"))
        {
            var attribute = ParseAttribute(attrElement);
            if (attribute is not null)
            {
                attributes.Add(attribute);
            }
        }

        return attributes.OrderBy(a => a.LogicalName).ToList();
    }

    private AttributeMetadata? ParseAttribute(XElement attrElement)
    {
        var physicalName = attrElement.Attribute("PhysicalName")?.Value;
        if (string.IsNullOrEmpty(physicalName))
        {
            return null;
        }

        var logicalName = physicalName.ToLowerInvariant();
        var attributeTypeName = attrElement.Element("Type")?.Value;
        var attributeType = ParseAttributeType(attributeTypeName);

        // Parse display name
        var displayNames = attrElement.Element("displaynames");
        var displayName = GetLocalizedLabel(displayNames);

        // Parse description
        var descriptions = attrElement.Element("Descriptions");
        var description = GetLocalizedLabel(descriptions);

        // Parse required level
        var requiredLevel = ParseRequiredLevel(attrElement.Element("RequiredLevel")?.Value);

        // Parse validation flags
        var validForReadApi = ParseBoolAttribute(attrElement, "ValidForReadApi", true);
        var isRetrievable = ParseBoolAttribute(attrElement, "IsRetrievable", true);
        var isValidForCreate = ParseBoolAttribute(attrElement, "ValidForCreateApi", true);
        var isValidForUpdate = ParseBoolAttribute(attrElement, "ValidForUpdateApi", true);

        // Parse type-specific properties
        int? maxLength = ParseIntElement(attrElement, "Length") ?? ParseIntElement(attrElement, "MaxLength");
        int? minValue = ParseIntElement(attrElement, "MinValue");
        int? maxValue = ParseIntElement(attrElement, "MaxValue");
        int? precision = ParseIntElement(attrElement, "Precision");

        // Parse optionset values
        var optionSetValues = ParseOptionSetValues(attrElement);
        var optionSetName = attrElement.Element("optionset")?.Attribute("Name")?.Value;

        // Parse lookup targets
        var lookupTargets = ParseLookupTargets(attrElement);

        return new AttributeMetadata
        {
            LogicalName = logicalName,
            PhysicalName = physicalName,
            DisplayName = displayName ?? physicalName,
            Description = description,
            AttributeType = attributeType,
            AttributeTypeName = attributeTypeName,
            MaxLength = maxLength,
            MinValue = minValue,
            MaxValue = maxValue,
            Precision = precision,
            RequiredLevel = requiredLevel,
            ValidForReadApi = validForReadApi,
            IsRetrievable = isRetrievable,
            IsValidForCreate = isValidForCreate,
            IsValidForUpdate = isValidForUpdate,
            OptionSetValues = optionSetValues,
            OptionSetName = optionSetName,
            LookupTargets = lookupTargets
        };
    }

    private static DataverseAttributeType ParseAttributeType(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return DataverseAttributeType.Unknown;
        }

        return typeName.ToLowerInvariant() switch
        {
            "primarykey" or "uniqueidentifier" => DataverseAttributeType.Uniqueidentifier,
            "nvarchar" or "string" => DataverseAttributeType.Nvarchar,
            "ntext" => DataverseAttributeType.Ntext,
            "memo" => DataverseAttributeType.Memo,
            "datetime" => DataverseAttributeType.DateTime,
            "int" or "integer" => DataverseAttributeType.Integer,
            "bigint" => DataverseAttributeType.BigInt,
            "decimal" => DataverseAttributeType.Decimal,
            "float" or "double" => DataverseAttributeType.Double,
            "money" => DataverseAttributeType.Money,
            "bit" or "boolean" => DataverseAttributeType.Boolean,
            "picklist" => DataverseAttributeType.Picklist,
            "state" => DataverseAttributeType.State,
            "status" => DataverseAttributeType.Status,
            "lookup" => DataverseAttributeType.Lookup,
            "owner" => DataverseAttributeType.Owner,
            "customer" => DataverseAttributeType.Customer,
            "entityname" => DataverseAttributeType.EntityName,
            "virtual" => DataverseAttributeType.Virtual,
            "managedproperty" => DataverseAttributeType.ManagedProperty,
            "multiselectpicklist" => DataverseAttributeType.MultiSelectPicklist,
            "image" => DataverseAttributeType.Image,
            "file" => DataverseAttributeType.File,
            _ => DataverseAttributeType.Unknown
        };
    }

    private static RequiredLevel ParseRequiredLevel(string? level)
    {
        if (string.IsNullOrEmpty(level))
        {
            return RequiredLevel.None;
        }

        return level.ToLowerInvariant() switch
        {
            "none" or "applicationrequired" => RequiredLevel.None,
            "recommended" => RequiredLevel.Recommended,
            "required" => RequiredLevel.Required,
            "systemrequired" => RequiredLevel.SystemRequired,
            _ => RequiredLevel.None
        };
    }

    private static bool ParseBoolAttribute(XElement element, string attributeName, bool defaultValue)
    {
        var value = element.Element(attributeName)?.Value ?? element.Attribute(attributeName)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return value.Equals("1", StringComparison.Ordinal) ||
               value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static int? ParseIntElement(XElement element, string elementName)
    {
        var value = element.Element(elementName)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return int.TryParse(value, out var result) ? result : null;
    }

    private static string? GetLocalizedLabel(XElement? labelsElement)
    {
        if (labelsElement is null)
        {
            return null;
        }

        // Try to get English (1033) first, then fall back to first available
        var englishLabel = labelsElement.Elements()
            .FirstOrDefault(e => e.Attribute("languagecode")?.Value == "1033");

        if (englishLabel is not null)
        {
            return englishLabel.Attribute("description")?.Value;
        }

        return labelsElement.Elements().FirstOrDefault()?.Attribute("description")?.Value;
    }

    private static List<OptionSetValue>? ParseOptionSetValues(XElement attrElement)
    {
        var optionSetElement = attrElement.Element("optionset");
        if (optionSetElement is null)
        {
            return null;
        }

        var options = new List<OptionSetValue>();

        foreach (var optionElement in optionSetElement.Elements("option"))
        {
            var valueStr = optionElement.Attribute("value")?.Value;
            if (string.IsNullOrEmpty(valueStr) || !int.TryParse(valueStr, out var value))
            {
                continue;
            }

            var labels = optionElement.Element("labels");
            var label = GetLocalizedLabel(labels) ?? $"Option_{value}";

            var descriptions = optionElement.Element("Descriptions");
            var description = GetLocalizedLabel(descriptions);

            options.Add(new OptionSetValue
            {
                Value = value,
                Label = label,
                Description = description
            });
        }

        return options.Count > 0 ? options.OrderBy(o => o.Value).ToList() : null;
    }

    private static List<string>? ParseLookupTargets(XElement attrElement)
    {
        var lookupTypes = attrElement.Element("LookupTypes");
        if (lookupTypes is null)
        {
            return null;
        }

        var targets = lookupTypes.Elements("LookupType")
            .Select(lt => lt.Attribute("id")?.Value ?? lt.Value)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        return targets.Count > 0 ? targets : null;
    }
}
