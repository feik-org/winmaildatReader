# Tasks

## 1. Solution-Gerüst

- [x] 1.1 `WinmailDatReader.sln` mit `src/WinmailDatReader.Core` (net10.0), `src/WinmailDatReader.App` (WPF, net10.0-windows) und `tests/WinmailDatReader.Core.Tests` (xUnit) anlegen, Projektverweise setzen und prüfen, dass `dotnet build` ohne Fehler durchläuft
- [x] 1.2 NuGet-Pakete `MimeKit` (Core) und `Microsoft.Web.WebView2` (App) hinzufügen, `.gitignore` für .NET anlegen und prüfen, dass `dotnet restore` und `dotnet build` erfolgreich sind

## 2. TNEF einlesen (Core)

- [x] 2.1 Test-Builder in `tests/` implementieren, der TNEF-Bytes mit Betreff, Absender, Datum, Text-/HTML-/komprimiertem RTF-Body und Anhängen (kurzer/langer/kein Name) erzeugt; prüfen, dass MimeKit die erzeugten Bytes als TNEF akzeptiert
- [x] 2.2 Modell `WinmailMessage`/`WinmailAttachment` und `WinmailReader.Read(Stream)`/`Read(path)` mit MimeKit implementieren (Metadaten, Bodies inkl. dekomprimiertem RTF, Anhänge mit langem Namen und eindeutigem Ersatznamen); prüfen durch Tests für jedes Szenario aus `specs/tnef-parsing` (Anhangsanzahl, abweichender Dateiname, fehlende Metadaten, nur RTF, kein Body, Namen, byte-genauer Inhalt)
- [x] 2.3 Signaturprüfung und `WinmailFormatException` für Nicht-TNEF- und abgeschnittene Dateien implementieren; prüfen durch Tests mit Textdatei und abgeschnittener TNEF-Datei

## 3. Export (Core)

- [x] 3.1 `SanitizeFileName` implementieren (Pfadbestandteile entfernen, unzulässige Zeichen/Steuerzeichen ersetzen, reservierte Windows-Namen, leere Namen); prüfen durch Tests mit `..\..\autostart.bat`, `Rechnung: März?.pdf`, `CON.txt` und leerem Namen
- [x] 3.2 `GetUniquePath` und Einzel-/Sammelexport mit Ergebnis (gespeicherte Dateien, Fehler je Datei) implementieren inkl. Prüfung, dass der Zielpfad im Zielordner liegt; prüfen durch Tests für vorhandene Datei, doppelte Anhangsnamen, byte-genaue Ausgabe und einen Schreibfehler
- [x] 3.3 Speichern des Nachrichtentexts je Format (`.html`, `.rtf`, `.txt`) inkl. Ermittlung der verfügbaren Formate implementieren; prüfen durch Tests, dass jedes vorhandene Format mit korrekter Endung und Inhalt geschrieben wird

## 4. Oberfläche (App)

- [ ] 4.1 `MainViewModel` mit `RelayCommand` und Hauptfenster-Layout (Menü/Toolbar, Metadatenbereich, Nachrichtentext, Anhangsliste mit formatierter Größe, Hinweise „Keine Anhänge“/„Kein Nachrichtentext vorhanden“) umsetzen; prüfen, dass die App startet und eine Beispieldatei korrekt anzeigt
- [ ] 4.2 Öffnen per Dialog (Filter `*.dat` und alle Dateien), Drag & Drop (erste Datei) und Kommandozeilenargument implementieren, Lesefehler als Meldung anzeigen; prüfen durch manuelles Öffnen über alle drei Wege, Abbrechen des Dialogs und Öffnen einer Textdatei
- [ ] 4.3 Body-Anzeige mit Formatpriorität HTML > RTF > Text umsetzen: WebView2 mit deaktivierten Skripten, blockierten externen Anfragen und abgebrochenen Navigationen; RTF in schreibgeschützter `RichTextBox`; Fallback bei fehlender WebView2-Runtime; prüfen mit HTML-Testnachricht mit externem Bild (keine Anfrage, z. B. per Fiddler/DevTools) und einer reinen RTF-Nachricht
- [ ] 4.4 Speichern-Aktionen anbinden: „Speichern unter…“ und Doppelklick für einzelnen Anhang, „Alle Anhänge speichern“ mit `OpenFolderDialog` und Erfolgs-/Fehlermeldung, „Nachrichtentext speichern“ mit Formatauswahl; Aktionen deaktivieren, wenn nichts vorhanden; prüfen manuell inkl. Speichern in einen schreibgeschützten Ordner
- [x] 4.5 `README.md` mit Build-/Startanleitung (`dotnet run --project src/WinmailDatReader.App -- <datei>`) und Hinweis zur WebView2-Runtime schreiben; prüfen, dass die beschriebenen Befehle wie angegeben funktionieren

## 5. Integration

- [ ] 5.1 Gesamtprüfung: `dotnet test` ist grün, und eine echte Outlook-`winmail.dat` lässt sich öffnen, anzeigen und vollständig exportieren (Anhänge mit Originaldateien vergleichen)

## Workflow follow-up

- Nach Review die Change mit `/opsx:archive` archivieren.
