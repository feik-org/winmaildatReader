namespace WinmailDatReader.Core.Tests;

public sealed class AttachmentExporterTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("winmail-export-");

    public void Dispose() => _dir.Delete(true);

    private string Dir => _dir.FullName;

    [Fact]
    public void Existing_file_is_kept_and_new_file_gets_suffix()
    {
        var existing = Path.Combine(Dir, "bericht.pdf");
        File.WriteAllBytes(existing, [9, 9]);

        var result = AttachmentExporter.SaveAll([new WinmailAttachment("bericht.pdf", [1, 2, 3])], Dir);

        Assert.False(result.HasFailures);
        Assert.Equal(Path.Combine(Dir, "bericht (2).pdf"), Assert.Single(result.SavedFiles));
        Assert.Equal([9, 9], File.ReadAllBytes(existing));
        Assert.Equal([1, 2, 3], File.ReadAllBytes(Path.Combine(Dir, "bericht (2).pdf")));
    }

    [Fact]
    public void Duplicate_attachment_names_get_unique_names()
    {
        var result = AttachmentExporter.SaveAll(
            [new WinmailAttachment("bild.png", [1]), new WinmailAttachment("bild.png", [2]), new WinmailAttachment("BILD.png", [3])],
            Dir);

        Assert.Equal(
            ["bild.png", "bild (2).png", "BILD (3).png"],
            result.SavedFiles.Select(Path.GetFileName));
        Assert.Equal([2], File.ReadAllBytes(Path.Combine(Dir, "bild (2).png")));
    }

    [Fact]
    public void Saved_files_are_byte_exact()
    {
        var data = new byte[123_457];
        new Random(7).NextBytes(data);

        var result = AttachmentExporter.SaveAll([new WinmailAttachment("daten.bin", data)], Dir);

        Assert.Equal(data, File.ReadAllBytes(Assert.Single(result.SavedFiles)));
    }

    [Fact]
    public void Single_save_writes_byte_exact_to_chosen_path()
    {
        var path = Path.Combine(Dir, "gewählt.dat");
        AttachmentExporter.Save(new WinmailAttachment("x", [5, 6, 7]), path);

        Assert.Equal([5, 6, 7], File.ReadAllBytes(path));
    }

    [Fact]
    public void Path_traversal_names_stay_inside_target_folder()
    {
        var result = AttachmentExporter.SaveAll(
            [new WinmailAttachment(@"..\..\autostart.bat", [1]), new WinmailAttachment("../x.txt", [2])],
            Dir);

        Assert.False(result.HasFailures);
        Assert.All(result.SavedFiles, f => Assert.Equal(Path.GetFullPath(Dir), Path.GetDirectoryName(f)));
        Assert.True(File.Exists(Path.Combine(Dir, "autostart.bat")));
    }

    [Fact]
    public void Invalid_characters_are_saved_inside_target_folder()
    {
        var result = AttachmentExporter.SaveAll([new WinmailAttachment("Rechnung: März?.pdf", [1])], Dir);

        Assert.Equal(Path.Combine(Dir, "Rechnung_ März_.pdf"), Assert.Single(result.SavedFiles));
    }

    [Fact]
    public void Write_errors_are_reported_per_file_without_aborting()
    {
        // Ein "Zielordner", der in Wirklichkeit eine Datei ist, lässt jeden Schreibversuch fehlschlagen.
        var notADirectory = Path.Combine(Dir, "keinordner");
        File.WriteAllBytes(notADirectory, [0]);

        var result = AttachmentExporter.SaveAll(
            [new WinmailAttachment("a.txt", [1]), new WinmailAttachment("b.txt", [2])],
            notADirectory);

        Assert.Empty(result.SavedFiles);
        Assert.Equal(["a.txt", "b.txt"], result.Failures.Select(f => f.FileName));
        Assert.All(result.Failures, f => Assert.False(string.IsNullOrWhiteSpace(f.Error)));
    }

    [Fact]
    public void Contained_path_rejects_escaping_names()
    {
        Assert.Throws<ArgumentException>(() => AttachmentExporter.GetContainedPath(Dir, @"..\evil.txt"));
        Assert.Throws<ArgumentException>(() => AttachmentExporter.GetContainedPath(Dir, @"C:\evil.txt"));
    }
}
