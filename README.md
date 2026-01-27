# MetricsReporter

MetricsReporter is a .NET 8 library and CLI for aggregating code metrics (OpenCover, Roslyn, SARIF) into unified JSON/HTML reports. The CLI is shipped as a `dotnet tool` wrapper over the library so that consumers can either reference the package or run the tool directly.

## Projects
- `MetricsReporter` — class library (`PackageId: MetricsReporter`).
- `MetricsReporter.Tool` — CLI wrapper (`PackAsTool`, `PackageId: MetricsReporter.Tool`, `ToolCommandName: metricsreporter`).
- `MetricsReporter.Tests` — NUnit/FluentAssertions unit tests.

## Prerequisites
- .NET 8 SDK
- Local NuGet feed at `C:\Users\baidakov\.nuget\local-feed` (see below)

## Build & test
```pwsh
cd C:\Users\baidakov\metricsreporter
dotnet restore
dotnet build --no-incremental
dotnet test
```

## Local NuGet feed
The repo includes `NuGet.Config` with two sources: `nuget.org` and `metricsreporter` pointing to `C:\Users\baidakov\.nuget\local-feed`.
If the feed directory does not exist, create it:
```pwsh
New-Item -ItemType Directory -Force -Path C:\Users\baidakov\.nuget\local-feed | Out-Null
```

## Packing to the local feed
```pwsh
# Library package
dotnet pack MetricsReporter/MetricsReporter.csproj -c Release -o C:\Users\baidakov\.nuget\local-feed

# CLI dotnet tool package
dotnet pack MetricsReporter.Tool/MetricsReporter.Tool.csproj -c Release -o C:\Users\baidakov\.nuget\local-feed
```
Packages will be versioned `0.2.0` by default (see `Directory.Build.props`).

## Installing the CLI from the local feed
```pwsh
# install (per-user global tool)
dotnet tool install --global MetricsReporter.Tool --version 0.2.0 --add-source C:\Users\baidakov\.nuget\local-feed
# update to a new packed version
dotnet tool update --global MetricsReporter.Tool --version 0.2.x --add-source C:\Users\baidakov\.nuget\local-feed
```
Run `metricsreporter --help` to see CLI options. Use `metricsreporter metrics-reader --help` for the metrics-reader sub-commands.

## Consuming the library from the local feed
Add the `metricsreporter` source or rely on the repo `NuGet.Config`, then reference the package:
```xml
<ItemGroup>
  <PackageReference Include="MetricsReporter" Version="0.2.0" />
</ItemGroup>
```
Restore with `dotnet restore` and build as usual.

## License
MIT (see `LICENSE`).

