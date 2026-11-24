using System.Text;
using WinmailDatReader.Core.Tests.TestData;

namespace WinmailDatReader.Core.Tests;

public class WinmailReaderTests
{
    private static WinmailMessage Read(byte[] bytes) => WinmailReader.Read(new MemoryStream(bytes));

    [Fact]
    public void Valid_file_with_two_attachments_yields_two_attachments()
    {
        var bytes = new TnefBuilder { Subject = "x" }
            .WithAttachment("A.TXT", "a.txt", [1])
            .WithAttachment("B.TXT", "b.txt", [2])
            .Build();

        Assert.Equal(2, Read(bytes).Attachments.Count);
    }

    [Fact]
    public void File_name_does_not_matter()
    {
        var bytes = new TnefBuilder { Subject = "Betreff" }.WithAttachment(null, "a.txt", [1]).Build();
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var winmail = Path.Combine(dir.FullName, "winmail.dat");
            var other = Path.Combine(dir.FullName, "nachricht.bin");
            File.WriteAllBytes(winmail, bytes);
            File.WriteAllBytes(other, bytes);

            var a = WinmailReader.Read(winmail);
            var b = WinmailReader.Read(other);

            Assert.Equal(a.Subject, b.Subject);
            Assert.Equal(a.Attachments.Single().Content, b.Attachments.Single().Content);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [Fact]
    public void Metadata_is_provided_unchanged()
    {
        var date = new DateTime(2026, 3, 14, 15, 9, 26, DateTimeKind.Utc);
        var message = Read(new TnefBuilder
        {
            Subject = "Angebot März",
            SenderName = "Erika Mustermann",
            SenderEmail = "erika@example.org",
            SentDate = date,
        }.Build());

        Assert.Equal("Angebot März", message.Subject);
        Assert.Equal("Erika Mustermann <erika@example.org>", message.Sender);
        Assert.Equal(new DateTimeOffset(date), message.Date);
    }

    [Fact]
    public void Missing_metadata_is_empty_and_read_succeeds()
    {
        var message = Read(new TnefBuilder { TextBody = "nur Text" }.Build());

        Assert.Null(message.Subject);
        Assert.Null(message.Sender);
        Assert.Null(message.Date);
        Assert.Equal("nur Text", message.TextBody);
    }

    [Fact]
    public void Text_and_html_bodies_are_provided()
    {
        var message = Read(new TnefBuilder { TextBody = "Hallo Welt", HtmlBody = "<p>Hallo Welt</p>" }.Build());

        Assert.Equal("Hallo Welt", message.TextBody);
        Assert.Equal("<p>Hallo Welt</p>", message.HtmlBody);
    }

    [Fact]
    public void Compressed_rtf_only_is_decompressed()
    {
        const string rtf = @"{\rtf1\ansi\ansicpg1252 Hallo {\b fett} RTF\par}";
        var message = Read(new TnefBuilder { RtfBody = rtf }.Build());

        Assert.Null(message.TextBody);
        Assert.Null(message.HtmlBody);
        Assert.NotNull(message.RtfBody);
        Assert.Equal(rtf, Encoding.ASCII.GetString(message.RtfBody!));
        Assert.True(message.HasBody);
    }

    [Fact]
    public void No_body_is_reported_and_read_succeeds()
    {
        var message = Read(new TnefBuilder { Subject = "leer" }.Build());

        Assert.False(message.HasBody);
        Assert.Equal("leer", message.Subject);
    }

    [Fact]
    public void Long_file_name_is_preferred_over_short_name()
    {
        var message = Read(new TnefBuilder().WithAttachment("JAHRES~1.PDF", "Jahresbericht 2026.pdf", [1]).Build());

        Assert.Equal("Jahresbericht 2026.pdf", message.Attachments.Single().FileName);
    }

    [Fact]
    public void Short_name_is_used_when_no_long_name_exists()
    {
        var message = Read(new TnefBuilder().WithAttachment("REPORT.PDF", null, [1]).Build());

        Assert.Equal("REPORT.PDF", message.Attachments.Single().FileName);
    }

    [Fact]
    public void Attachments_without_name_get_unique_fallback_names()
    {
        var message = Read(new TnefBuilder()
            .WithAttachment(null, null, [1])
            .WithAttachment(null, "Anhang_1", [2]) // kollidiert mit dem ersten Ersatznamen
            .WithAttachment(null, null, [3])
            .Build());

        var names = message.Attachments.Select(a => a.FileName).ToList();
        Assert.Equal(3, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal("Anhang_1", names[1]);
        Assert.StartsWith("Anhang_", names[0]);
        Assert.StartsWith("Anhang_", names[2]);
    }

    [Fact]
    public void Attachment_content_and_size_are_byte_exact()
    {
        var data = new byte[200_000];
        new Random(42).NextBytes(data);

        var attachment = Read(new TnefBuilder().WithAttachment(null, "blob.bin", data).Build()).Attachments.Single();

        Assert.Equal(data.Length, attachment.Size);
        Assert.Equal(data, attachment.Content);
    }

    [Fact]
    public void Non_tnef_file_is_rejected()
    {
        var ex = Assert.Throws<WinmailFormatException>(() => Read(Encoding.UTF8.GetBytes("Das ist nur eine Textdatei.")));
        Assert.Equal(WinmailReader.NotTnefMessage, ex.Message);
    }

    [Fact]
    public void Empty_file_is_rejected()
    {
        Assert.Throws<WinmailFormatException>(() => Read([]));
    }

    [Fact]
    public void Truncated_file_is_rejected()
    {
        var data = new byte[10_000];
        new Random(1).NextBytes(data);
        var bytes = new TnefBuilder { Subject = "x" }.WithAttachment(null, "groß.bin", data).Build();

        foreach (var cut in new[] { bytes.Length / 2, bytes.Length - 3, 30 })
        {
            var truncated = bytes.AsSpan(0, cut).ToArray();
            var ex = Assert.Throws<WinmailFormatException>(() => Read(truncated));
            Assert.StartsWith(WinmailReader.CorruptMessage, ex.Message);
        }
    }
}
