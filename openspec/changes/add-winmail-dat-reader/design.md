# Design

## Context

Das Repository enthält bisher nur die OpenSpec-Struktur; es gibt keinen Code. Auf dem Rechner sind .NET-10-SDKs installiert (u. a. 10.0.404). Die Anwendung wird ausschließlich unter Windows genutzt. Anforderungen: siehe `specs/tnef-parsing`, `specs/message-viewer`, `specs/content-export`.

## Goals / Non-Goals

**Goals:**
- Lese- und Exportlogik ohne UI-Abhängigkeit, damit sie automatisiert testbar ist.
- Schlanke Oberfläche: ein Hauptfenster ohne zusätzliche MVVM-Frameworks.

**Non-Goals:**
- Kein Erzeugen oder Bearbeiten von TNEF-Dateien.
- Keine Anzeige verschachtelter Nachrichten-Anhänge (eingebettete `.msg`) als eigene Nachricht; sie werden als normaler Anhang gespeichert.
- Keine Installer-/MSIX-Paketierung und keine automatische Dateizuordnung für `.dat` (Kommandozeilenargument genügt für „Öffnen mit…“).
- Keine Lokalisierung; die Oberfläche ist deutsch.

## Decisions

### Solution-Aufbau
`WinmailDatReader.sln` im Repository-Root mit:
- `src/WinmailDatReader.Core` (`net10.0`): Einlesen (TNEF → eigenes Modell `WinmailMessage` mit `Subject`, `Sender`, `Date`, `TextBody`, `HtmlBody`, `RtfBody`, `Attachments`) und Exportlogik (Dateinamen bereinigen, eindeutige Namen, Speichern).
- `src/WinmailDatReader.App` (`net10.0-windows`, WPF): Hauptfenster, Dialoge, Anzeige.
- `tests/WinmailDatReader.Core.Tests` (`net10.0`, xUnit).

Alternative: alles in einem Projekt – verworfen, weil Lese- und Exportlogik dann nur schwer ohne UI testbar wäre.

### WPF statt WinForms
WPF bringt mit `RichTextBox` eine eingebaute RTF-Darstellung mit und bietet sauberes Layout/Data-Binding. WinForms wäre ebenfalls möglich, bietet aber keinen Vorteil. Umsetzung mit einem einfachen `MainViewModel` (`INotifyPropertyChanged`, `ICommand`-Hilfsklasse) ohne externe MVVM-Bibliothek.

### TNEF-Parsing mit MimeKit
`MimeKit.Tnef.TnefPart` (`ConvertToMessage()`) bzw. `TnefReader` decken Metadaten, Text/HTML, komprimiertes RTF und Anhänge inklusive langer Dateinamen (`PR_ATTACH_LONG_FILENAME`) ab. Die Core-Bibliothek kapselt MimeKit hinter `WinmailReader.Read(Stream)` und gibt nur das eigene Modell heraus, sodass die Bibliothek austauschbar bleibt.
Prüfung der Signatur `0x223E9F78` erfolgt vorab selbst, um eine klare Fehlermeldung („keine gültige winmail.dat“) statt einer generischen Parser-Ausnahme zu liefern. Parser-Fehler werden in eine eigene `WinmailFormatException` übersetzt.

Alternative: eigener TNEF-Parser – verworfen, da TNEF-/MAPI-Eigenschaften und RTF-Dekompression (LZFu) fehleranfällig sind.

### HTML-Anzeige mit WebView2
`Microsoft.Web.WebView2` rendert HTML mit aktueller Engine (das alte WPF-`WebBrowser`-Control basiert auf Internet Explorer). Externe Inhalte werden blockiert über einen `WebResourceRequested`-Filter für alle URIs (nur der per `NavigateToString` geladene Inhalt wird zugelassen), `IsScriptEnabled = false` und eine Content-Security-Policy-Meta-Angabe, die vor dem Laden eingefügt wird. Navigationen durch Linkklicks werden abgebrochen.
RTF wird in einer schreibgeschützten `RichTextBox`, Klartext in einer schreibgeschützten `TextBox` angezeigt. Die Auswahl des Formats (HTML > RTF > Text) liegt im ViewModel.

### Export
`AttachmentExporter` in Core:
- `SanitizeFileName`: nur den letzten Pfadbestandteil behalten (`/` und `\` als Trenner), `Path.GetInvalidFileNameChars()` und Steuerzeichen durch `_` ersetzen, reservierte Windows-Namen (`CON`, `NUL`, `COM1` …) mit `_` präfixen, leere Namen durch Ersatznamen ersetzen.
- `GetUniquePath`: `name (2).ext`, `name (3).ext` … bis ein freier Name gefunden ist; beim Sammelexport werden zusätzlich die in diesem Lauf bereits vergebenen Namen berücksichtigt.
- Abschließende Prüfung, dass der volle Zielpfad innerhalb des Zielordners liegt.
- Sammelexport liefert ein Ergebnis mit gespeicherten Dateien und Fehlern je Datei, statt beim ersten Fehler abzubrechen.
Der Einzel-Speichern-Dialog nutzt den bereinigten Namen als Vorschlag; Überschreiben bestätigt dort der Standard-Dialog. Ordnerauswahl über `Microsoft.Win32.OpenFolderDialog` (ab .NET 8 in WPF verfügbar).

### Testdaten
Tests erzeugen TNEF-Dateien zur Laufzeit über einen kleinen Test-Builder (Signatur, Attribute mit Level/ID/Länge/Daten/Prüfsumme, MAPI-Eigenschaften), damit keine fremden E-Mails im Repository liegen. Zusätzlich darf eine selbst erzeugte Beispieldatei unter `tests/.../TestData` abgelegt werden.

## Risks / Trade-offs

- [WebView2-Runtime fehlt auf dem Zielrechner] → Unter Windows 11 vorinstalliert; bei Fehler beim Initialisieren wird auf die Anzeige von RTF bzw. Klartext (oder HTML-Quelltext) zurückgefallen und ein Hinweis angezeigt.
- [MimeKit liefert bei einzelnen exotischen TNEF-Dateien unvollständige Daten] → Kapselung hinter eigenem Reader erlaubt später gezielte Nachbesserung über `TnefReader` direkt.
- [Self-contained Test-Builder bildet TNEF nur vereinfacht nach] → Zusätzlich manueller Test mit einer echten Outlook-`winmail.dat` in den Tasks.
- [HTML-Text referenziert eingebettete Bilder per `cid:`] → Werden durch die Blockade nicht angezeigt; Bilder sind als Anhänge speicherbar. Bewusst akzeptiert.
