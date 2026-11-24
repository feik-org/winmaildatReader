# Spec Delta

## Purpose

Ermöglicht es, die aus einer winmail.dat gelesenen Anhänge und den Nachrichtentext sicher als Dateien auf dem Dateisystem zu speichern.

## ADDED Requirements

### Requirement: Einzelnen Anhang speichern
Die Anwendung SHALL das Speichern eines ausgewählten Anhangs über einen Speichern-Dialog anbieten, der den Anhangsnamen vorschlägt. Die gespeicherte Datei MUST byte-genau dem Anhangsinhalt entsprechen.

#### Scenario: Anhang speichern
- **WHEN** der Benutzer einen Anhang auswählt, „Speichern unter…“ wählt und einen Zielpfad bestätigt
- **THEN** wird der Anhang byte-genau unter diesem Pfad gespeichert

#### Scenario: Speichern per Doppelklick
- **WHEN** der Benutzer doppelt auf einen Anhang in der Liste klickt
- **THEN** öffnet sich der Speichern-Dialog für diesen Anhang

### Requirement: Alle Anhänge speichern
Die Anwendung SHALL das Speichern aller Anhänge in einen vom Benutzer gewählten Ordner anbieten und danach melden, wie viele Dateien gespeichert wurden.

#### Scenario: Alle Anhänge in Ordner speichern
- **WHEN** der Benutzer „Alle Anhänge speichern“ wählt und einen Ordner auswählt
- **THEN** werden alle Anhänge dort gespeichert und die Anzahl wird gemeldet

#### Scenario: Keine Anhänge vorhanden
- **WHEN** die geöffnete Nachricht keine Anhänge enthält
- **THEN** ist die Aktion „Alle Anhänge speichern“ nicht verfügbar

### Requirement: Namenskonflikte beim Sammelexport auflösen
Beim Speichern aller Anhänge MUST die Anwendung vorhandene Dateien und gleichnamige Anhänge erhalten, indem sie eindeutige Namen der Form `name (2).ext` vergibt.

#### Scenario: Datei existiert bereits
- **WHEN** im Zielordner bereits `bericht.pdf` existiert und ein Anhang `bericht.pdf` gespeichert wird
- **THEN** wird der Anhang als `bericht (2).pdf` gespeichert und die vorhandene Datei bleibt unverändert

#### Scenario: Zwei Anhänge mit gleichem Namen
- **WHEN** eine Nachricht zwei Anhänge namens `bild.png` enthält
- **THEN** werden sie als `bild.png` und `bild (2).png` gespeichert

### Requirement: Sichere Dateinamen
Die Anwendung MUST unter Windows unzulässige Zeichen in Anhangsnamen ersetzen und Pfadbestandteile entfernen, sodass niemals außerhalb des gewählten Zielordners geschrieben wird.

#### Scenario: Anhangsname mit Pfad
- **WHEN** ein Anhang den Namen `..\..\autostart.bat` trägt
- **THEN** wird er als `autostart.bat` innerhalb des Zielordners gespeichert

#### Scenario: Unzulässige Zeichen
- **WHEN** ein Anhang den Namen `Rechnung: März?.pdf` trägt
- **THEN** werden die unzulässigen Zeichen ersetzt und die Datei wird im Zielordner gespeichert

### Requirement: Nachrichtentext speichern
Die Anwendung SHALL das Speichern des Nachrichtentexts anbieten, wobei jedes vorhandene Format mit passender Dateiendung (`.html`, `.rtf`, `.txt`) wählbar ist.

#### Scenario: HTML-Text speichern
- **WHEN** der Benutzer „Nachrichtentext speichern“ wählt und das HTML-Format bestätigt
- **THEN** wird der HTML-Text als `.html`-Datei gespeichert

#### Scenario: Kein Nachrichtentext
- **WHEN** die Nachricht keinen Nachrichtentext enthält
- **THEN** ist die Aktion „Nachrichtentext speichern“ nicht verfügbar

### Requirement: Fehler beim Speichern melden
Die Anwendung MUST Schreibfehler (z. B. fehlende Berechtigung, voller Datenträger) verständlich melden und beim Sammelexport angeben, welche Dateien nicht gespeichert werden konnten.

#### Scenario: Zielordner schreibgeschützt
- **WHEN** der Benutzer in einen schreibgeschützten Ordner speichert
- **THEN** erscheint eine Fehlermeldung und die Anwendung läuft weiter
