using System.Text;
using MimeKit.Tnef;

namespace WinmailDatReader.Core.Tests.TestData;

/// <summary>
/// Erzeugt TNEF-Bytes (winmail.dat) zur Laufzeit, damit keine echten E-Mails im Repository liegen.
/// </summary>
public sealed class TnefBuilder
{
    static TnefBuilder() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public string? Subject { get; set; }
    public string? SenderName { get; set; }
    public string? SenderEmail { get; set; }
    public DateTime? SentDate { get; set; }
    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }

    /// <summary>RTF-Text, der als komprimiertes RTF (PR_RTF_COMPRESSED) geschrieben wird.</summary>
    public string? RtfBody { get; set; }

    public List<TestAttachment> Attachments { get; } = [];

    public TnefBuilder WithAttachment(string? shortName, string? longName, byte[] data)
    {
        Attachments.Add(new TestAttachment(shortName, longName, data));
        return this;
    }

    public byte[] Build()
    {
        using var output = new MemoryStream();
        using (var writer = new TnefWriter(output, 1252, 0x1234, leaveOpen: true))
        {
            writer.WriteAttribute(TnefAttributeTag.MessageClass, "IPM.Microsoft Mail.Note");

            using (var props = writer.OpenPropertyWriter(TnefAttributeTag.MapiProperties))
            {
                if (Subject != null)
                {
                    props.WritePropertyTag(TnefPropertyTag.SubjectW);
                    props.WriteValue(Subject);
                }
                if (SenderName != null)
                {
                    props.WritePropertyTag(TnefPropertyTag.SenderNameW);
                    props.WriteValue(SenderName);
                }
                if (SenderEmail != null)
                {
                    props.WritePropertyTag(TnefPropertyTag.SenderEmailAddressW);
                    props.WriteValue(SenderEmail);
                }
                if (SentDate != null)
                {
                    props.WritePropertyTag(TnefPropertyTag.ClientSubmitTime);
                    props.WriteValue(SentDate.Value);
                }
                if (TextBody != null)
                {
                    props.WritePropertyTag(TnefPropertyTag.BodyW);
                    props.WriteValue(TextBody);
                }
                if (HtmlBody != null)
                {
                    props.WritePropertyTag(TnefPropertyTag.BodyHtmlB);
                    props.WriteValue(Encoding.UTF8.GetBytes(HtmlBody));
                }
                if (RtfBody != null)
                {
                    props.WritePropertyTag(TnefPropertyTag.RtfCompressed);
                    using var rtf = props.OpenRtfCompressedStream(true);
                    var bytes = Encoding.ASCII.GetBytes(RtfBody);
                    rtf.Write(bytes, 0, bytes.Length);
                }
            }

            foreach (var attachment in Attachments)
            {
                // RENDDATA: atyp=file(1), ulPosition=-1, dxWidth, dyHeight, dwFlags
                var renddata = new byte[14];
                BitConverter.GetBytes((ushort)1).CopyTo(renddata, 0);
                BitConverter.GetBytes(-1).CopyTo(renddata, 2);
                writer.WriteAttribute(TnefAttributeTag.AttachRenderData, renddata);

                if (attachment.ShortName != null)
                    writer.WriteAttribute(TnefAttributeTag.AttachTitle, attachment.ShortName);

                writer.WriteAttribute(TnefAttributeTag.AttachData, attachment.Data);

                using var props = writer.OpenPropertyWriter(TnefAttributeTag.Attachment);
                props.WritePropertyTag(TnefPropertyTag.AttachMethod);
                props.WriteValue(1); // ATTACH_BY_VALUE
                if (attachment.LongName != null)
                {
                    props.WritePropertyTag(TnefPropertyTag.AttachLongFilenameW);
                    props.WriteValue(attachment.LongName);
                }
                props.WritePropertyTag(TnefPropertyTag.AttachDataBin);
                props.WriteValue(attachment.Data);
            }
        }
        return output.ToArray();
    }
}

public sealed record TestAttachment(string? ShortName, string? LongName, byte[] Data);
