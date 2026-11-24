namespace WinmailDatReader.Core;

/// <summary>Macht Anhangsnamen (aus nicht vertrauenswürdiger Quelle) zu sicheren Windows-Dateinamen.</summary>
public static class FileNameSanitizer
{
    public const string DefaultName = "Anhang";

    private static readonly HashSet<char> InvalidChars =
    [
        .. Path.GetInvalidFileNameChars(),
        // unter Windows unzulässig, auf anderen Plattformen nicht in GetInvalidFileNameChars enthalten
        '<', '>', ':', '"', '/', '\\', '|', '?', '*',
    ];

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    private const int MaxLength = 200;

    public static string Sanitize(string? name, string fallback = DefaultName)
    {
        // Nur den letzten Pfadbestandteil behalten – beide Trenner, unabhängig vom Betriebssystem.
        var value = name ?? string.Empty;
        var lastSeparator = value.LastIndexOfAny(['/', '\\']);
        if (lastSeparator >= 0)
            value = value[(lastSeparator + 1)..];

        var chars = value.Select(c => c < 32 || InvalidChars.Contains(c) ? '_' : c).ToArray();
        value = new string(chars).Trim();

        // Windows entfernt abschließende Punkte/Leerzeichen; führende Punkte vermeiden "..".
        value = value.Trim('.', ' ');

        if (value.Length == 0 || value.All(c => c == '_'))
            value = fallback;

        var stem = value.Split('.')[0].TrimEnd(' ');
        if (ReservedNames.Contains(stem))
            value = "_" + value;

        if (value.Length > MaxLength)
        {
            var ext = Path.GetExtension(value);
            if (ext.Length > 20)
                ext = string.Empty;
            value = value[..(MaxLength - ext.Length)] + ext;
        }

        return value;
    }
}
