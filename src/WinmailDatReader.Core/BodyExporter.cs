using System.Text;

namespace WinmailDatReader.Core;

public enum BodyFormat
{
    Html,
    Rtf,
    Text,
}

/// <summary>Ermittelt die vorhandenen Formate des Nachrichtentexts und speichert sie.</summary>
public static class BodyExporter
{
    // UTF-8 mit BOM: Browser bzw. Editoren erkennen die Kodierung zuverlässig (BOM hat Vorrang vor meta charset).
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <summary>Vorhandene Formate in Anzeigepriorität (HTML, RTF, Text).</summary>
    public static IReadOnlyList<BodyFormat> GetAvailableFormats(WinmailMessage message)
    {
        var formats = new List<BodyFormat>(3);
        if (message.HtmlBody != null) formats.Add(BodyFormat.Html);
        if (message.RtfBody != null) formats.Add(BodyFormat.Rtf);
        if (message.TextBody != null) formats.Add(BodyFormat.Text);
        return formats;
    }

    public static string GetExtension(BodyFormat format) => format switch
    {
        BodyFormat.Html => ".html",
        BodyFormat.Rtf => ".rtf",
        BodyFormat.Text => ".txt",
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public static string GetDisplayName(BodyFormat format) => format switch
    {
        BodyFormat.Html => "HTML-Datei",
        BodyFormat.Rtf => "RTF-Dokument",
        BodyFormat.Text => "Textdatei",
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    /// <summary>Vorschlag für den Dateinamen, abgeleitet vom Betreff.</summary>
    public static string SuggestFileName(WinmailMessage message, BodyFormat format)
        => FileNameSanitizer.Sanitize(message.Subject, "Nachricht") + GetExtension(format);

    public static void Save(WinmailMessage message, BodyFormat format, string path)
    {
        switch (format)
        {
            case BodyFormat.Html when message.HtmlBody != null:
                File.WriteAllText(path, message.HtmlBody, Utf8WithBom);
                break;
            case BodyFormat.Rtf when message.RtfBody != null:
                File.WriteAllBytes(path, message.RtfBody);
                break;
            case BodyFormat.Text when message.TextBody != null:
                File.WriteAllText(path, message.TextBody, Utf8WithBom);
                break;
            default:
                throw new InvalidOperationException($"Die Nachricht enthält keinen Text im Format {format}.");
        }
    }
}
