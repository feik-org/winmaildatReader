# Proposal

## Why

Der Build meldet Compiler- und Analyzer-Warnungen derzeit nur als Warnungen. Insbesondere Nullable-Warnungen (mögliche `NullReferenceException`, CS86xx) können so unbemerkt in den Code gelangen. Der Build ist aktuell warnungsfrei (`dotnet build --no-incremental`: 0 Warnungen) – jetzt lässt sich die Regel ohne Aufräumarbeit einführen und dauerhaft absichern.

## What Changes

- Neue zentrale Datei `Directory.Build.props` im Repository-Root, die für alle Projekte (App, Core, Tests) gilt:
  - `TreatWarningsAsErrors=true` – jede Warnung bricht den Build ab.
  - `Nullable=enable` zentral, damit neue Projekte die Nullable-Analyse automatisch erhalten.
  - Nullable-Warnungen werden zusätzlich explizit über `WarningsAsErrors` (`nullable`) als Fehler geführt, sodass sie auch dann Fehler bleiben, wenn `TreatWarningsAsErrors` in einem Projekt einmal lokal abgeschaltet wird.
- Die redundanten `<Nullable>enable</Nullable>`-Einträge in den drei `.csproj`-Dateien werden entfernt.
- **BREAKING** (nur für Entwickler): Code mit Warnungen kompiliert nicht mehr.

## Capabilities

### New Capabilities
- (keine)

### Modified Capabilities
- (keine)

Reine Build-/Tooling-Änderung ohne Auswirkung auf das Verhalten der Anwendung; die Change setzt daher `skip_specs: true`.

## Impact

- Betroffene Dateien: neue `Directory.Build.props`; `src/WinmailDatReader.App/WinmailDatReader.App.csproj`, `src/WinmailDatReader.Core/WinmailDatReader.Core.csproj`, `tests/WinmailDatReader.Core.Tests/WinmailDatReader.Core.Tests.csproj`.
- Gilt für Debug- und Release-Builds, lokal wie in der IDE.
- NuGet-Warnungen (z. B. NU1901–NU1904 aus dem Sicherheits-Audit) werden durch `TreatWarningsAsErrors` ebenfalls zu Fehlern; ein neu veröffentlichter CVE in einer Abhängigkeit kann den Build daher plötzlich brechen (siehe design.md).
- Keine neuen Abhängigkeiten, keine Laufzeitänderung.
