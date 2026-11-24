using System.Text;
using WinmailDatReader.Core.Tests.TestData;

namespace WinmailDatReader.Core.Tests;

/// <summary>
/// Erzeugt Beispieldateien für manuelle Tests der App unter <c>bin/.../samples</c>
/// und prüft, dass sie sich einlesen lassen.
/// </summary>
public class SampleFileTests
{
    public static string SampleDirectory => Path.Combine(AppContext.BaseDirectory, "samples");

    [Fact]
    public void Sample_files_are_generated_and_readable()
    {
        Directory.CreateDirectory(SampleDirectory);
        var pdfLike = Encoding.ASCII.GetBytes("%PDF-1.4\n% Beispiel\n");
        var image = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        var samples = new Dictionary<string, TnefBuilder>
        {
            ["sample-html.dat"] = new TnefBuilder
            {
                Subject = "Beispiel mit HTML und externem Bild",
                SenderName = "Erika Mustermann",
                SenderEmail = "erika@example.org",
                SentDate = new DateTime(2026, 11, 20, 9, 30, 0, DateTimeKind.Utc),
                TextBody = "Hallo,\r\nanbei der Bericht.\r\n",
                HtmlBody = "<html><body><h2>Hallo,</h2><p>anbei der <b>Bericht</b>.</p>" +
                           "<p><img src=\"https://example.com/tracking.png\" alt=\"externes Bild\"></p>" +
                           "<p><a href=\"https://example.com\">Link</a></p>" +
                           "<script>document.body.innerHTML='SKRIPT AUSGEFÜHRT'</script></body></html>",
            }
            .WithAttachment("BERICH~1.PDF", "Bericht 2026.pdf", pdfLike)
            .WithAttachment(null, "bild.png", image)
            .WithAttachment(null, "bild.png", image)
            .WithAttachment(null, @"..\..\autostart.bat", Encoding.ASCII.GetBytes("echo harmlos")),

            ["sample-rtf.dat"] = new TnefBuilder
            {
                Subject = "Nur RTF, keine Anhänge",
                SenderName = "Max Mustermann",
                RtfBody = @"{\rtf1\ansi\ansicpg1252{\fonttbl{\f0 Arial;}}\f0\fs22 Hallo {\b fett} und {\i kursiv}.\par Gr\'fc\'dfe\par}",
            },

            ["sample-empty.dat"] = new TnefBuilder(),
        };

        foreach (var (name, builder) in samples)
        {
            var path = Path.Combine(SampleDirectory, name);
            File.WriteAllBytes(path, builder.Build());
            Assert.NotNull(WinmailReader.Read(path));
        }

        File.WriteAllText(Path.Combine(SampleDirectory, "keine-tnef.dat"), "Das ist keine winmail.dat.");
    }
}
