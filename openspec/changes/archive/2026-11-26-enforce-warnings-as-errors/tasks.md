# Tasks

## 1. Zentrale Build-Einstellungen

- [x] 1.1 `Directory.Build.props` im Repo-Root anlegen mit `Nullable=enable`, `TreatWarningsAsErrors=true` und `WarningsAsErrors=$(WarningsAsErrors);nullable` (siehe design.md, Entscheidung 2); prüfen mit `dotnet msbuild src/WinmailDatReader.Core/WinmailDatReader.Core.csproj -getProperty:TreatWarningsAsErrors -getProperty:Nullable -getProperty:WarningsAsErrors` → `true`, `enable`, enthält `nullable`
- [x] 1.2 `<Nullable>enable</Nullable>` aus `WinmailDatReader.App.csproj`, `WinmailDatReader.Core.csproj` und `WinmailDatReader.Core.Tests.csproj` entfernen; prüfen, dass `-getProperty:Nullable` für alle drei Projekte weiterhin `enable` liefert
- [x] 1.3 Kurzen Hinweis im `README.md` ergänzen, dass Warnungen (inkl. Nullable) den Build abbrechen und wo die Einstellung liegt; prüfen durch Lesen des Abschnitts

## 2. Verifikation

- [x] 2.1 `dotnet build WinmailDatReader.sln --no-incremental` ausführen und prüfen: 0 Warnungen, 0 Fehler
- [x] 2.2 `dotnet test WinmailDatReader.sln` ausführen und prüfen, dass alle Tests grün sind
- [x] 2.3 Gegenprobe (temporär, danach rückgängig machen): in `WinmailDatReader.Core` eine Zeile wie `string s = null;` einfügen und prüfen, dass der Build mit `error CS8600` fehlschlägt; zusätzlich eine unbenutzte Variable einfügen und prüfen, dass `CS0168`/`CS0219` als Fehler gemeldet wird; Änderungen entfernen und erneut fehlerfrei bauen

## Workflow follow-up

- Change nach Review mit `/opsx:archive` archivieren.
