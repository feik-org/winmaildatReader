namespace WinmailDatReader.Core.Tests;

public class FileNameSanitizerTests
{
    [Theory]
    [InlineData(@"..\..\autostart.bat", "autostart.bat")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData(@"C:\Windows\evil.dll", "evil.dll")]
    [InlineData("bericht.pdf", "bericht.pdf")]
    [InlineData("Jahresbericht 2026.pdf", "Jahresbericht 2026.pdf")]
    public void Path_components_are_removed(string input, string expected)
        => Assert.Equal(expected, FileNameSanitizer.Sanitize(input));

    [Fact]
    public void Invalid_characters_are_replaced()
    {
        var result = FileNameSanitizer.Sanitize("Rechnung: März?.pdf");

        Assert.Equal("Rechnung_ März_.pdf", result);
        Assert.DoesNotContain(result, c => "<>:\"/\\|?*".Contains(c));
    }

    [Fact]
    public void Control_characters_are_replaced()
        => Assert.Equal("a_b.txt", FileNameSanitizer.Sanitize("a\u0001b.txt"));

    [Theory]
    [InlineData("CON.txt", "_CON.txt")]
    [InlineData("nul", "_nul")]
    [InlineData("com1.log", "_com1.log")]
    [InlineData("CONSOLE.txt", "CONSOLE.txt")]
    public void Reserved_windows_names_are_prefixed(string input, string expected)
        => Assert.Equal(expected, FileNameSanitizer.Sanitize(input));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("..")]
    [InlineData(@"ordner\")]
    [InlineData("???")]
    public void Empty_names_get_fallback(string? input)
        => Assert.Equal(FileNameSanitizer.DefaultName, FileNameSanitizer.Sanitize(input));

    [Fact]
    public void Trailing_dots_and_spaces_are_removed()
        => Assert.Equal("datei.txt", FileNameSanitizer.Sanitize("datei.txt. . "));

    [Fact]
    public void Overlong_names_are_shortened_keeping_extension()
    {
        var result = FileNameSanitizer.Sanitize(new string('a', 500) + ".pdf");

        Assert.True(result.Length <= 200);
        Assert.EndsWith(".pdf", result);
    }
}
