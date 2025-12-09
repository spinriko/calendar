param(
    [string]$Solution = 'pto.track.sln',
    [switch]$Execute,
    [string]$Configuration = 'Release'
)

$dotnet = 'dotnet'
$cmd = "$dotnet build `"$Solution`" -c $Configuration /p:RunAnalyzersDuringBuild=true -v minimal"

$timestamp = (Get-Date -Format "yyyyMMdd-HHmmss")
$artifactsDir = Join-Path $PSScriptRoot "..\artifacts\analyzers"
if (-not (Test-Path $artifactsDir)) { New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null }
$logFile = Join-Path $artifactsDir "analyzers-$timestamp.log"

if ($Execute) {
    Write-Host "Running analyzers build: $cmd"
    & $dotnet build $Solution -c $Configuration /p:RunAnalyzersDuringBuild=true -v minimal 2>&1 | Tee-Object $logFile
    exit $LASTEXITCODE
}
else {
    Write-Host "Dry-run: analyzer build command (no changes will be executed)."
    Write-Host $cmd
    Write-Host "To actually run analyzers, re-run with the `-Execute` switch."
    Write-Host "Log will be written to: $logFile when executed."
}
