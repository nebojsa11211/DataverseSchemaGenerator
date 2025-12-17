# Dataverse Schema Generator

A .NET 8 console application that generates valid JSON Schema (Draft 7) documents from Microsoft Dataverse solution exports (`customizations.xml`).

## Overview

This generator produces language-independent JSON Schema documents that can be reused by multiple systems including OpenAPI, EventBus, and various integrations. The schemas serve as the single source of truth for entity data structures.

## Features

- Parses `customizations.xml` from Dataverse solution exports
- Generates one JSON Schema document per entity
- Full type mapping from Dataverse types to JSON Schema types
- Inline OptionSet support with `oneOf` and `const/title` patterns
- Configurable attribute filtering (ValidForReadApi, IsRetrievable)
- Optional EventBus envelope schema generation
- Deterministic and reproducible output

## Installation

```bash
# Clone the repository
cd DataverseSchemaGenerator

# Build the project
dotnet build

# Or publish as a single executable
dotnet publish -c Release -r win-x64 --self-contained
```

## Usage

### Basic Usage

```bash
dotnet run -- --input customizations.xml --out ./schemas
```

### Full Example

```bash
dotnet run -- \
  --input customizations.xml \
  --out ./schemas \
  --base-id https://github.com/org/one-crm-json-schemas/schemas/ \
  --generate-events
```

### CLI Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--input` | `-i` | Path to customizations.xml (required) | - |
| `--out` | `-o` | Output directory for schemas | `./schemas` |
| `--base-id` | `-b` | Base URI for `$id` property | `https://schemas.example.com/dataverse/` |
| `--entities` | `-e` | Filter to specific entities (comma-separated) | All entities |
| `--include-non-readable` | - | Include attributes where ValidForReadApi = false | `false` |
| `--include-non-retrievable` | - | Include attributes where IsRetrievable = false | `false` |
| `--generate-events` | - | Generate EventBus envelope schemas | `false` |
| `--compact` | - | Output compact JSON (no indentation) | `false` |

### Examples

Generate schemas for specific entities:
```bash
dotnet run -- -i customizations.xml -o ./schemas -e account,contact
```

Include all attributes (bypass API filters):
```bash
dotnet run -- -i customizations.xml --include-non-readable --include-non-retrievable
```

## Type Mapping

| Dataverse Type | JSON Schema Type |
|---------------|------------------|
| `uniqueidentifier`, `primarykey` | `string` with `format: uuid` |
| `nvarchar`, `string` | `string` with `maxLength` |
| `ntext`, `memo` | `string` with `maxLength` |
| `datetime` | `string` with `format: date-time` |
| `int`, `integer` | `integer` with `minimum`/`maximum` |
| `bigint` | `integer` with `format: int64` |
| `decimal`, `double`, `money` | `number` with `multipleOf` for precision |
| `bit`, `boolean` | `boolean` |
| `picklist`, `state`, `status` | `oneOf` with `const`/`title` (if inline OptionSet) or `integer` |
| `multiselectpicklist` | `array` with `items` containing `oneOf` |
| `lookup` | `string` with `format: uuid` and `x-lookup-target` |
| `owner` | `string` with `format: uuid` and `x-lookup-targets` |
| `customer` | `string` with `format: uuid` and `x-lookup-targets` |

## Output Structure

```
./schemas/
  account.json
  contact.json
  ...
  events/              # Only if --generate-events is specified
    account-event.json
    contact-event.json
    ...
```

## Schema Structure

Each generated entity schema follows this structure:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://example.com/schemas/account.json",
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
      "description": "Select the primary industry for the account.",
      "oneOf": [
        { "const": 1, "title": "Accounting" },
        { "const": 2, "title": "Agriculture" }
      ]
    }
  },
  "required": ["accountid", "name", "ownerid", "statecode"]
}
```

## EventBus Envelope Schema

When `--generate-events` is specified, the generator creates envelope schemas for each entity:

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

## Architecture

The generator follows a clean separation of concerns:

```
DataverseSchemaGenerator/
  Models/
    DataverseMetadata.cs    # Data models for entities and attributes
  Parsing/
    CustomizationsParser.cs # XML parser for customizations.xml
  Schema/
    TypeMapper.cs           # Dataverse to JSON Schema type mapping
    JsonSchemaBuilder.cs    # JSON Schema document builder
  Output/
    SchemaWriter.cs         # File output handler
  Program.cs                # CLI interface
```

## Filtering Rules

By default, only attributes meeting these criteria are included:
- `ValidForReadApi = true`
- `IsRetrievable = true`

Attributes of type `Virtual` and `ManagedProperty` are always excluded.

Use `--include-non-readable` and `--include-non-retrievable` flags to bypass these filters.

## Required Fields

Attributes are marked as `required` in the schema when their `RequiredLevel` is:
- `Required`
- `SystemRequired`

## License

MIT
