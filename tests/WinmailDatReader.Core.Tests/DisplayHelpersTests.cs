using System.Globalization;
using System.Text;

namespace WinmailDatReader.Core.Tests;

public class DisplayHelpersTests
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    [Theory]
    [InlineData(0, "0 Bytes")]
    [InlineData(1, "1 Byte")]
    [InlineData(1023, "1023 Bytes")]
    [InlineData(12595, "12,3 KB")]
    [InlineData(5 * 1024 * 1024, "5,0 MB")]
    [InlineData(3L * 1024 * 1024 * 1024, "3,0 GB")]
    public void FormatSize_is_readable(long bytes, string expected)
        => Assert.Equal(expected, DisplayHelpers.FormatSize(bytes, De));

    [Fact]
    public void Display_priority_is_html_then_rtf_then_text()
    {
        var rtf = Encoding.ASCII.GetBytes("{\\rtf1}");
        Assert.Equal(BodyFormat.Html, DisplayHelpers.ChooseDisplayFormat(new WinmailMessage { HtmlBody = "h", RtfBody = rtf, TextBody = "t" }, true));
        Assert.Equal(BodyFormat.Rtf, DisplayHelpers.ChooseDisplayFormat(new WinmailMessage { RtfBody = rtf, TextBody = "t" }, true));
        Assert.Equal(BodyFormat.Text, DisplayHelpers.ChooseDisplayFormat(new WinmailMessage { TextBody = "t" }, true));
        Assert.Null(DisplayHelpers.ChooseDisplayFormat(new WinmailMessage(), true));
    }

    [Fact]
    public void Without_html_rendering_html_is_only_last_resort()
    {
        Assert.Equal(BodyFormat.Text, DisplayHelpers.ChooseDisplayFormat(new WinmailMessage { HtmlBody = "h", TextBody = "t" }, false));
        Assert.Equal(BodyFormat.Html, DisplayHelpers.ChooseDisplayFormat(new WinmailMessage { HtmlBody = "h" }, false));
    }
}
