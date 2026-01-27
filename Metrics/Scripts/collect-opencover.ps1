# Collect OpenCover coverage for MetricsReporter projects on demand.
# Steps:
# 1. Clean prior OpenCover outputs and generated reports.
# 2. Build with OpenCover instrumentation enabled.
# 3. Run tests to collect coverage and generate HTML.
# 4. Verify that the metrics JSON report exists.

param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..' '..') | Select-Object -ExpandProperty Path
$openCoverDir = Join-Path $repoRoot 'Metrics\OpenCover'
$coverageTool = Join-Path $openCoverDir 'MetricsReporter.Tool.coverage.xml'
$coverageLib = Join-Path $openCoverDir 'MetricsReporter.coverage.xml'
$reportJson = Join-Path $repoRoot 'Metrics\MetricsReport.g.json'
$reportHtml = Join-Path $repoRoot 'Metrics\MetricsReport.html'

Write-Host "Cleaning previous OpenCover outputs and reports..." -ForegroundColor Cyan
if (Test-Path $openCoverDir) {
    Remove-Item $openCoverDir -Force -Recurse -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $openCoverDir -Force | Out-Null
if (Test-Path $reportJson) { Remove-Item $reportJson -Force -ErrorAction SilentlyContinue }
if (Test-Path $reportHtml) { Remove-Item $reportHtml -Force -ErrorAction SilentlyContinue }

Write-Host "Running tests with OpenCover instrumentation and coverage collection..." -ForegroundColor Cyan
dotnet test MetricsReporter.Tests/MetricsReporter.Tests.csproj `
    /p:OpenCoverInstrumentationEnabled=true `
    /p:OpenCoverCollectionEnabled=true `
    /p:OpenCoverHtmlReportEnabled=true `
    /p:OpenCoverVerbose=false `
    | Write-Output

if (-not (Test-Path $coverageLib)) {
    throw "Coverage file was not created at $coverageLib"
}
if (-not (Test-Path $coverageTool)) {
    throw "Coverage file was not created at $coverageTool"
}

Write-Host "OpenCover collection completed successfully. Coverage at $openCoverDir" -ForegroundColor Green
