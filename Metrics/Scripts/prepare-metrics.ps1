[CmdletBinding()]
param()

# Runs the MSBuild GenerateSolutionMetrics target to produce Roslyn/SARIF inputs
# using paths from build/metrics.props; used as the generate hook in config.

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir ".." "..")).Path
$solutionPath = Join-Path $repoRoot "MetricsReporter.sln"
$roslynToolDir = (Resolve-Path (Join-Path $repoRoot "build" "Resources" "metrics" "win-arm64")).Path

if (-not (Test-Path $solutionPath))
{
  throw "MetricsReporter.sln was not found at '$solutionPath'. Ensure the script runs from the repository clone."
}

if (-not (Test-Path $roslynToolDir))
{
  throw "Roslyn metrics tool directory was not found at '$roslynToolDir'. Verify build/Resources assets are present."
}

$msbuildArgs = @(
  "/t:Rebuild;GenerateSolutionMetrics",
  "/p:RoslynMetricsEnabled=true",
  "/p:SarifMetricsEnabled=true",
  "/p:RoslynMetricsToolDir=$roslynToolDir"
)

Write-Host "dotnet msbuild $solutionPath $($msbuildArgs -join ' ')"
dotnet msbuild $solutionPath @msbuildArgs

