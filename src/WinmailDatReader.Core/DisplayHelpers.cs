using System.Globalization;

namespace WinmailDatReader.Core;

public static class DisplayHelpers
{
    /// <summary>Formatiert eine Größe lesbar, z. B. „12,3 KB“.</summary>
    public static string FormatSize(long bytes, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (bytes < 1024)
            return bytes == 1 ? "1 Byte" : $"{bytes.ToString(culture)} Bytes";

        string[] units = ["KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = -1;
        do
        {
            value /= 1024;
            unit++;
        }
        while (value >= 1024 && unit < units.Length - 1);

        return $"{value.ToString("0.0", culture)} {units[unit]}";
    }

    /// <summary>
    /// Wählt das anzuzeigende Format: HTML vor RTF vor Text. Ist keine HTML-Darstellung möglich,
    /// wird HTML nur noch als letzte Möglichkeit (als Quelltext) gewählt.
    /// </summary>
    public static BodyFormat? ChooseDisplayFormat(WinmailMessage message, bool htmlRenderingAvailable)
    {
        var formats = BodyExporter.GetAvailableFormats(message);
        if (formats.Count == 0)
            return null;
        if (htmlRenderingAvailable || formats.Count == 1)
            return formats[0];
        return formats.FirstOrDefault(f => f != BodyFormat.Html);
    }
}
