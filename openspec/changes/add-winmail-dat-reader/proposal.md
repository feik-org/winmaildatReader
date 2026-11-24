# Proposal

## Why

Outlook verschickt Nachrichten häufig im TNEF-Format, sodass Empfänger ohne Outlook nur einen unlesbaren `winmail.dat`-Anhang erhalten. Es soll eine einfache Windows-Anwendung entstehen, mit der man eine solche Datei öffnen, ihren Inhalt (Nachrichtentext und eingebettete Anhänge) ansehen und die Inhalte auf der Festplatte speichern kann.

## What Changes

- Neue .NET-10-Solution `WinmailDatReader.sln` mit einer Windows-Desktop-Anwendung (WPF), einer Klassenbibliothek für das Auslesen der TNEF-Daten und einem Testprojekt.
- Öffnen einer `winmail.dat`-Datei (bzw. beliebiger TNEF-Datei) über einen Datei-öffnen-Dialog, per Drag & Drop oder als Kommandozeilenargument (z. B. über „Öffnen mit…“).
- Auslesen von Nachrichten-Metadaten (Betreff, Absender, Datum, soweit vorhanden), des Nachrichtentexts (Text, HTML oder RTF) und aller eingebetteten Anhänge.
- Anzeige der Inhalte: Metadaten, Nachrichtentext und eine Liste der Anhänge mit Dateiname und Größe.
- Speichern: einzelne Anhänge, alle Anhänge auf einmal in einen Ordner sowie den Nachrichtentext als Datei.
- Verständliche Fehlermeldung, wenn die Datei kein gültiges TNEF-Format hat.

## Capabilities

### New Capabilities
- `tnef-parsing`: Lesen einer TNEF-Datei (`winmail.dat`) und Bereitstellen von Metadaten, Nachrichtentext und Anhängen als Datenmodell, inklusive Fehlerbehandlung für ungültige Dateien.
- `message-viewer`: Desktop-Oberfläche zum Öffnen einer Datei (Dialog, Drag & Drop, Kommandozeile) und Anzeigen von Metadaten, Nachrichtentext und Anhangsliste.
- `content-export`: Speichern einzelner oder aller Anhänge sowie des Nachrichtentexts auf das Dateisystem, inklusive Behandlung von Namenskonflikten und unzulässigen Dateinamen.

### Modified Capabilities
- (keine)

## Impact

- Neues Projekt ohne bestehenden Code; alle Dateien werden neu angelegt.
- Zielplattform: Windows, .NET 10 (`net10.0-windows`), SDK ist lokal installiert.
- Neue Abhängigkeit: NuGet-Paket `MimeKit` (enthält einen erprobten TNEF-Reader), für Tests `xunit`.
