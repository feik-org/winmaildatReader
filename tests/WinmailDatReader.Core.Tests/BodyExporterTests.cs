using System.Text;

namespace WinmailDatReader.Core.Tests;

public sealed class BodyExporterTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("winmail-body-");

    public void Dispose() => _dir.Delete(true);

    private static readonly byte[] Rtf = Encoding.ASCII.GetBytes(@"{\rtf1\ansi Grüße\par}");

    private static WinmailMessage Full() => new()
    {
        Subject = "Angebot: März?",
        HtmlBody = "<p>Grüße</p>",
        RtfBody = Rtf,
        TextBody = "Grüße",
    };

    [Fact]
    public void Available_formats_are_in_priority_order()
    {
        Assert.Equal([BodyFormat.Html, BodyFormat.Rtf, BodyFormat.Text], BodyExporter.GetAvailableFormats(Full()));
        Assert.Equal([BodyFormat.Text], BodyExporter.GetAvailableFormats(new WinmailMessage { TextBody = "x" }));
        Assert.Empty(BodyExporter.GetAvailableFormats(new WinmailMessage()));
    }

    [Theory]
    [InlineData(BodyFormat.Html, ".html")]
    [InlineData(BodyFormat.Rtf, ".rtf")]
    [InlineData(BodyFormat.Text, ".txt")]
    public void Each_format_is_written_with_matching_extension_and_content(BodyFormat format, string extension)
    {
        var message = Full();
        var name = BodyExporter.SuggestFileName(message, format);
        Assert.EndsWith(extension, name);
        Assert.Equal("Angebot_ März_" + extension, name);

        var path = Path.Combine(_dir.FullName, name);
        BodyExporter.Save(message, format, path);

        switch (format)
        {
            case BodyFormat.Html:
                Assert.Equal("<p>Grüße</p>", File.ReadAllText(path, Encoding.UTF8));
                break;
            case BodyFormat.Rtf:
                Assert.Equal(Rtf, File.ReadAllBytes(path));
                break;
            case BodyFormat.Text:
                Assert.Equal("Grüße", File.ReadAllText(path, Encoding.UTF8));
                break;
        }
    }

    [Fact]
    public void Suggested_name_without_subject_uses_fallback()
        => Assert.Equal("Nachricht.txt", BodyExporter.SuggestFileName(new WinmailMessage { TextBody = "x" }, BodyFormat.Text));

    [Fact]
    public void Saving_missing_format_throws()
        => Assert.Throws<InvalidOperationException>(() =>
            BodyExporter.Save(new WinmailMessage { TextBody = "x" }, BodyFormat.Html, Path.Combine(_dir.FullName, "a.html")));
}
