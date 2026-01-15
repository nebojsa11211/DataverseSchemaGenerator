# Dataverse Schema Generator - Complete Documentation

This document provides a comprehensive description of all functionalities in the Dataverse Schema Generator solution.

## Table of Contents

1. [Overview](#overview)
2. [Solution Architecture](#solution-architecture)
3. [Core Library (DataverseSchemaGenerator.Core)](#core-library)
   - [Data Models](#data-models)
   - [XML Parsing](#xml-parsing)
   - [Type Mapping](#type-mapping)
   - [JSON Schema Building](#json-schema-building)
   - [Global OptionSet Registry](#global-optionset-registry)
   - [Schema Output](#schema-output)
   - [Orchestration Service](#orchestration-service)
   - [Batch Processor](#batch-processor)
4. [Console Application](#console-application)
5. [WPF Application](#wpf-application)
6. [Data Flow](#data-flow)
7. [Type Mapping Reference](#type-mapping-reference)
8. [Filtering Behavior](#filtering-behavior)
9. [Extensibility Guide](#extensibility-guide)

---

## Overview

The Dataverse Schema Generator is a .NET solution that generates **JSON Schema (Draft 7)** documents from Microsoft Dataverse solution exports (`customizations.xml`). These schemas serve as the single source of truth for entity data structures and can be reused by multiple systems including OpenAPI specifications, EventBus integrations, and various data validation scenarios.

### Key Capabilities

- **Parses** Microsoft Dataverse `customizations.xml` files exported from Power Platform solutions
- **Generates** one JSON Schema document per entity with full type mapping
- **Supports** inline OptionSet (picklist) values with `oneOf` and `const/title` patterns
- **Provides** configurable attribute filtering based on API visibility flags
- **Creates** optional EventBus envelope schemas for event-driven architectures
- **Offers** both command-line (CLI) and graphical (WPF) user interfaces

---

## Solution Architecture

```
DataverseSchemaGenerator.sln
├── DataverseSchemaGenerator.Core/     # Shared library (.NET 8)
│   ├── Models/                        # Data models
│   ├── Parsing/                       # XML parsing
│   ├── Schema/                        # Schema generation
│   ├── Output/                        # File writing
│   └── Services/                      # Orchestration & batch processing
├── DataverseSchemaGenerator/          # Console CLI app (.NET 8)
└── DataverseSchemaGenerator.Wpf/      # WPF GUI app (.NET 10-windows)
    ├── ViewModels/                    # MVVM ViewModels
    └── Services/                      # Dialog abstractions
```

### Dependencies

| Project | Package | Version | Purpose |
|---------|---------|---------|---------|
| Core | System.Xml.Linq | Built-in | XML parsing |
| Core | System.Text.Json | Built-in | JSON generation |
| Console | System.CommandLine | 2.0.0-beta4 | CLI argument parsing |
| WPF | CommunityToolkit.Mvvm | 8.4.0 | MVVM source generators |

---

## Core Library

The Core library (`DataverseSchemaGenerator.Core`) contains all business logic and is shared between both applications.

### Data Models

**Location:** `Models/DataverseMetadata.cs`

#### EntityMetadata

Represents a complete Dataverse entity (table) definition.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | string | Entity name (e.g., "account") |
| `LogicalName` | string | Lowercase logical name used in APIs |
| `DisplayName` | string | User-friendly display name |
| `Description` | string | Entity description text |
| `PrimaryIdAttribute` | string | Primary key attribute name (e.g., "accountid") |
| `PrimaryNameAttribute` | string | Primary name attribute (e.g., "name") |
| `Attributes` | List\<AttributeMetadata\> | Collection of all entity columns |

#### AttributeMetadata

Represents a single attribute (column) within an entity.

**Identity Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `LogicalName` | string | Lowercase logical name (e.g., "accountnumber") |
| `PhysicalName` | string | Physical column name (e.g., "AccountNumber") |
| `DisplayName` | string | User-friendly display name |
| `Description` | string | Attribute description text |

**Type Information:**
| Property | Type | Description |
|----------|------|-------------|
| `AttributeType` | DataverseAttributeType | Enum value representing the data type |
| `AttributeTypeName` | string | String representation of the type |

**Validation & Constraints:**
| Property | Type | Description |
|----------|------|-------------|
| `RequiredLevel` | RequiredLevel | None, Recommended, Required, or SystemRequired |
| `ValidForReadApi` | bool | Whether attribute is included in Read API responses |
| `IsRetrievable` | bool | Whether attribute can be retrieved via API |
| `IsValidForCreate` | bool | Whether attribute can be set during entity creation |
| `IsValidForUpdate` | bool | Whether attribute can be updated after creation |

**Type-Specific Properties:**
| Property | Type | Applicable Types | Description |
|----------|------|------------------|-------------|
| `MaxLength` | int | String, Nvarchar, Memo | Maximum character length |
| `MinValue` | long? | Integer, BigInt | Minimum allowed value |
| `MaxValue` | long? | Integer, BigInt | Maximum allowed value |
| `Precision` | int | Decimal, Money | Decimal precision (digits after point) |
| `OptionSetValues` | List\<OptionSetValue\> | Picklist, State, Status | Inline option definitions |
| `OptionSetName` | string | Picklist, State, Status | Reference to global OptionSet |
| `LookupTargets` | List\<string\> | Lookup, Owner, Customer | Target entity logical names |

#### DataverseAttributeType Enum

All supported Dataverse attribute types (27 total):

| Category | Types |
|----------|-------|
| **Text** | Uniqueidentifier, String, Nvarchar, Ntext, Memo |
| **Numeric** | Integer, BigInt, Decimal, Double, Money |
| **Date/Time** | DateTime |
| **Logic** | Boolean |
| **Picklist** | Picklist, State, Status, MultiSelectPicklist |
| **Relationships** | Lookup, Owner, Customer |
| **Special** | EntityName, Virtual, ManagedProperty, Image, File, Unknown |

#### OptionSetValue

Represents a single option within a picklist.

| Property | Type | Description |
|----------|------|-------------|
| `Value` | int | Integer value stored in database |
| `Label` | string | User-friendly label text |
| `Description` | string | Optional description |

#### RequiredLevel Enum

| Value | Description |
|-------|-------------|
| `None` | Optional field |
| `Recommended` | Suggested but not enforced |
| `Required` | Required by application logic |
| `SystemRequired` | Required by system (cannot be overridden) |

#### GeneratorOptions

Configuration options for schema generation.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `InputPath` | string | (required) | Path to customizations.xml file |
| `OutputPath` | string | "./" | Output directory for generated schemas |
| `BaseId` | string | "https://schemas.example.com/dataverse/" | Base URI for `$id` property |
| `FilterValidForReadApi` | bool | true | Only include attributes with ValidForReadApi=true |
| `FilterIsRetrievable` | bool | true | Only include attributes with IsRetrievable=true |
| `EntityFilter` | List\<string\> | empty | Entity logical names to process (empty = all) |
| `GenerateEventEnvelopes` | bool | false | Generate EventBus envelope schemas |
| `PrettyPrint` | bool | true | Indent JSON output for readability |
| `TimestampSuffix` | string? | null | Optional timestamp suffix for output filenames (format: ddMMyy_HHmmss) |

---

### XML Parsing

**Location:** `Parsing/CustomizationsParser.cs`

The `CustomizationsParser` class parses Dataverse `customizations.xml` files using LINQ to XML (`XDocument`).

#### Main Method

```csharp
IReadOnlyList<EntityMetadata> Parse(string filePath)
```

Loads the XML file and extracts all entity definitions from the `/ImportExportXml/Entities/Entity` path. Returns a sorted list of entities by logical name.

#### Parsing Process

1. **Entity Extraction:** Iterates through all `<Entity>` elements
2. **Entity Parsing:** For each entity:
   - Extracts name, display name, description
   - Reads primary key and primary name attributes
   - Parses all attributes within `<EntityInfo><entity><attributes>`
3. **Attribute Parsing:** For each attribute:
   - Extracts physical name, type, display name, description
   - Parses validation flags (ValidForReadApi, IsRetrievable, etc.)
   - Extracts type-specific properties (length, precision, min/max values)
   - Parses inline OptionSet values if present
   - Extracts lookup targets for relationship attributes

#### Type Mapping (XML to Enum)

| XML Type String | DataverseAttributeType |
|-----------------|------------------------|
| "primarykey", "uniqueidentifier" | Uniqueidentifier |
| "nvarchar", "string" | Nvarchar |
| "ntext" | Ntext |
| "memo" | Memo |
| "datetime" | DateTime |
| "int", "integer" | Integer |
| "bigint" | BigInt |
| "decimal" | Decimal |
| "float", "double" | Double |
| "money" | Money |
| "bit", "boolean" | Boolean |
| "picklist" | Picklist |
| "state" | State |
| "status" | Status |
| "lookup" | Lookup |
| "owner" | Owner |
| "customer" | Customer |
| "entityname" | EntityName |
| "virtual" | Virtual |
| "managedproperty" | ManagedProperty |
| "multiselectpicklist" | MultiSelectPicklist |
| "image" | Image |
| "file" | File |

#### Localization Support

The parser prioritizes English labels (language code 1033) when extracting display names and descriptions. Falls back to the first available label if English is not present.

---

### Type Mapping

**Location:** `Schema/TypeMapper.cs`

The `TypeMapper` static class converts Dataverse attribute types to JSON Schema property definitions.

#### Main Method

```csharp
static JsonObject MapToJsonSchemaProperty(AttributeMetadata attribute)
```

Returns a complete JSON Schema property object for the given attribute, including:
- `type` specification
- `format` when applicable
- `description` from attribute metadata
- Type-specific constraints (maxLength, minimum, maximum, etc.)
- OptionSet values as `oneOf` patterns
- Custom extensions for relationships

#### Type Mappings

| Dataverse Type | JSON Schema Output |
|----------------|-------------------|
| **Uniqueidentifier** | `{ "type": "string", "format": "uuid" }` |
| **String/Nvarchar** | `{ "type": "string", "maxLength": N }` |
| **Ntext/Memo** | `{ "type": "string", "maxLength": N }` |
| **DateTime** | `{ "type": "string", "format": "date-time" }` |
| **Integer** | `{ "type": "integer", "minimum": N, "maximum": M }` |
| **BigInt** | `{ "type": "integer", "format": "int64" }` |
| **Decimal/Double** | `{ "type": "number", "multipleOf": 0.001 }` |
| **Money** | `{ "type": "number", "multipleOf": 0.01 }` |
| **Boolean** | `{ "type": "boolean" }` |
| **Picklist/State/Status** | `{ "oneOf": [{"const": 1, "title": "Label1"}, ...] }` |
| **MultiSelectPicklist** | `{ "type": "array", "items": {"oneOf": [...]}, "uniqueItems": true }` |
| **Lookup** | `{ "type": "string", "format": "uuid", "x-lookup-target": "entity" }` |
| **Owner/Customer** | `{ "type": "string", "format": "uuid", "x-lookup-targets": ["entity1", "entity2"] }` |
| **EntityName** | `{ "type": "string", "pattern": "^[a-z_][a-z0-9_]*$" }` |
| **Image/File** | `{ "type": "string", "contentEncoding": "base64" }` |

#### OptionSet Resolution Priority

For picklist-type attributes, the mapper resolves option values in this order:
1. **Inline values** from attribute's `OptionSetValues` property
2. **Global registry by name** using `OptionSetName` property
3. **Global registry by attribute** using attribute's logical name
4. **Fallback** to `{ "type": "integer" }` if no values found

#### Custom Extensions

The mapper adds custom `x-` prefixed properties for relationship metadata:
- `x-lookup-target`: Single target entity (string) for Lookup types
- `x-lookup-targets`: Multiple target entities (array) for Owner/Customer types

---

### JSON Schema Building

**Location:** `Schema/JsonSchemaBuilder.cs`

The `JsonSchemaBuilder` class constructs JSON Schema (Draft 7) documents from entity metadata.

#### Constructor

```csharp
JsonSchemaBuilder(GeneratorOptions options)
```

#### Entity Schema Generation

```csharp
JsonObject BuildEntitySchema(EntityMetadata entity)
```

Generates a complete JSON Schema for an entity with this structure:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://example.com/schemas/account.json",
  "title": "Account",
  "description": "Business that represents a customer or potential customer.",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "accountid": { ... },
    "name": { ... }
  },
  "required": ["accountid", "name", "ownerid", "statecode"]
}
```

**Process:**
1. Creates root schema object with `$schema`, `$id`, `title`, `type`, and `additionalProperties: false`
2. Filters attributes based on `GeneratorOptions` settings
3. Maps each attribute to a JSON Schema property using `TypeMapper`
4. Collects required attributes (RequiredLevel = Required or SystemRequired)
5. Returns complete schema object

#### Event Envelope Schema Generation

```csharp
JsonObject BuildEventEnvelopeSchema(EntityMetadata entity)
```

Generates an EventBus wrapper schema with this structure:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://example.com/schemas/events/account-event.json",
  "title": "Account Event",
  "description": "EventBus envelope for account entity events",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "eventId": { "type": "string", "format": "uuid" },
    "eventType": { "type": "string", "enum": ["Create", "Update", "Delete"] },
    "eventTime": { "type": "string", "format": "date-time" },
    "entityName": { "type": "string", "const": "account" },
    "correlationId": { "type": "string", "format": "uuid" },
    "userId": { "type": "string", "format": "uuid" },
    "organizationId": { "type": "string", "format": "uuid" },
    "data": { "$ref": "https://example.com/schemas/account.json" },
    "previousData": {
      "oneOf": [
        { "$ref": "https://example.com/schemas/account.json" },
        { "type": "null" }
      ]
    }
  },
  "required": ["eventId", "eventType", "eventTime", "entityName", "data"]
}
```

#### Attribute Filtering

The builder filters attributes based on these rules:

1. **Always excluded:** Virtual and ManagedProperty types
2. **Conditional exclusion:**
   - `ValidForReadApi = false` (unless `FilterValidForReadApi = false`)
   - `IsRetrievable = false` (unless `FilterIsRetrievable = false`)

---

### Global OptionSet Registry

**Location:** `Schema/GlobalOptionSetRegistry.cs`

The `GlobalOptionSetRegistry` static class maintains a registry of global OptionSets that are not embedded directly in attribute definitions.

#### Purpose

Some OptionSets in Dataverse are defined globally and referenced by multiple attributes across different entities. The registry provides a central lookup for these shared option definitions.

#### Public Methods

| Method | Description |
|--------|-------------|
| `Register(GlobalOptionSetDefinition)` | Adds a global OptionSet to the registry |
| `GetOptionSetValues(optionSetName, attributeLogicalName)` | Retrieves option values by OptionSet name or attribute mapping |
| `GetOptionSetDescription(optionSetName, attributeLogicalName)` | Retrieves description for schema documentation |
| `IsRegistered(optionSetName)` | Checks if an OptionSet is registered |
| `GetRegisteredOptionSetNames()` | Returns all registered OptionSet names |
| `Clear()` | Clears all registrations (for testing) |
| `RegisterBuiltIns()` | Re-registers built-in OptionSets |

#### GlobalOptionSetDefinition Structure

| Property | Type | Description |
|----------|------|-------------|
| `Name` | string | OptionSet name (case-insensitive key) |
| `Description` | string | Description for JSON Schema |
| `AttributeLogicalNames` | List\<string\> | Attributes that reference this OptionSet |
| `Values` | List\<OptionSetValue\> | The actual option definitions |

#### Built-In OptionSets

The registry includes these pre-registered OptionSets:

**in_countryos (Country OptionSet)**
- 194 countries with values 1-194
- Mapped to attributes: `in_country`
- Sample values: 1=Afghanistan, 8=Australia, 31=Canada, 185=United States of America

**in_syncstatusos (Sync Status OptionSet)**
- Values: 1=OK, 2=Error
- Mapped to attributes: `in_syncstatus`

---

### Schema Output

**Location:** `Output/SchemaWriter.cs`

The `SchemaWriter` class handles writing JSON Schema documents to the file system.

#### Constructor

```csharp
SchemaWriter(GeneratorOptions options)
```

Configures JSON writer with:
- **Indentation:** Based on `PrettyPrint` option
- **Encoding:** `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` for readable Unicode characters
- **Timestamp suffix:** Based on `TimestampSuffix` option for batch mode

#### Methods

| Method | Description |
|--------|-------------|
| `WriteSchemaAsync(fileName, schema)` | Writes schema to specified file |
| `WriteEntitySchemaAsync(entity, schema)` | Writes to `{logicalName}.json` or `{logicalName}_{timestamp}.json` |
| `WriteEventSchemaAsync(entity, schema)` | Writes to `events/{logicalName}-event.json` or `events/{logicalName}-event_{timestamp}.json` |

#### Output Filenames

The writer supports two naming modes based on the `TimestampSuffix` option:

**Default (no timestamp):**
- Entity schemas: `{logicalName}.json` (e.g., `account.json`)
- Event schemas: `events/{logicalName}-event.json` (e.g., `events/account-event.json`)

**With timestamp suffix:**
- Entity schemas: `{logicalName}_{timestamp}.json` (e.g., `account_150126_084512.json`)
- Event schemas: `events/{logicalName}-event_{timestamp}.json` (e.g., `events/account-event_150126_084512.json`)

#### Output Format

- **Pretty-printed:** Indented JSON for readability (default)
- **Compact:** Minified JSON for smaller file sizes
- **Encoding:** UTF-8 with relaxed escaping

---

### Orchestration Service

**Location:** `Services/SchemaGeneratorService.cs`

The `SchemaGeneratorService` class orchestrates the complete schema generation pipeline.

#### Events

```csharp
event Action<string> ProgressChanged
```

Fired when progress messages need to be reported. Subscribers can display these in UI or console output.

#### Methods

| Method | Description |
|--------|-------------|
| `ParseEntities(filePath)` | Parses customizations.xml and returns entity list |
| `GenerateAsync(options, entities, cancellationToken)` | Generates all schemas asynchronously |
| `FilterEntities(entities, entityFilter)` | Filters entities by logical name |

#### GenerationResult

| Property | Type | Description |
|----------|------|-------------|
| `EntitySchemasGenerated` | int | Count of entity schemas written |
| `EventSchemasGenerated` | int | Count of event envelope schemas written |

#### Generation Process

1. Creates `JsonSchemaBuilder` and `SchemaWriter` instances
2. For each entity:
   - Builds entity schema
   - Writes schema to file
   - Reports progress
   - If event generation enabled:
     - Builds event envelope schema
     - Writes event schema to `events/` subdirectory
3. Returns generation result with counts

---

### Batch Processor

**Location:** `Services/BatchProcessor.cs`

The `BatchProcessor` class handles batch processing of XML files from an input folder with timestamps and organized archiving.

#### Events

```csharp
event Action<string> ProgressChanged
```

Fired when progress messages need to be reported.

#### Static Methods

| Method | Description |
|--------|-------------|
| `GetInputFolderPath(basePath?)` | Gets the input folder path (default: `./Input`) |
| `EnsureInputFolderExists(basePath?)` | Creates the input folder if it doesn't exist |
| `GetInputFiles(basePath?)` | Gets all `*.xml` files from the input folder |
| `GenerateTimestamp(dateTime?)` | Generates a timestamp string (format: ddMMyy_HHmmss) |

#### Instance Methods

| Method | Description |
|--------|-------------|
| `ProcessBatchAsync(...)` | Processes all XML files in the input folder |

#### BatchResult

Result of batch processing operation.

| Property | Type | Description |
|----------|------|-------------|
| `ProcessedFiles` | List\<BatchFileResult\> | Individual file results |
| `TotalFilesProcessed` | int | Count of successfully processed files |
| `TotalFilesFailed` | int | Count of failed files |
| `TotalEntitySchemas` | int | Total entity schemas generated |
| `TotalEventSchemas` | int | Total event schemas generated |

#### BatchFileResult

Result of processing a single file.

| Property | Type | Description |
|----------|------|-------------|
| `SourceFile` | string | Original file path |
| `Timestamp` | string | Timestamp used for this file |
| `Success` | bool | Whether processing succeeded |
| `ErrorMessage` | string? | Error message if failed |
| `EntitySchemasGenerated` | int | Entity schemas generated |
| `EventSchemasGenerated` | int | Event schemas generated |
| `ArchivePath` | string? | Path where file was archived (on success) |
| `BadXmlPath` | string? | Path where file was moved (on failure) |

#### Processing Flow

1. Get all XML files from input folder
2. For each file:
   - Generate timestamp (ddMMyy_HHmmss)
   - Parse entities using `SchemaGeneratorService`
   - Generate schemas with timestamp suffix
   - On success: Archive to `Input/Archive/{timestamp}/`
   - On failure: Move to `Input/BadXml/{timestamp}/`
3. Return aggregate results

---

## Console Application

**Location:** `DataverseSchemaGenerator/Program.cs`

The console application provides a command-line interface for schema generation with two operating modes: **default mode** (simple folder-based processing) and **batch mode** (timestamped output with archiving).

### Operating Modes

#### Default Mode (Simplest Usage)

When no `--input` is specified, the app operates in default mode:

1. Scans the `Input` folder for `*.xml` files
2. Processes each file and generates JSON schemas to the output directory (current directory by default)
3. Uses clean filenames without timestamps (e.g., `account.json`, `contact.json`)
4. Archives processed files to `Input/Archive/`

```bash
# Just place customizations.xml in Input folder and run:
dotnet run --project DataverseSchemaGenerator
```

#### Batch Mode

When `--batch` is specified (and no `--input`), batch mode features are enabled:

1. Scans the `Input` folder for `*.xml` files
2. Adds timestamp suffixes to output filenames (format: `ddMMyy_HHmmss`)
3. Archives processed files to `Input/Archive/{timestamp}/`
4. Moves failed files to `Input/BadXml/{timestamp}/`

```bash
# Process with timestamps and organized archiving
dotnet run --project DataverseSchemaGenerator -- --batch
```

Example output filenames in batch mode:
- `account_150126_084512.json`
- `contact_150126_084512.json`

#### Single File Mode

When `--input` is specified, the app processes only that specific file:

```bash
dotnet run --project DataverseSchemaGenerator -- --input ./customizations.xml --out ./schemas
```

### Command-Line Options

| Option | Alias | Type | Default | Description |
|--------|-------|------|---------|-------------|
| `--input` | `-i` | FileInfo | (optional) | Path to a specific customizations.xml. If omitted, scans Input folder |
| `--out` | `-o` | DirectoryInfo | `.` (current dir) | Output directory |
| `--base-id` | `-b` | string | `https://schemas.example.com/dataverse/` | Base URI for $id |
| `--entities` | `-e` | string[] | (all) | Entity filter (comma-separated) |
| `--include-non-readable` | | bool | false | Include ValidForReadApi=false attributes |
| `--include-non-retrievable` | | bool | false | Include IsRetrievable=false attributes |
| `--generate-events` | | bool | false | Generate EventBus envelope schemas |
| `--compact` | | bool | false | Output compact JSON |
| `--batch` | | bool | false | Enable batch mode (timestamps + organized archiving) |

### Usage Examples

```bash
# Default mode: scan Input folder, output to current directory
dotnet run --project DataverseSchemaGenerator

# Default mode with custom output directory
dotnet run --project DataverseSchemaGenerator -- --out ./schemas

# Batch mode: timestamps and organized archiving
dotnet run --project DataverseSchemaGenerator -- --batch

# Single file mode: process specific file
dotnet run --project DataverseSchemaGenerator -- --input customizations.xml --out ./schemas

# Generate specific entities with event envelopes
dotnet run --project DataverseSchemaGenerator -- -i custom.xml -o out/ -e account,contact --generate-events

# Include all attributes with compact output
dotnet run --project DataverseSchemaGenerator -- -i custom.xml --include-non-readable --include-non-retrievable --compact

# Custom base URI
dotnet run --project DataverseSchemaGenerator -- -i custom.xml -b https://myorg.com/schemas/dataverse/
```

### Folder Structure

```
./
├── Input/                      # Place customizations.xml files here
│   ├── customizations.xml      # Files to be processed
│   ├── Archive/               # Processed files are moved here
│   │   └── {timestamp}/       # In batch mode, organized by timestamp
│   │       └── customizations.xml
│   └── BadXml/                # Failed files (batch mode only)
│       └── {timestamp}/
│           └── invalid.xml
├── account.json               # Generated schemas (default mode)
├── contact.json
└── Output/                    # Alternative output location
    ├── account_150126_084512.json  # Batch mode with timestamps
    └── contact_150126_084512.json
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success (all files processed) |
| 1 | Error (file not found, parsing errors, or any files failed) |

### Execution Flow

**Default Mode (Input Folder Scanning):**
1. Ensure `Input` folder exists (create if missing)
2. Scan for `*.xml` files
3. For each file:
   - Parse entities from XML
   - Apply entity filter if specified
   - Generate schemas with clean filenames
   - Archive processed file to `Input/Archive/`
4. Output summary with counts

**Batch Mode:**
1. Ensure `Input` folder exists
2. Scan for `*.xml` files
3. For each file:
   - Generate timestamp (ddMMyy_HHmmss format)
   - Parse entities from XML
   - Generate schemas with timestamp suffix
   - Archive to `Input/Archive/{timestamp}/` on success
   - Move to `Input/BadXml/{timestamp}/` on failure
4. Output summary with success/failure counts

**Single File Mode:**
1. Validate input file exists
2. Parse entities from XML
3. Apply entity filter if specified
4. Generate schemas asynchronously
5. Output summary with counts

---

## WPF Application

**Location:** `DataverseSchemaGenerator.Wpf/`

The WPF application provides a graphical user interface using the MVVM pattern with CommunityToolkit.Mvvm.

### Main Window Layout

The UI consists of six sections:

#### 1. Input/Output Section
- **Input File:** Text box with Browse button for selecting customizations.xml
- **Output Directory:** Text box with Browse button for selecting output folder
- **Base ID:** Text box for entering the base URI for schema `$id` values

#### 2. Options Section (Checkboxes)
- **Generate Event Envelopes:** Create EventBus wrapper schemas
- **Compact Output:** Output minified JSON without indentation
- **Include Non-Readable:** Include attributes where ValidForReadApi=false
- **Include Non-Retrievable:** Include attributes where IsRetrievable=false

#### 3. Entities Section
- **Toolbar:** Load File, Select All, Select None buttons
- **Entity List:** Checkboxes for each entity showing display name, logical name, and attribute count
- **Empty State:** Instructions shown when no file is loaded

#### 4. Action Buttons
- **Generate Schemas:** Large button to start generation
- **Cancel:** Button to cancel in-progress generation (visible only during generation)

#### 5. Log Section
- **Log List:** Timestamped progress messages in monospace font

#### 6. Status Bar
- **Status Message:** Current operation status
- **Progress Indicator:** Indeterminate progress bar during generation

### ViewModels

#### MainViewModel

The primary ViewModel containing all UI state and commands.

**Observable Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `InputFilePath` | string | Path to customizations.xml |
| `OutputDirectoryPath` | string | Output directory path |
| `BaseId` | string | Base URI for $id (default: "https://schemas.example.com/dataverse/") |
| `GenerateEventEnvelopes` | bool | Generate event schemas option |
| `CompactOutput` | bool | Compact JSON output option |
| `IncludeNonReadable` | bool | Include non-readable attributes option |
| `IncludeNonRetrievable` | bool | Include non-retrievable attributes option |
| `Entities` | ObservableCollection\<EntityViewModel\> | Loaded entities |
| `LogMessages` | ObservableCollection\<string\> | Log entries |
| `IsGenerating` | bool | Generation in progress flag |
| `StatusMessage` | string | Current status text |

**Commands:**

| Command | Description |
|---------|-------------|
| `BrowseInputFile` | Opens file dialog for customizations.xml |
| `BrowseOutputDirectory` | Opens folder browser dialog |
| `LoadFile` | Parses and loads entities from input file |
| `SelectAll` | Selects all entities |
| `SelectNone` | Deselects all entities |
| `GenerateSchemas` | Generates schemas for selected entities |
| `CancelGeneration` | Cancels in-progress generation |

#### EntityViewModel

Wraps `EntityMetadata` for UI binding and selection tracking.

| Property | Type | Description |
|----------|------|-------------|
| `IsSelected` | bool | Selection state (default: true) |
| `LogicalName` | string | Entity logical name |
| `DisplayName` | string | Entity display name |
| `AttributeCount` | int | Number of attributes |
| `Description` | string | Entity description |
| `DisplayText` | string | Formatted display: "Name (N attributes)" |

### Services

#### IDialogService Interface

Abstraction for file/folder dialogs to enable testing.

```csharp
public interface IDialogService
{
    string? ShowOpenFileDialog(string title, string filter);
    string? ShowFolderBrowserDialog(string title);
}
```

#### DialogService Implementation

WPF implementation using `Microsoft.Win32.OpenFileDialog` and `Microsoft.Win32.OpenFolderDialog`.

### Workflow

1. **Load File:** User browses to customizations.xml and clicks Load
2. **Entity Selection:** Entities appear with checkboxes; user selects which to generate
3. **Configure Options:** User sets output path, base ID, and generation options
4. **Generate:** User clicks Generate Schemas button
5. **Monitor Progress:** Log shows timestamped progress messages
6. **Complete:** Status bar shows final count of generated schemas

---

## Data Flow

### Console Application Flow (Single File Mode)

```
customizations.xml
       │
       ▼
┌──────────────────┐
│ Parse Arguments  │ ◄── System.CommandLine
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ Validate Input   │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  ParseEntities   │ ◄── CustomizationsParser
│  (XML → Models)  │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Filter Entities │ ◄── --entities option
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  GenerateAsync   │
│  ┌─────────────┐ │
│  │ Build Schema│ │ ◄── JsonSchemaBuilder + TypeMapper
│  └──────┬──────┘ │
│         │        │
│  ┌──────▼──────┐ │
│  │ Write File  │ │ ◄── SchemaWriter
│  └─────────────┘ │
└────────┬─────────┘
         │
         ▼
   JSON Schema Files
```

### Console Application Flow (Default/Batch Mode)

```
Input/*.xml files
       │
       ▼
┌───────────────────────┐
│  Scan Input Folder    │ ◄── BatchProcessor.GetInputFiles()
└────────┬──────────────┘
         │
         ▼
  ┌──────┴──────┐
  │ For Each    │
  │ XML File    │
  └──────┬──────┘
         │
         ▼
┌──────────────────┐
│  ParseEntities   │ ◄── CustomizationsParser
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Filter Entities │ ◄── --entities option
└────────┬─────────┘
         │
         ▼
┌───────────────────────┐
│  GenerateAsync        │
│  (with timestamp if   │ ◄── SchemaWriter with TimestampSuffix
│   --batch enabled)    │
└────────┬──────────────┘
         │
  ┌──────┴──────┐
  │  Success?   │
  └──────┬──────┘
         │
    ┌────┴────┐
    ▼         ▼
┌────────┐ ┌─────────┐
│Archive │ │ BadXml  │  ◄── Batch mode only
│ File   │ │  Move   │
└────────┘ └─────────┘
         │
         ▼
   JSON Schema Files
   (account.json or account_150126_084512.json)
```

### WPF Application Flow

```
User Interaction
       │
       ├──► Browse Input File ──► DialogService ──► InputFilePath
       │
       ├──► Browse Output Dir ──► DialogService ──► OutputDirectoryPath
       │
       ├──► Click Load File
       │         │
       │         ▼
       │    ParseEntities ──► EntityViewModel[] ──► UI ListBox
       │
       ├──► Check/Uncheck Entities ──► IsSelected properties
       │
       ├──► Set Options ──► ViewModel properties
       │
       └──► Click Generate Schemas
                 │
                 ▼
            GenerateAsync
                 │
            ProgressChanged ──► AddLog() ──► UI Log ListBox
                 │
                 ▼
           JSON Schema Files
```

---

## Type Mapping Reference

### Complete Mapping Table

| Dataverse Type | JSON Schema Type | Format | Additional Properties |
|----------------|------------------|--------|----------------------|
| uniqueidentifier | string | uuid | - |
| nvarchar, string | string | - | maxLength |
| ntext, memo | string | - | maxLength |
| datetime | string | date-time | - |
| int, integer | integer | - | minimum, maximum |
| bigint | integer | int64 | - |
| decimal | number | - | multipleOf: 10^(-precision) |
| double, float | number | - | - |
| money | number | - | multipleOf: 0.01 |
| boolean, bit | boolean | - | - |
| picklist | oneOf | - | const/title pairs |
| state | oneOf | - | const/title pairs |
| status | oneOf | - | const/title pairs |
| multiselectpicklist | array | - | items with oneOf, uniqueItems |
| lookup | string | uuid | x-lookup-target |
| owner | string | uuid | x-lookup-targets |
| customer | string | uuid | x-lookup-targets |
| entityname | string | - | pattern: ^[a-z_][a-z0-9_]*$ |
| image | string | - | contentEncoding: base64 |
| file | string | - | contentEncoding: base64 |

### OptionSet Mapping Examples

**Simple Picklist:**
```json
{
  "description": "Account status",
  "oneOf": [
    { "const": 0, "title": "Active" },
    { "const": 1, "title": "Inactive" }
  ]
}
```

**MultiSelect Picklist:**
```json
{
  "description": "Selected categories",
  "type": "array",
  "items": {
    "oneOf": [
      { "const": 1, "title": "Category A" },
      { "const": 2, "title": "Category B" }
    ]
  },
  "uniqueItems": true
}
```

### Lookup Mapping Examples

**Single-Target Lookup:**
```json
{
  "description": "Primary contact",
  "type": "string",
  "format": "uuid",
  "x-lookup-target": "contact"
}
```

**Multi-Target Lookup (Customer):**
```json
{
  "description": "Customer reference",
  "type": "string",
  "format": "uuid",
  "x-lookup-targets": ["account", "contact"]
}
```

---

## Filtering Behavior

### Default Filtering Rules

By default, attributes are **included** only when ALL conditions are met:

1. **Type is not Virtual or ManagedProperty** (always excluded)
2. **ValidForReadApi = true** (unless `--include-non-readable`)
3. **IsRetrievable = true** (unless `--include-non-retrievable`)

### Filtering Options

| Option | Effect |
|--------|--------|
| `--include-non-readable` | Includes attributes with ValidForReadApi=false |
| `--include-non-retrievable` | Includes attributes with IsRetrievable=false |

### Required Field Rules

Attributes are marked as `required` in the JSON Schema when:
- `RequiredLevel = Required` OR
- `RequiredLevel = SystemRequired`

---

## Extensibility Guide

### Adding a New Global OptionSet

```csharp
GlobalOptionSetRegistry.Register(new GlobalOptionSetDefinition
{
    Name = "new_optionsetname",
    Description = "Description for schema documentation",
    AttributeLogicalNames = new List<string> { "attribute1", "attribute2" },
    Values = new List<OptionSetValue>
    {
        new() { Value = 1, Label = "Option One", Description = "First option" },
        new() { Value = 2, Label = "Option Two", Description = "Second option" }
    }
});
```

### Adding a New Dataverse Attribute Type

1. **Add enum value** to `DataverseAttributeType` in `DataverseMetadata.cs`
2. **Add parsing case** to `ParseAttributeType()` in `CustomizationsParser.cs`
3. **Add mapping method** to `TypeMapper.cs`
4. **Add dispatch case** to `MapToJsonSchemaProperty()` in `TypeMapper.cs`

### Adding a New CLI Option

1. **Create Option\<T\>** in `Program.cs`:
   ```csharp
   var newOption = new Option<bool>("--new-option", "Description");
   ```
2. **Add to RootCommand:**
   ```csharp
   rootCommand.AddOption(newOption);
   ```
3. **Parse in SetHandler:**
   ```csharp
   handler.SetHandler(async (input, output, ..., newOption) => { ... });
   ```
4. **Add property** to `GeneratorOptions` in `DataverseMetadata.cs`

### Adding a New WPF Option

1. **Add property** to `MainViewModel.cs`:
   ```csharp
   [ObservableProperty]
   private bool _newOption;
   ```
2. **Add UI control** in `MainWindow.xaml`:
   ```xml
   <CheckBox Content="New Option" IsChecked="{Binding NewOption}" />
   ```
3. **Use in generation** by updating `GeneratorOptions` creation in `GenerateSchemasAsync()`

---

## Output Examples

### Generated Entity Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://schemas.example.com/dataverse/account.json",
  "title": "Account",
  "description": "Business that represents a customer or potential customer.",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "accountid": {
      "description": "Unique identifier of the account.",
      "type": "string",
      "format": "uuid"
    },
    "name": {
      "description": "Type the company or business name.",
      "type": "string",
      "maxLength": 160
    },
    "industrycode": {
      "description": "Select the primary industry.",
      "oneOf": [
        { "const": 1, "title": "Accounting" },
        { "const": 2, "title": "Agriculture" },
        { "const": 3, "title": "Construction" }
      ]
    },
    "primarycontactid": {
      "description": "Primary contact for the account.",
      "type": "string",
      "format": "uuid",
      "x-lookup-target": "contact"
    }
  },
  "required": ["accountid", "name", "ownerid", "statecode"]
}
```

### Generated Event Envelope Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://schemas.example.com/dataverse/events/account-event.json",
  "title": "Account Event",
  "description": "EventBus envelope for account entity events",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "eventId": { "type": "string", "format": "uuid" },
    "eventType": { "type": "string", "enum": ["Create", "Update", "Delete"] },
    "eventTime": { "type": "string", "format": "date-time" },
    "entityName": { "type": "string", "const": "account" },
    "correlationId": { "type": "string", "format": "uuid" },
    "userId": { "type": "string", "format": "uuid" },
    "organizationId": { "type": "string", "format": "uuid" },
    "data": { "$ref": "https://schemas.example.com/dataverse/account.json" },
    "previousData": {
      "oneOf": [
        { "$ref": "https://schemas.example.com/dataverse/account.json" },
        { "type": "null" }
      ]
    }
  },
  "required": ["eventId", "eventType", "eventTime", "entityName", "data"]
}
```

### Output Directory Structure

**Default Mode (clean filenames):**
```
./
├── Input/                      # Source XML files
│   ├── Archive/               # Successfully processed files
│   │   └── customizations.xml
│   └── BadXml/                # (batch mode only)
├── account.json
├── contact.json
├── opportunity.json
└── events/                    # Only with --generate-events
    ├── account-event.json
    ├── contact-event.json
    └── opportunity-event.json
```

**Batch Mode (timestamped filenames):**
```
./
├── Input/
│   ├── Archive/
│   │   └── 150126_084512/     # Timestamped subdirectories
│   │       └── customizations.xml
│   └── BadXml/
│       └── 150126_084515/     # Failed files by timestamp
│           └── invalid.xml
├── account_150126_084512.json
├── contact_150126_084512.json
├── opportunity_150126_084512.json
└── events/                    # Only with --generate-events
    ├── account-event_150126_084512.json
    ├── contact-event_150126_084512.json
    └── opportunity-event_150126_084512.json
```
