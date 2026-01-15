namespace DataverseSchemaGenerator.Core.Models;

/// <summary>
/// Represents a Dataverse entity with its attributes.
/// </summary>
public sealed class EntityMetadata
{
    public required string Name { get; init; }
    public required string LogicalName { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? PrimaryIdAttribute { get; init; }
    public string? PrimaryNameAttribute { get; init; }
    public List<AttributeMetadata> Attributes { get; init; } = [];
}

/// <summary>
/// Represents a Dataverse attribute (column).
/// </summary>
public sealed class AttributeMetadata
{
    public required string LogicalName { get; init; }
    public required string PhysicalName { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public required DataverseAttributeType AttributeType { get; init; }
    public string? AttributeTypeName { get; init; }

    // String properties
    public int? MaxLength { get; init; }

    // Numeric properties
    public int? MinValue { get; init; }
    public int? MaxValue { get; init; }
    public int? Precision { get; init; }

    // Validation properties
    public RequiredLevel RequiredLevel { get; init; } = RequiredLevel.None;
    public bool ValidForReadApi { get; init; } = true;
    public bool IsRetrievable { get; init; } = true;
    public bool IsValidForCreate { get; init; } = true;
    public bool IsValidForUpdate { get; init; } = true;

    // OptionSet properties
    public List<OptionSetValue>? OptionSetValues { get; init; }
    public string? OptionSetName { get; init; }

    // Lookup properties
    public List<string>? LookupTargets { get; init; }
}

/// <summary>
/// Represents a value in a Dataverse OptionSet (picklist).
/// </summary>
public sealed class OptionSetValue
{
    public required int Value { get; init; }
    public required string Label { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Dataverse attribute types mapped from customizations.xml.
/// </summary>
public enum DataverseAttributeType
{
    Unknown,
    Uniqueidentifier,
    String,
    Nvarchar,
    Ntext,
    Memo,
    DateTime,
    Integer,
    BigInt,
    Decimal,
    Double,
    Money,
    Boolean,
    Picklist,
    State,
    Status,
    Lookup,
    Owner,
    Customer,
    EntityName,
    Virtual,
    ManagedProperty,
    MultiSelectPicklist,
    Image,
    File
}

/// <summary>
/// Dataverse required level for attributes.
/// </summary>
public enum RequiredLevel
{
    None,
    Recommended,
    Required,
    SystemRequired
}

/// <summary>
/// Configuration options for the schema generator.
/// </summary>
public sealed class GeneratorOptions
{
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public required string BaseId { get; init; }

    /// <summary>
    /// Only include attributes where ValidForReadApi = true.
    /// </summary>
    public bool FilterValidForReadApi { get; init; } = true;

    /// <summary>
    /// Only include attributes where IsRetrievable = true.
    /// </summary>
    public bool FilterIsRetrievable { get; init; } = true;

    /// <summary>
    /// Filter to specific entities (empty = all entities).
    /// </summary>
    public List<string> EntityFilter { get; init; } = [];

    /// <summary>
    /// Generate EventBus envelope schemas.
    /// </summary>
    public bool GenerateEventEnvelopes { get; init; } = false;

    /// <summary>
    /// Pretty print JSON output.
    /// </summary>
    public bool PrettyPrint { get; init; } = true;

    /// <summary>
    /// Timestamp suffix for output filenames (format: ddMMyy_HHmmss).
    /// When set, output files will be named {entity}_{timestamp}.json.
    /// </summary>
    public string? TimestampSuffix { get; init; }
}
