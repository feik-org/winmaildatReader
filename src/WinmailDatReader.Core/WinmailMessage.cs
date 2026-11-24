namespace WinmailDatReader.Core;

/// <summary>Inhalt einer eingelesenen winmail.dat (TNEF).</summary>
public sealed class WinmailMessage
{
    public string? Subject { get; init; }
    public string? Sender { get; init; }
    public DateTimeOffset? Date { get; init; }

    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }

    /// <summary>Dekomprimiertes RTF als Rohbytes (RTF ist byteorientiert, Kodierung steht im RTF selbst).</summary>
    public byte[]? RtfBody { get; init; }

    public IReadOnlyList<WinmailAttachment> Attachments { get; init; } = [];

    public bool HasBody => TextBody != null || HtmlBody != null || RtfBody != null;
}

/// <summary>Ein eingebetteter Anhang.</summary>
public sealed class WinmailAttachment
{
    public WinmailAttachment(string fileName, byte[] content)
    {
        FileName = fileName;
        Content = content;
    }

    /// <summary>Anzeigename (langer Dateiname bzw. Ersatzname). Nicht bereinigt – vor dem Speichern <see cref="FileNameSanitizer"/> verwenden.</summary>
    public string FileName { get; }

    public byte[] Content { get; }

    public long Size => Content.LongLength;
}
