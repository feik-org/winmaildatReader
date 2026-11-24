using System.Buffers.Binary;
using System.Text;
using MimeKit.Tnef;

namespace WinmailDatReader.Core;

/// <summary>Liest TNEF-Dateien (winmail.dat) in ein <see cref="WinmailMessage"/>.</summary>
public static class WinmailReader
{
    private const uint TnefSignature = 0x223E9F78;

    public const string NotTnefMessage = "Die Datei ist keine gültige winmail.dat/TNEF-Datei.";
    public const string CorruptMessage = "Die winmail.dat ist beschädigt oder unvollständig und kann nicht gelesen werden.";

    static WinmailReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static WinmailMessage Read(string path)
    {
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    public static WinmailMessage Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // In den Speicher laden: erlaubt die Signaturprüfung vorab und entkoppelt vom Dateihandle.
        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        buffer.Position = 0;

        if (buffer.Length < 4 || BinaryPrimitives.ReadUInt32LittleEndian(buffer.GetBuffer().AsSpan(0, 4)) != TnefSignature)
            throw new WinmailFormatException(NotTnefMessage);

        try
        {
            using var reader = new TnefReader(buffer, TnefOptions.Default, leaveOpen: false)
            {
                ComplianceLogger = new StrictComplianceLogger(),
            };
            using var tnef = TnefMessage.Load(reader, default);
            return Convert(tnef);
        }
        catch (WinmailFormatException)
        {
            throw;
        }
        catch (Exception ex) when (ex is TnefException or EndOfStreamException or InvalidDataException or FormatException or ArgumentException)
        {
            throw new WinmailFormatException(CorruptMessage, ex);
        }
    }

    private static WinmailMessage Convert(TnefMessage tnef)
    {
        var props = tnef.Properties;

        return new WinmailMessage
        {
            Subject = NullIfEmpty(tnef.Subject) ?? NullIfEmpty(GetString(props, TnefPropertyTag.SubjectW)),
            Sender = GetSender(props),
            Date = GetDate(props),
            TextBody = NullIfEmpty(tnef.TextBody?.GetText()),
            HtmlBody = NullIfEmpty(tnef.HtmlBody?.GetText()),
            RtfBody = GetRtf(tnef.RtfBody),
            Attachments = GetAttachments(tnef),
        };
    }

    private static string? GetSender(TnefPropertySet props)
    {
        var name = NullIfEmpty(GetString(props, TnefPropertyTag.SenderNameW))
                   ?? NullIfEmpty(GetString(props, TnefPropertyTag.SentRepresentingNameW));
        var email = NullIfEmpty(GetString(props, TnefPropertyTag.SenderSmtpAddressW))
                    ?? NullIfEmpty(GetString(props, TnefPropertyTag.SenderEmailAddressW));

        // Exchange-interne Adressen (/O=...) sind für Benutzer nicht hilfreich.
        if (email != null && email.StartsWith('/'))
            email = null;

        if (name != null && email != null && !string.Equals(name, email, StringComparison.OrdinalIgnoreCase))
            return $"{name} <{email}>";
        return name ?? email;
    }

    private static DateTimeOffset? GetDate(TnefPropertySet props)
    {
        foreach (var tag in new[] { TnefPropertyTag.ClientSubmitTime, TnefPropertyTag.MessageDeliveryTime })
        {
            if (props.TryGetValue(tag, out var prop) && prop.TryGetDateTime(out var value))
                return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }
        return null;
    }

    private static byte[]? GetRtf(TnefMessageBody? body)
    {
        if (body == null)
            return null;

        using var decoded = body.OpenDecodedRead();
        using var ms = new MemoryStream();
        decoded.CopyTo(ms);
        return ms.Length == 0 ? null : ms.ToArray();
    }

    private static List<WinmailAttachment> GetAttachments(TnefMessage tnef)
    {
        var names = tnef.Attachments.Select(a => NullIfEmpty(a.FileName?.Trim())).ToList();
        var used = new HashSet<string>(names.Where(n => n != null)!, StringComparer.OrdinalIgnoreCase);
        var result = new List<WinmailAttachment>(names.Count);
        var fallbackCounter = 0;

        for (var i = 0; i < tnef.Attachments.Count; i++)
        {
            var attachment = tnef.Attachments[i];
            var name = names[i];
            if (name == null)
            {
                do name = $"Anhang_{++fallbackCounter}";
                while (!used.Add(name));
            }

            using var content = attachment.OpenRead();
            using var ms = new MemoryStream();
            content.CopyTo(ms);

            if (attachment.HasContent && ms.Length != attachment.Length)
                throw new WinmailFormatException(CorruptMessage);

            result.Add(new WinmailAttachment(name, ms.ToArray()));
        }
        return result;
    }

    private static string? GetString(TnefPropertySet props, TnefPropertyTag tag)
        => props.TryGetValue(tag, out var prop) && prop.TryGetString(out var value) ? value : null;

    private static string? NullIfEmpty(string? value)
    {
        value = value?.TrimEnd('\0');
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>Bricht das Einlesen bei Verstößen ab, durch die Inhalte fehlen oder unvollständig wären.</summary>
    private sealed class StrictComplianceLogger : ITnefComplianceLogger
    {
        public void Log(in TnefComplianceIssue issue)
        {
            switch (issue.Violation)
            {
                case TnefComplianceViolation.InvalidSignature:
                    throw new WinmailFormatException(NotTnefMessage);
                case TnefComplianceViolation.TruncatedStream:
                case TnefComplianceViolation.InvalidAttributeLength:
                case TnefComplianceViolation.InvalidPropertyLength:
                case TnefComplianceViolation.InvalidPropertyCount:
                case TnefComplianceViolation.InvalidValueCount:
                case TnefComplianceViolation.DataSizeLimitExceeded:
                case TnefComplianceViolation.TooManyAttachments:
                    throw new WinmailFormatException($"{CorruptMessage} ({issue.Description})");
            }
        }
    }
}
