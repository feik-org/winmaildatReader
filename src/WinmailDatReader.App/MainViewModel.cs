using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Input;
using WinmailDatReader.Core;

namespace WinmailDatReader.App;

public sealed class AttachmentItem(WinmailAttachment attachment)
{
    public WinmailAttachment Attachment { get; } = attachment;
    public string Name => Attachment.FileName;
    public string SizeText => DisplayHelpers.FormatSize(Attachment.Size);
}

public sealed class MainViewModel : ObservableObject
{
    private readonly IDialogService _dialogs;
    private WinmailMessage? _message;
    private string? _filePath;
    private AttachmentItem? _selectedAttachment;

    public MainViewModel(IDialogService dialogs)
    {
        _dialogs = dialogs;
        OpenCommand = new RelayCommand(OpenWithDialog);
        SaveAttachmentCommand = new RelayCommand(SaveSelectedAttachment, () => SelectedAttachment != null);
        SaveAllAttachmentsCommand = new RelayCommand(SaveAllAttachments, () => HasAttachments);
        SaveBodyCommand = new RelayCommand(SaveBody, () => HasBody);
    }

    public ICommand OpenCommand { get; }
    public ICommand SaveAttachmentCommand { get; }
    public ICommand SaveAllAttachmentsCommand { get; }
    public ICommand SaveBodyCommand { get; }

    public WinmailMessage? Message
    {
        get => _message;
        private set
        {
            if (!SetProperty(ref _message, value))
                return;
            Attachments.Clear();
            foreach (var a in value?.Attachments ?? [])
                Attachments.Add(new AttachmentItem(a));
            SelectedAttachment = Attachments.FirstOrDefault();
            foreach (var name in new[] { nameof(IsLoaded), nameof(SubjectText), nameof(SenderText), nameof(DateText),
                         nameof(HasAttachments), nameof(AttachmentsHeader), nameof(HasBody) })
                OnPropertyChanged(name);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string? FilePath
    {
        get => _filePath;
        private set
        {
            if (SetProperty(ref _filePath, value))
                OnPropertyChanged(nameof(WindowTitle));
        }
    }

    public string WindowTitle => FilePath == null ? "winmail.dat Reader" : $"{Path.GetFileName(FilePath)} – winmail.dat Reader";

    public bool IsLoaded => Message != null;
    public string SubjectText => Message?.Subject ?? "(kein Betreff)";
    public string SenderText => Message?.Sender ?? "(unbekannt)";
    public string DateText => Message?.Date?.ToLocalTime().ToString("dddd, dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture) ?? "(unbekannt)";

    public ObservableCollection<AttachmentItem> Attachments { get; } = [];
    public bool HasAttachments => Attachments.Count > 0;
    public string AttachmentsHeader => $"Anhänge ({Attachments.Count})";
    public bool HasBody => Message?.HasBody == true;

    public AttachmentItem? SelectedAttachment
    {
        get => _selectedAttachment;
        set
        {
            if (SetProperty(ref _selectedAttachment, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    private void OpenWithDialog()
    {
        var path = _dialogs.ShowOpenFile();
        if (path != null)
            Open(path);
    }

    /// <summary>Öffnet eine Datei. Bei Fehlern bleibt die bisherige Anzeige erhalten.</summary>
    public bool Open(string path)
    {
        try
        {
            var message = WinmailReader.Read(path);
            FilePath = Path.GetFullPath(path);
            Message = message;
            return true;
        }
        catch (Exception ex) when (ex is WinmailFormatException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _dialogs.ShowError($"Die Datei „{Path.GetFileName(path)}“ konnte nicht geöffnet werden.\n\n{ex.Message}");
            return false;
        }
    }

    public void SaveSelectedAttachment()
    {
        if (SelectedAttachment is not { } item)
            return;

        var name = FileNameSanitizer.Sanitize(item.Name);
        var ext = Path.GetExtension(name);
        var filter = string.IsNullOrEmpty(ext)
            ? "Alle Dateien (*.*)|*.*"
            : $"{ext.TrimStart('.').ToUpperInvariant()}-Datei (*{ext})|*{ext}|Alle Dateien (*.*)|*.*";
        var index = 1;
        var path = _dialogs.ShowSaveFile(name, filter, ref index);
        if (path == null)
            return;

        try
        {
            AttachmentExporter.Save(item.Attachment, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError($"„{name}“ konnte nicht gespeichert werden.\n\n{ex.Message}");
        }
    }

    private void SaveAllAttachments()
    {
        if (Message is not { } message || message.Attachments.Count == 0)
            return;

        var folder = _dialogs.ShowSelectFolder("Ordner für alle Anhänge auswählen");
        if (folder == null)
            return;

        var result = AttachmentExporter.SaveAll(message.Attachments, folder);
        if (!result.HasFailures)
        {
            _dialogs.ShowInfo($"{result.SavedFiles.Count} {(result.SavedFiles.Count == 1 ? "Datei wurde" : "Dateien wurden")} in „{folder}“ gespeichert.");
            return;
        }

        var text = new StringBuilder();
        text.AppendLine($"{result.SavedFiles.Count} von {message.Attachments.Count} Dateien wurden in „{folder}“ gespeichert.");
        text.AppendLine();
        text.AppendLine("Nicht gespeichert werden konnten:");
        foreach (var failure in result.Failures)
            text.AppendLine($"• {failure.FileName}: {failure.Error}");
        _dialogs.ShowError(text.ToString());
    }

    private void SaveBody()
    {
        if (Message is not { } message)
            return;
        var formats = BodyExporter.GetAvailableFormats(message);
        if (formats.Count == 0)
            return;

        var filter = string.Join("|", formats.Select(f =>
        {
            var ext = BodyExporter.GetExtension(f);
            return $"{BodyExporter.GetDisplayName(f)} (*{ext})|*{ext}";
        }));
        var index = 1;
        var path = _dialogs.ShowSaveFile(Path.GetFileNameWithoutExtension(BodyExporter.SuggestFileName(message, formats[0])), filter, ref index);
        if (path == null)
            return;

        var format = formats[Math.Clamp(index - 1, 0, formats.Count - 1)];
        if (!string.Equals(Path.GetExtension(path), BodyExporter.GetExtension(format), StringComparison.OrdinalIgnoreCase))
            path += BodyExporter.GetExtension(format);

        try
        {
            BodyExporter.Save(message, format, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError($"Der Nachrichtentext konnte nicht gespeichert werden.\n\n{ex.Message}");
        }
    }
}
