# Roslyn Code Metrics Tool — Bundled Binary

This directory contains a platform-specific self-contained publish of the
[`Microsoft.CodeAnalysis.Metrics`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Metrics)
NuGet package (`Metrics.exe`), used by the `GenerateSolutionMetrics` MSBuild target in
`build/metrics.targets` to produce `Metrics/Roslyn/SolutionMetrics.g.xml`.

## Bundled version

| Field | Value |
|---|---|
| Source | [dotnet/roslyn-analyzers](https://github.com/dotnet/roslyn-analyzers) |
| Commit | `45b767dde6b83e4e426f9aae07ddca8ea3e505e6` |
| File version | `1.0.0` |
| Platform | `win-arm64` |

## Platform restriction

The current bundle only runs on **Windows ARM64**. Running `metricsreporter generate`
(or `prepare-metrics.ps1`) on Windows x64 will fail with a platform mismatch error.

## Obtaining a binary for a different platform

### Option A — Download from NuGet (recommended)

The tool is packed in the `Microsoft.CodeAnalysis.Metrics` NuGet package.
Extract the binary matching your platform:

```powershell
# Download the package
Invoke-WebRequest `
  "https://www.nuget.org/api/v2/package/Microsoft.CodeAnalysis.Metrics" `
  -OutFile metrics.nupkg

# Rename to zip and extract
Rename-Item metrics.nupkg metrics.zip
Expand-Archive metrics.zip ./metrics-pkg -Force

# Copy the right platform folder
#   win-x64:   metrics-pkg/tools/win-x64/
#   win-arm64: metrics-pkg/tools/win-arm64/
#   (adjust as needed)
Copy-Item ./metrics-pkg/tools/win-x64/* ./build/Resources/metrics/win-arm64/ -Recurse -Force
```

> After copying, the folder path stays `win-arm64/` — just replace the contents.
> Alternatively adjust `RoslynMetricsToolDir` in `build/metrics.props` and
> `prepare-metrics.ps1` to point to an `win-x64/` folder alongside this one.

### Option B — Build from source

```powershell
git clone https://github.com/dotnet/roslyn-analyzers.git
cd roslyn-analyzers
dotnet publish src/Tools/Metrics/Metrics.csproj `
  -r win-x64 -c Release --self-contained true `
  -o /path/to/build/Resources/metrics/win-x64
```

## Updating the bundle

After replacing the binary, update the version row in this README, then run:

```powershell
metricsreporter generate
```

to verify the new binary produces valid XML output.
