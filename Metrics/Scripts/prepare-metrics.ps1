[CmdletBinding()]
param()

# Runs the MSBuild GenerateSolutionMetrics target to produce Roslyn/SARIF inputs
# using paths from build/metrics.props; used as the generate hook in config.

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
$solutionPath = Join-Path $repoRoot "MetricsReporter.sln"

$msbuildArgs = @(
  "/t:GenerateSolutionMetrics",
  "/p:RoslynMetricsEnabled=true",
  "/p:SarifMetricsEnabled=true"
)

Write-Host "dotnet msbuild $solutionPath $($msbuildArgs -join ' ')"
dotnet msbuild $solutionPath @msbuildArgs

