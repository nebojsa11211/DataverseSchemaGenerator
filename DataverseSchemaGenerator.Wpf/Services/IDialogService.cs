namespace DataverseSchemaGenerator.Wpf.Services;

/// <summary>
/// Service for showing file/folder dialogs.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Show an open file dialog.
    /// </summary>
    string? ShowOpenFileDialog(string title, string filter);

    /// <summary>
    /// Show a folder browser dialog.
    /// </summary>
    string? ShowFolderBrowserDialog(string title);
}
