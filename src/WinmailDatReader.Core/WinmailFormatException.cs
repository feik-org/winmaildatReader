namespace WinmailDatReader.Core;

/// <summary>Die Datei ist keine gültige oder eine beschädigte winmail.dat/TNEF-Datei.</summary>
public sealed class WinmailFormatException : Exception
{
    public WinmailFormatException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
