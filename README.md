# winmail.dat Reader

Kleine Windows-Anwendung zum Öffnen von `winmail.dat`-Dateien (TNEF-Format aus Outlook).
Sie zeigt Betreff, Absender, Datum, den Nachrichtentext (HTML, RTF oder Text) und die
enthaltenen Anhänge an und kann Anhänge sowie den Nachrichtentext speichern.

## Voraussetzungen

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (Version laut `global.json`: 10.0.404 oder neuer)
- [Microsoft Edge WebView2-Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) für die
  formatierte Anzeige von HTML-Nachrichten. Unter Windows 10/11 ist sie meist schon installiert.
  Fehlt sie, startet die App trotzdem und zeigt statt des HTML-Texts einen Hinweis an.

## Projektstruktur

| Projekt | Inhalt |
|---|---|
| `src/WinmailDatReader.Core` | Einlesen der TNEF-Daten (über [MimeKit](https://github.com/jstedfast/MimeKit)) und Export von Anhängen/Nachrichtentext |
| `src/WinmailDatReader.App` | WPF-Oberfläche |
| `tests/WinmailDatReader.Core.Tests` | xUnit-Tests für die Core-Bibliothek |

## Bauen und testen

```powershell
dotnet build
dotnet test
```

## Starten

```powershell
# App ohne Datei starten
dotnet run --project src/WinmailDatReader.App

# App direkt mit einer Datei starten
dotnet run --project src/WinmailDatReader.App -- C:\Pfad\zu\winmail.dat
```

Eine Datei lässt sich außerdem über **Datei → Öffnen…** (Strg+O) oder per Drag & Drop ins Fenster öffnen.
Für „Öffnen mit…“ im Explorer kann die gebaute `WinmailDatReader.App.exe` aus
`src/WinmailDatReader.App/bin/<Konfiguration>/net10.0-windows/` ausgewählt werden.

### Beispieldateien

`dotnet test` erzeugt Beispieldateien zum Ausprobieren unter
`tests/WinmailDatReader.Core.Tests/bin/Debug/net10.0/samples/`:

- `sample-html.dat`: HTML-Text mit externem Bild, Link und Skript sowie Anhängen (u. a. doppelter Name und schädlicher Pfad `..\..\autostart.bat`)
- `sample-rtf.dat`: Nachricht nur mit RTF-Text
- `sample-empty.dat`: Nachricht ohne Text und ohne Anhänge
- `keine-tnef.dat`: keine gültige TNEF-Datei, zum Prüfen der Fehlermeldung

## Sicherheit

HTML-Nachrichten werden in einer abgeschotteten WebView2 angezeigt: JavaScript ist deaktiviert,
externe Inhalte (z. B. Tracking-Bilder) werden nicht geladen, und Klicks auf Links lösen keine
Navigation aus. Beim Speichern werden Dateinamen bereinigt (keine Pfadbestandteile, keine unzulässigen
oder reservierten Namen), und bereits vorhandene Dateien werden nicht überschrieben.
