# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build DataverseSchemaGenerator/DataverseSchemaGenerator.csproj
dotnet build DataverseSchemaGenerator.Wpf/DataverseSchemaGenerator.Wpf.csproj

# Run console app
cd DataverseSchemaGenerator
dotnet run -- --input ../samples/customizations.xml --out ./schemas --base-id https://example.com/schemas/

# Run WPF app
cd DataverseSchemaGenerator.Wpf
dotnet run
```

## Architecture

This solution generates JSON Schema (Draft 7) documents from Microsoft Dataverse `customizations.xml` exports.

### Project Structure

```
DataverseSchemaGenerator.sln
├── DataverseSchemaGenerator.Core/     # Shared library (.NET 8)
├── DataverseSchemaGenerator/          # Console app (.NET 8)
└── DataverseSchemaGenerator.Wpf/      # WPF app with MVVM (.NET 10-windows)
```

### Core Library (`DataverseSchemaGenerator.Core`)

Contains all business logic, referenced by both Console and WPF apps:

- **Models/DataverseMetadata.cs** - `EntityMetadata`, `AttributeMetadata`, `OptionSetValue`, `GeneratorOptions`, enums
- **Parsing/CustomizationsParser.cs** - XML parser using `System.Xml.Linq`
- **Schema/TypeMapper.cs** - Dataverse → JSON Schema type mapping (static class)
- **Schema/JsonSchemaBuilder.cs** - Builds JSON Schema documents with filtering
- **Output/SchemaWriter.cs** - Writes schemas to filesystem using `Utf8JsonWriter`
- **Services/SchemaGeneratorService.cs** - Orchestrates parsing and generation with progress events

### Console App (`DataverseSchemaGenerator`)

CLI using `System.CommandLine`. Entry point is `Program.cs` with standard command-line options for input/output paths, filtering, and event generation.

### WPF App (`DataverseSchemaGenerator.Wpf`)

MVVM pattern using `CommunityToolkit.Mvvm`:

- **ViewModels/MainViewModel.cs** - Uses `[ObservableProperty]`, `[RelayCommand]` source generators
- **ViewModels/EntityViewModel.cs** - Wraps `EntityMetadata` with selection state
- **Services/IDialogService.cs** - Abstraction for file/folder dialogs
- **MainWindow.xaml** - Data-bound to `MainViewModel`

## Type Mapping Reference

Key mappings from Dataverse to JSON Schema:

| Dataverse | JSON Schema |
|-----------|-------------|
| `uniqueidentifier` | `string` + `format: uuid` |
| `nvarchar`, `memo` | `string` + `maxLength` |
| `datetime` | `string` + `format: date-time` |
| `int`, `bigint` | `integer` |
| `decimal`, `money` | `number` + `multipleOf` |
| `picklist`, `state`, `status` | `oneOf` with `const`/`title` or `integer` |
| `lookup`, `owner`, `customer` | `string` + `format: uuid` + `x-lookup-target(s)` |

## Filtering Defaults

Attributes are included only when `ValidForReadApi = true` AND `IsRetrievable = true`. Attributes with type `Virtual` or `ManagedProperty` are always excluded.
