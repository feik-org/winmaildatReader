using System.Windows;
using Microsoft.Win32;

namespace WinmailDatReader.App;

public interface IDialogService
{
    string? ShowOpenFile();

    /// <summary>Zeigt einen Speichern-Dialog. <paramref name="filterIndex"/> ist 1-basiert (wie in WPF).</summary>
    string? ShowSaveFile(string suggestedName, string filter, ref int filterIndex);

    string? ShowSelectFolder(string title);

    void ShowError(string message);

    void ShowInfo(string message);
}

public sealed class WpfDialogService(Func<Window?> owner) : IDialogService
{
    private const string Caption = "winmail.dat Reader";

    public string? ShowOpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "winmail.dat öffnen",
            Filter = "winmail.dat-Dateien (*.dat)|*.dat|Alle Dateien (*.*)|*.*",
            FilterIndex = 1,
            CheckFileExists = true,
        };
        return dialog.ShowDialog(owner()) == true ? dialog.FileName : null;
    }

    public string? ShowSaveFile(string suggestedName, string filter, ref int filterIndex)
    {
        var dialog = new SaveFileDialog
        {
            FileName = suggestedName,
            Filter = filter,
            FilterIndex = filterIndex,
            AddExtension = true,
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog(owner()) != true)
            return null;
        filterIndex = dialog.FilterIndex;
        return dialog.FileName;
    }

    public string? ShowSelectFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title };
        return dialog.ShowDialog(owner()) == true ? dialog.FolderName : null;
    }

    public void ShowError(string message)
        => MessageBox.Show(owner()!, message, Caption, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string message)
        => MessageBox.Show(owner()!, message, Caption, MessageBoxButton.OK, MessageBoxImage.Information);
}
