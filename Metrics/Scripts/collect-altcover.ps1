# Collect AltCover coverage for MetricsReporter projects on demand.
# Steps:
# 1. Clean prior AltCover outputs and generated reports.
# 2. Build with AltCover instrumentation enabled.
# 3. Run tests to collect coverage and generate HTML.
# 4. Verify that the metrics JSON report exists.

param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..' '..') | Select-Object -ExpandProperty Path
$altCoverDir = Join-Path $repoRoot 'Metrics\AltCover'
$coverageTool = Join-Path $altCoverDir 'MetricsReporter.Tool.coverage.xml'
$coverageLib = Join-Path $altCoverDir 'MetricsReporter.coverage.xml'
$reportJson = Join-Path $repoRoot 'Metrics\MetricsReport.g.json'
$reportHtml = Join-Path $repoRoot 'Metrics\MetricsReport.html'

Write-Host "Cleaning previous AltCover outputs and reports..." -ForegroundColor Cyan
if (Test-Path $altCoverDir) {
    Remove-Item $altCoverDir -Force -Recurse -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $altCoverDir -Force | Out-Null
if (Test-Path $reportJson) { Remove-Item $reportJson -Force -ErrorAction SilentlyContinue }
if (Test-Path $reportHtml) { Remove-Item $reportHtml -Force -ErrorAction SilentlyContinue }

Write-Host "Building solution with AltCover instrumentation..." -ForegroundColor Cyan
dotnet build `
    /p:AltCoverInstrumentationEnabled=true `
    /p:AltCoverCollectionEnabled=false `
    /p:AltCoverHtmlReportEnabled=false `
    | Write-Output

Write-Host "Running tests with coverage collection..." -ForegroundColor Cyan
dotnet test MetricsReporter.Tests/MetricsReporter.Tests.csproj `
    /p:AltCoverInstrumentationEnabled=true `
    /p:AltCoverCollectionEnabled=true `
    /p:AltCoverHtmlReportEnabled=true `
    /p:AltCoverVerbose=false `
    | Write-Output

if (-not (Test-Path $coverageLib)) {
    throw "Coverage file was not created at $coverageLib"
}
if (-not (Test-Path $coverageTool)) {
    throw "Coverage file was not created at $coverageTool"
}

Write-Host "AltCover collection completed successfully. Coverage at $altCoverDir" -ForegroundColor Green

