using Microsoft.Win32;

namespace DataverseSchemaGenerator.Wpf.Services;

/// <summary>
/// Implementation of IDialogService using WPF dialogs.
/// </summary>
public class DialogService : IDialogService
{
    public string? ShowOpenFileDialog(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ShowFolderBrowserDialog(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
