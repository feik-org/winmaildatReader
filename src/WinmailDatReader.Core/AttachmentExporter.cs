namespace WinmailDatReader.Core;

/// <summary>Ergebnis eines Sammelexports.</summary>
public sealed class ExportResult
{
    public List<string> SavedFiles { get; } = [];
    public List<ExportFailure> Failures { get; } = [];
    public bool HasFailures => Failures.Count > 0;
}

public sealed record ExportFailure(string FileName, string Error);

/// <summary>Speichert Anhänge auf das Dateisystem.</summary>
public static class AttachmentExporter
{
    /// <summary>
    /// Liefert einen freien Pfad im Zielordner der Form <c>name.ext</c>, <c>name (2).ext</c>, ….
    /// <paramref name="reserved"/> enthält in diesem Lauf bereits vergebene Pfade und wird ergänzt.
    /// </summary>
    public static string GetUniquePath(string directory, string fileName, ISet<string>? reserved = null)
    {
        var safeName = FileNameSanitizer.Sanitize(fileName);
        var stem = Path.GetFileNameWithoutExtension(safeName);
        var ext = Path.GetExtension(safeName);

        for (var i = 1; ; i++)
        {
            var candidate = i == 1 ? safeName : $"{stem} ({i}){ext}";
            var path = GetContainedPath(directory, candidate);
            if (File.Exists(path) || Directory.Exists(path) || (reserved != null && reserved.Contains(path)))
                continue;
            reserved?.Add(path);
            return path;
        }
    }

    /// <summary>Speichert einen Anhang byte-genau unter dem vom Benutzer gewählten Pfad (überschreibt).</summary>
    public static void Save(WinmailAttachment attachment, string path)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        File.WriteAllBytes(path, attachment.Content);
    }

    /// <summary>Speichert alle Anhänge in den Ordner, ohne vorhandene Dateien zu überschreiben.</summary>
    public static ExportResult SaveAll(IEnumerable<WinmailAttachment> attachments, string directory)
    {
        var result = new ExportResult();
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var attachment in attachments)
        {
            try
            {
                var path = GetUniquePath(directory, attachment.FileName, reserved);
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
                    stream.Write(attachment.Content);
                result.SavedFiles.Add(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                result.Failures.Add(new ExportFailure(attachment.FileName, ex.Message));
            }
        }
        return result;
    }

    /// <summary>Kombiniert Ordner und Dateiname und stellt sicher, dass das Ergebnis im Ordner liegt.</summary>
    internal static string GetContainedPath(string directory, string fileName)
    {
        var root = Path.GetFullPath(directory);
        var rootWithSeparator = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(root, fileName));

        if (!full.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ||
            Path.GetDirectoryName(full) is not { } parent ||
            !string.Equals(Path.TrimEndingDirectorySeparator(parent), Path.TrimEndingDirectorySeparator(root), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Der Dateiname „{fileName}“ würde außerhalb des Zielordners gespeichert.", nameof(fileName));
        }
        return full;
    }
}
