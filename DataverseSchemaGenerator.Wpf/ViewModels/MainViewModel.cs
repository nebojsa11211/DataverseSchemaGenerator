using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataverseSchemaGenerator.Core.Models;
using DataverseSchemaGenerator.Core.Services;
using DataverseSchemaGenerator.Wpf.Services;

namespace DataverseSchemaGenerator.Wpf.ViewModels;

/// <summary>
/// Main ViewModel for the application.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly SchemaGeneratorService _generatorService;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainViewModel() : this(new DialogService())
    {
    }

    public MainViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        _generatorService = new SchemaGeneratorService();
        _generatorService.ProgressChanged += OnProgressChanged;
    }

    // Input/Output paths
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadFileCommand))]
    private string _inputFilePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateSchemasCommand))]
    private string _outputDirectoryPath = string.Empty;

    [ObservableProperty]
    private string _baseId = "https://schemas.example.com/dataverse/";

    // Options
    [ObservableProperty]
    private bool _generateEventEnvelopes;

    [ObservableProperty]
    private bool _compactOutput;

    [ObservableProperty]
    private bool _includeNonReadable;

    [ObservableProperty]
    private bool _includeNonRetrievable;

    // Entities
    public ObservableCollection<EntityViewModel> Entities { get; } = [];

    // Log output
    public ObservableCollection<string> LogMessages { get; } = [];

    // Status
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateSchemasCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseInputFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseOutputDirectoryCommand))]
    private bool _isGenerating;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Commands
    [RelayCommand]
    private void BrowseInputFile()
    {
        var path = _dialogService.ShowOpenFileDialog(
            "Select customizations.xml",
            "XML Files (*.xml)|*.xml|All Files (*.*)|*.*");

        if (!string.IsNullOrEmpty(path))
        {
            InputFilePath = path;
        }
    }

    [RelayCommand]
    private void BrowseOutputDirectory()
    {
        var path = _dialogService.ShowFolderBrowserDialog("Select Output Directory");

        if (!string.IsNullOrEmpty(path))
        {
            OutputDirectoryPath = path;
        }
    }

    private bool CanLoadFile() => !string.IsNullOrEmpty(InputFilePath) && !IsGenerating;

    [RelayCommand(CanExecute = nameof(CanLoadFile))]
    private void LoadFile()
    {
        try
        {
            LogMessages.Clear();

            // Unsubscribe from existing entities before clearing
            foreach (var entity in Entities)
            {
                entity.PropertyChanged -= OnEntityPropertyChanged;
            }
            Entities.Clear();

            if (!File.Exists(InputFilePath))
            {
                AddLog($"Error: File not found: {InputFilePath}");
                StatusMessage = "File not found";
                return;
            }

            AddLog($"Loading: {InputFilePath}");
            var entities = _generatorService.ParseEntities(InputFilePath);

            foreach (var entity in entities)
            {
                var viewModel = new EntityViewModel(entity);
                viewModel.PropertyChanged += OnEntityPropertyChanged;
                Entities.Add(viewModel);
            }

            AddLog($"Found {entities.Count} entities");
            StatusMessage = $"Loaded {entities.Count} entities";

            // Set default output path if empty
            if (string.IsNullOrEmpty(OutputDirectoryPath))
            {
                var dir = Path.GetDirectoryName(InputFilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    OutputDirectoryPath = Path.Combine(dir, "schemas");
                }
            }
        }
        catch (Exception ex)
        {
            AddLog($"Error: {ex.Message}");
            StatusMessage = "Error loading file";
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var entity in Entities)
        {
            entity.IsSelected = true;
        }
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var entity in Entities)
        {
            entity.IsSelected = false;
        }
    }

    private bool CanGenerateSchemas() =>
        !IsGenerating &&
        !string.IsNullOrEmpty(OutputDirectoryPath) &&
        Entities.Any(e => e.IsSelected);

    [RelayCommand(CanExecute = nameof(CanGenerateSchemas))]
    private async Task GenerateSchemasAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            IsGenerating = true;
            StatusMessage = "Generating schemas...";

            var selectedEntities = Entities
                .Where(e => e.IsSelected)
                .Select(e => e.Entity)
                .ToList();

            AddLog($"Generating schemas for {selectedEntities.Count} entities...");

            var options = new GeneratorOptions
            {
                InputPath = InputFilePath,
                OutputPath = OutputDirectoryPath,
                BaseId = BaseId,
                FilterValidForReadApi = !IncludeNonReadable,
                FilterIsRetrievable = !IncludeNonRetrievable,
                GenerateEventEnvelopes = GenerateEventEnvelopes,
                PrettyPrint = !CompactOutput
            };

            var result = await _generatorService.GenerateAsync(
                options,
                selectedEntities,
                _cancellationTokenSource.Token);

            AddLog($"Successfully generated {result.EntitySchemasGenerated} entity schemas");
            if (GenerateEventEnvelopes)
            {
                AddLog($"Successfully generated {result.EventSchemasGenerated} event schemas");
            }
            AddLog($"Output: {OutputDirectoryPath}");

            StatusMessage = $"Done! Generated {result.EntitySchemasGenerated} schemas";
        }
        catch (OperationCanceledException)
        {
            AddLog("Operation cancelled");
            StatusMessage = "Cancelled";
        }
        catch (Exception ex)
        {
            AddLog($"Error: {ex.Message}");
            StatusMessage = "Error generating schemas";
        }
        finally
        {
            IsGenerating = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelGeneration()
    {
        _cancellationTokenSource?.Cancel();
    }

    private void OnProgressChanged(string message)
    {
        // Ensure we're on the UI thread
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            AddLog(message);
        });
    }

    private void OnEntityPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EntityViewModel.IsSelected))
        {
            GenerateSchemasCommand.NotifyCanExecuteChanged();
        }
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogMessages.Add($"[{timestamp}] {message}");
    }
}
