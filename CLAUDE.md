# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build DataverseSchemaGenerator/DataverseSchemaGenerator.csproj
dotnet build DataverseSchemaGenerator.Wpf/DataverseSchemaGenerator.Wpf.csproj

# Run console app (default mode - simplest usage)
# Scans Input folder for *.xml files, outputs clean JSON schemas to current directory
dotnet run --project DataverseSchemaGenerator

# Run console app with explicit input file
dotnet run --project DataverseSchemaGenerator -- --input ./samples/customizations.xml --out ./schemas

# Run console app in batch mode (timestamps in filenames, archiving of processed files)
dotnet run --project DataverseSchemaGenerator -- --batch

# Run WPF app (requires .NET 10 preview)
cd DataverseSchemaGenerator.Wpf
dotnet run

# Publish as single executable
dotnet publish DataverseSchemaGenerator/DataverseSchemaGenerator.csproj -c Release -r win-x64 --self-contained
```

**Note:** There are currently no test projects in this solution.

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

CLI using `System.CommandLine`. Entry point is `Program.cs`.

#### Default Mode (Simplest Usage)

By default, the app:
1. Scans the `Input` folder for `*.xml` files
2. Outputs JSON schema files (e.g., `account.json`, `contact.json`) to the current directory
3. Uses clean filenames without timestamps
4. Moves processed XML files to `Input/Archive/`

Just place your `customizations.xml` files in the `Input` folder and run `dotnet run`.

#### Options

- `--input`/`-i` - Path to a specific customizations.xml file (bypasses Input folder scanning)
- `--out`/`-o` - Output directory (default: current directory)
- `--base-id`/`-b` - Base URI for `$id` property (default: `https://schemas.example.com/dataverse/`)
- `--entities`/`-e` - Filter to specific entities (comma-separated)
- `--generate-events` - Generate EventBus envelope schemas
- `--include-non-readable`, `--include-non-retrievable` - Bypass API filters
- `--compact` - Output compact JSON (no indentation)
- `--batch` - Enable batch mode

#### Batch Mode

When `--batch` is specified, the app runs in batch mode:
1. Scans the `Input` folder for all `*.xml` files
2. Processes each file with timestamp-suffixed output filenames (format: `ddMMyy_HHmmss`)
3. Successfully processed files are archived to `Input/Archive/{timestamp}/`
4. Failed XML files are moved to `Input/BadXml/{timestamp}/`

Default output folder in batch mode: `./Output`

Example output filenames in batch mode:
- `account_150126_084512.json`
- `contact_150126_084512.json`

Services:
- **Services/BatchProcessor.cs** - Handles folder scanning, timestamp generation, file archiving, and bad file handling

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
