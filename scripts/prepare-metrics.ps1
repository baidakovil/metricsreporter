[CmdletBinding()]
param(
  [switch]$DisableSarif,
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
$solutionPath = Join-Path $repoRoot "MetricsReporter.sln"
$buildDir = Join-Path $repoRoot "build"
$metricsDir = Join-Path $buildDir "Metrics"
$metricsReportDir = Join-Path $metricsDir "Report"
$metricsTempDir = Join-Path $buildDir "MetricsTemp"
$roslynDir = Join-Path $metricsDir "Roslyn"
$sarifDir = Join-Path $metricsDir "Sarif"
$toolDir = Join-Path $buildDir "Resources\\metrics\\win-arm64"
$metricsExe = Join-Path $toolDir "Metrics.exe"
$outputFile = Join-Path $roslynDir "SolutionMetrics.g.xml"

New-Item -ItemType Directory -Force -Path $metricsDir, $metricsReportDir, $metricsTempDir | Out-Null
New-Item -ItemType Directory -Force -Path $roslynDir | Out-Null
if (-not $DisableSarif) {
  New-Item -ItemType Directory -Force -Path $sarifDir | Out-Null
}

if (-not (Test-Path $metricsExe)) {
  throw "Metrics tool not found at '$metricsExe'. Copy it from rca-plugin/build/Resources/metrics before running."
}

$buildArgs = @("build", $solutionPath, "--no-incremental", "-c", $Configuration)
if (-not $DisableSarif) {
  $sarifDirFull = [System.IO.Path]::GetFullPath($sarifDir)
  $buildArgs += "/p:SarifMetricsEnabled=true"
  $buildArgs += "/p:SarifOutputDir=`"$sarifDirFull`""
}

Write-Host "dotnet $($buildArgs -join ' ')"
dotnet @buildArgs

$solutionFull = (Resolve-Path $solutionPath).Path
$metricsOutFull = [System.IO.Path]::GetFullPath($outputFile)

Push-Location $toolDir
try {
  Write-Host "Running Microsoft.CodeAnalysis.Metrics.exe for $solutionFull"
  & $metricsExe "/solution:$solutionFull" "/out:$metricsOutFull" "/quiet"
}
finally {
  Pop-Location
}

