# Spec Delta

## Purpose

Liest TNEF-kodierte Dateien (typischerweise `winmail.dat` aus Outlook) und stellt deren Metadaten, Nachrichtentext und eingebettete Anhänge für Anzeige und Export bereit.

## ADDED Requirements

### Requirement: TNEF-Datei einlesen
Das System SHALL eine TNEF-Datei unabhängig von ihrem Dateinamen anhand ihres Inhalts einlesen und daraus eine Nachricht mit Metadaten, Nachrichtentext und Anhängen erzeugen.

#### Scenario: Gültige winmail.dat mit Anhängen
- **WHEN** eine gültige TNEF-Datei mit zwei eingebetteten Anhängen eingelesen wird
- **THEN** liefert das System eine Nachricht mit genau zwei Anhängen

#### Scenario: Abweichender Dateiname
- **WHEN** eine gültige TNEF-Datei mit dem Namen `nachricht.bin` eingelesen wird
- **THEN** wird sie genauso verarbeitet wie eine Datei namens `winmail.dat`

### Requirement: Metadaten bereitstellen
Das System SHALL, soweit in der Datei vorhanden, Betreff, Absender und Sende- bzw. Empfangsdatum bereitstellen. Fehlende Werte MUST als leer gekennzeichnet werden, ohne dass das Einlesen fehlschlägt.

#### Scenario: Metadaten vorhanden
- **WHEN** die TNEF-Datei einen Betreff und einen Absender enthält
- **THEN** stellt das System diese Werte unverändert bereit

#### Scenario: Metadaten fehlen
- **WHEN** die TNEF-Datei keinen Betreff enthält
- **THEN** ist der Betreff leer und das Einlesen ist trotzdem erfolgreich

### Requirement: Nachrichtentext bereitstellen
Das System SHALL den Nachrichtentext in jedem in der Datei enthaltenen Format (Klartext, HTML, RTF) bereitstellen. Komprimiertes RTF MUST dekomprimiert werden.

#### Scenario: Nur komprimiertes RTF vorhanden
- **WHEN** die Datei den Nachrichtentext ausschließlich als komprimiertes RTF enthält
- **THEN** stellt das System den dekomprimierten RTF-Text bereit

#### Scenario: Kein Nachrichtentext
- **WHEN** die Datei keinen Nachrichtentext enthält
- **THEN** meldet das System, dass kein Nachrichtentext vorhanden ist, und das Einlesen ist erfolgreich

### Requirement: Anhänge bereitstellen
Das System SHALL für jeden eingebetteten Anhang Dateiname, Größe in Bytes und den vollständigen Inhalt bereitstellen. Der lange Dateiname MUST bevorzugt werden; ohne Dateinamen MUST ein eindeutiger Ersatzname (z. B. `Anhang_1`) vergeben werden.

#### Scenario: Langer Dateiname vorhanden
- **WHEN** ein Anhang sowohl einen kurzen 8.3-Namen als auch einen langen Dateinamen besitzt
- **THEN** wird der lange Dateiname verwendet

#### Scenario: Anhang ohne Namen
- **WHEN** ein Anhang keinen Dateinamen besitzt
- **THEN** erhält er einen Ersatznamen, der innerhalb der Nachricht eindeutig ist

#### Scenario: Inhalt unverändert
- **WHEN** ein Anhang bereitgestellt wird
- **THEN** stimmen sein Inhalt und seine Größe byte-genau mit den in der TNEF-Datei kodierten Daten überein

### Requirement: Ungültige Dateien erkennen
Das System MUST Dateien ohne gültige TNEF-Signatur sowie beschädigte oder abgeschnittene TNEF-Dateien mit einem verständlichen Fehler ablehnen, statt abzustürzen.

#### Scenario: Keine TNEF-Datei
- **WHEN** eine beliebige andere Datei (z. B. eine Textdatei) eingelesen wird
- **THEN** meldet das System, dass es sich nicht um eine gültige winmail.dat/TNEF-Datei handelt

#### Scenario: Abgeschnittene Datei
- **WHEN** eine TNEF-Datei mitten in den Daten abgeschnitten ist
- **THEN** meldet das System einen Lesefehler und es wird kein unvollständiger Anhang als vollständig ausgegeben
