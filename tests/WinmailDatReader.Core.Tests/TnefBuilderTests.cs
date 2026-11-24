using MimeKit.Tnef;
using WinmailDatReader.Core.Tests.TestData;

namespace WinmailDatReader.Core.Tests;

public class TnefBuilderTests
{
    [Fact]
    public void Built_bytes_are_accepted_by_MimeKit()
    {
        var bytes = new TnefBuilder
        {
            Subject = "Hallo",
            SenderName = "Max Muster",
            SentDate = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            TextBody = "Text",
            HtmlBody = "<p>Html</p>",
            RtfBody = @"{\rtf1\ansi Hallo RTF}",
        }
        .WithAttachment("BERICH~1.PDF", "Bericht 2026.pdf", [1, 2, 3])
        .WithAttachment(null, null, [4, 5])
        .Build();

        using var message = TnefMessage.Load(new MemoryStream(bytes), TnefOptions.Default, default);

        Assert.Equal("Hallo", message.Subject);
        Assert.NotNull(message.TextBody);
        Assert.NotNull(message.HtmlBody);
        Assert.NotNull(message.RtfBody);
        Assert.Equal(2, message.Attachments.Count);
        Assert.Equal("Bericht 2026.pdf", message.Attachments[0].FileName);
    }
}
