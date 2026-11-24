# Spec Delta

## Purpose

Windows-Desktop-Oberfläche, mit der Benutzer eine winmail.dat-Datei öffnen und deren Metadaten, Nachrichtentext und Anhänge ansehen können.

## ADDED Requirements

### Requirement: Datei über Dialog öffnen
Die Anwendung SHALL einen Datei-öffnen-Dialog anbieten, der standardmäßig nach `*.dat` filtert und zusätzlich die Auswahl aller Dateien erlaubt.

#### Scenario: Datei auswählen
- **WHEN** der Benutzer „Öffnen“ wählt und eine winmail.dat auswählt
- **THEN** wird die Datei eingelesen und ihr Inhalt angezeigt

#### Scenario: Dialog abbrechen
- **WHEN** der Benutzer den Dialog abbricht
- **THEN** bleibt die bisherige Anzeige unverändert

### Requirement: Datei per Drag & Drop öffnen
Die Anwendung SHALL das Öffnen einer Datei durch Ziehen auf das Hauptfenster unterstützen. Bei mehreren gezogenen Dateien MUST die erste Datei geöffnet werden.

#### Scenario: Datei auf Fenster ziehen
- **WHEN** der Benutzer eine winmail.dat auf das Fenster zieht
- **THEN** wird die Datei eingelesen und ihr Inhalt angezeigt

### Requirement: Datei per Kommandozeile öffnen
Die Anwendung SHALL einen als erstes Kommandozeilenargument übergebenen Dateipfad beim Start öffnen, sodass „Öffnen mit…“ im Explorer funktioniert.

#### Scenario: Start mit Dateipfad
- **WHEN** die Anwendung mit dem Pfad einer winmail.dat als Argument gestartet wird
- **THEN** zeigt sie nach dem Start direkt den Inhalt dieser Datei an

### Requirement: Metadaten und Nachrichtentext anzeigen
Die Anwendung SHALL Dateipfad, Betreff, Absender und Datum sowie den Nachrichtentext anzeigen. HTML MUST gerendert, RTF formatiert und Klartext als Text dargestellt werden; ist mehr als ein Format vorhanden, MUST HTML vor RTF vor Klartext bevorzugt werden.

#### Scenario: HTML-Text vorhanden
- **WHEN** die Nachricht einen HTML-Text enthält
- **THEN** wird der HTML-Text gerendert angezeigt

#### Scenario: Kein Nachrichtentext
- **WHEN** die Nachricht keinen Nachrichtentext enthält
- **THEN** zeigt die Anwendung den Hinweis „Kein Nachrichtentext vorhanden“

### Requirement: Externe Inhalte im HTML blockieren
Die Anwendung MUST beim Anzeigen von HTML-Nachrichtentext das Nachladen externer Ressourcen und das Ausführen von Skripten unterbinden.

#### Scenario: HTML mit externem Bild
- **WHEN** der HTML-Text ein Bild von einer externen URL referenziert
- **THEN** wird keine Netzwerkanfrage an diese URL gestellt

### Requirement: Anhangsliste anzeigen
Die Anwendung SHALL alle Anhänge in einer Liste mit Dateiname und lesbar formatierter Größe (z. B. „12,3 KB“) anzeigen.

#### Scenario: Nachricht mit Anhängen
- **WHEN** eine Nachricht mit drei Anhängen geöffnet wird
- **THEN** zeigt die Liste drei Einträge mit Name und Größe

#### Scenario: Nachricht ohne Anhänge
- **WHEN** eine Nachricht ohne Anhänge geöffnet wird
- **THEN** zeigt die Anwendung den Hinweis „Keine Anhänge“

### Requirement: Fehler beim Öffnen anzeigen
Die Anwendung MUST Lesefehler in einer verständlichen Meldung anzeigen und weiter bedienbar bleiben.

#### Scenario: Ungültige Datei geöffnet
- **WHEN** der Benutzer eine Datei öffnet, die keine gültige TNEF-Datei ist
- **THEN** erscheint eine Fehlermeldung und die Anwendung läuft weiter
