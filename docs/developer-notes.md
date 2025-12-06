# Developer Notes for MetricsReporter

## Where dotnet tool binaries live
- `dotnet tool run metricsreporter …` loads the installed tool from the .NET tool cache, not from your local bin:
  - Packages: `%USERPROFILE%\.nuget\packages\metricsreporter.tool\<version>\…`
  - Executables and shims: `%USERPROFILE%\.dotnet\toolspkgs` and `%USERPROFILE%\.dotnet\tools\.store\metricsreporter.tool\<version>\…`
- The tool manifest `.config/dotnet-tools.json` in the consumer repo (e.g., `sample-plugin`) pins the version that is executed.

## Updating MetricsReporter and consuming it in a consumer repo
1) In `C:\Users\baidakov\metricsreporter`:
   - Make code changes.
   - Bump the version (patch is enough) in `Directory.Build.props`.
   - Pack both packages to the local feed:
     ```pwsh
     dotnet pack src/MetricsReporter/MetricsReporter.csproj -c Release -o C:\Users\baidakov\.nuget\local-feed
     dotnet pack src/MetricsReporter.Tool/MetricsReporter.Tool.csproj -c Release -o C:\Users\baidakov\.nuget\local-feed
     ```
2) In `C:\Users\baidakov\sample-plugin`:
   - Update the tool from the local feed:
     ```pwsh
     dotnet tool update --tool-manifest .config\dotnet-tools.json `
       --add-source C:\Users\baidakov\.nuget\local-feed `
       MetricsReporter.Tool --version <new-version>
     ```
   - If the library package is referenced directly, update `PackageReference` (or `dotnet add package MetricsReporter --version <…> --source C:\Users\baidakov\.nuget\local-feed`).
   - Restore and verify: `dotnet tool restore`, `dotnet restore`, `dotnet build`, run metrics commands.

### Do I need to bump the version?
- Recommended: always bump at least the patch. NuGet caches packages by version; reusing the same version requires manual cache cleanup in `%USERPROFILE%\.nuget\packages\metricsreporter*\<version>` and deleting the old `.nupkg` from the feed, plus `--no-cache` updates. Bumping avoids mistakes and stale binaries.

## Using the tool while developing MetricsReporter itself
- Safe, but `dotnet tool run …` executes the last installed package from the cache, not your live source.
- To test current source without packing, prefer:
  ```pwsh
  dotnet run --project src/MetricsReporter.Tool/MetricsReporter.Tool.csproj -- metrics-reader ...
  ```
  or pack/update the tool from the local feed as described above.

## Quick reference commands
- Generate metrics/HTML in a consumer repo:
  ```pwsh
  dotnet msbuild sample-plugin.sln /t:Build `
    /p:GenerateMetricsDashboard=true `
    /p:RoslynMetricsEnabled=true `
    /p:SarifMetricsEnabled=true `
    /p:MetricsTargetsAnchorProject=Sample.Core.Tests `
    /p:AltCoverEnabled=true `
    /p:MrCoverageEnabled=false
  ```
  Outputs: `build/Metrics/Report/MetricsReport.html`, `MetricsReport.g.json`, `build/MetricsTemp/SuppressedSymbols.g.json`.
- Read metrics examples:
  - Coupling: `dotnet tool run metricsreporter metrics-reader readany --namespace <ns> --metric Coupling --symbol-kind Any`
  - Coverage: `dotnet tool run metricsreporter metrics-reader readany --namespace <ns> --metric AltCoverBranchCoverage --group-by type`
  - SARIF: `dotnet tool run metricsreporter metrics-reader readsarif --namespace <ns> [--ruleid <CAxxxx|IDExxxx>]`
  - Check symbol: `dotnet tool run metricsreporter metrics-reader test --symbol <FQN> --metric <Metric>`


