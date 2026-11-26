# Design

## Context

- Drei Projekte: `WinmailDatReader.App` (WPF, `net10.0-windows`), `WinmailDatReader.Core` (`net10.0`), `WinmailDatReader.Core.Tests` (xUnit). SDK 10.0.404 über `global.json`.
- Alle drei setzen bereits `<Nullable>enable</Nullable>` einzeln in der `.csproj`; es gibt weder `Directory.Build.props` noch `.editorconfig`.
- Ein vollständiger Rebuild der Solution ergibt heute 0 Warnungen und 0 Fehler – die Umstellung erfordert also keine Code-Korrekturen.

## Goals / Non-Goals

**Goals:**
- Jede Compiler-, Analyzer- und NuGet-Warnung bricht den Build ab – in allen bestehenden und künftigen Projekten des Repos.
- Nullable-Warnungen bleiben auch dann Fehler, wenn ein Projekt `TreatWarningsAsErrors` lokal überschreibt.

**Non-Goals:**
- Zusätzliche Analyzer aktivieren oder verschärfen (`AnalysisLevel`, `EnforceCodeStyleInBuild`, StyleCop o. Ä.).
- Eine `.editorconfig` mit Regel-Schweregraden einführen.
- CI-Pipeline anlegen (existiert nicht).

## Decisions

1. **Zentrale `Directory.Build.props` im Repo-Root statt Einträge in jeder `.csproj`.**
   MSBuild importiert sie automatisch für alle Projekte unterhalb des Roots. Neue Projekte erben die Regel ohne Zutun. Alternative „pro `.csproj`“ verworfen: leicht zu vergessen, dreifach gepflegt.

2. **Inhalt:**
   ```xml
   <Project>
     <PropertyGroup>
       <Nullable>enable</Nullable>
       <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
       <WarningsAsErrors>$(WarningsAsErrors);nullable</WarningsAsErrors>
     </PropertyGroup>
   </Project>
   ```
   - `nullable` ist der vom C#-Compiler unterstützte Sammelbegriff für alle Nullable-Warnungen (CS8600–CS8xxx). Er macht die Absicht explizit und schützt vor einem späteren lokalen `TreatWarningsAsErrors=false`.
   - `$(WarningsAsErrors);` hängt an, statt evtl. vom SDK vorbelegte Werte zu überschreiben.

3. **`<Nullable>` aus den `.csproj` entfernen.** Eine einzige Quelle der Wahrheit; projektspezifische Werte würden die zentrale Einstellung sonst still überschreiben (die `.csproj` wird nach `Directory.Build.props` ausgewertet).

4. **NuGet-Audit-Warnungen (NU1901–NU1904) bleiben Fehler.** Die Anforderung lautet „Warnings als Errors“ ohne Ausnahme; eine bekannte Schwachstelle in einer Abhängigkeit soll auffallen. Alternative `WarningsNotAsErrors` für NU190x verworfen, kann bei Bedarf nachgerüstet werden (siehe Risiken).

## Risks / Trade-offs

- [Neuer CVE in `MimeKit`, `Microsoft.Web.WebView2` o. Ä. bricht den Build ohne Codeänderung] → Paket aktualisieren; als Notlösung `<WarningsNotAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>` in `Directory.Build.props`.
- [SDK-Update bringt neue Warnungen und bricht den Build] → `global.json` pinnt die Feature-Band-Version; Warnungen beim SDK-Update beheben.
- [Gezieltes Unterdrücken einzelner Warnungen bleibt per `#pragma warning disable` / `!` möglich] → bewusst akzeptiert; im Code-Review prüfen.

## Migration Plan

Datei anlegen, `.csproj` bereinigen, `dotnet build --no-incremental` und `dotnet test` laufen lassen. Rollback: `Directory.Build.props` löschen und `<Nullable>` in den `.csproj` wiederherstellen (bzw. Commit revertieren).
