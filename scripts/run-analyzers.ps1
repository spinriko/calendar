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
$sarifFile = Join-Path $artifactsDir "analyzers-$timestamp.sarif"

if ($Execute) {
    Write-Host "Running analyzers build: $cmd"
    # Run build with analyzers enabled and emit a SARIF error log in addition to a plaintext log
    & $dotnet build $Solution -c $Configuration /p:RunAnalyzersDuringBuild=true /p:ErrorLog="$sarifFile" /p:ErrorLogFormat=sarifv2.1.0 -v minimal 2>&1 | Tee-Object $logFile
    if (Test-Path $sarifFile) {
        Write-Host "SARIF error log written to: $sarifFile"
    }
    else {
        Write-Host "SARIF file was not created by MSBuild — creating an empty SARIF skeleton for CI consumers."
        $sarifSkeleton = @'
{
  "$schema": "https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0.json",
  "version": "2.1.0",
  "runs": []
}
'@
        try {
            $sarifSkeleton | Out-File -FilePath $sarifFile -Encoding utf8 -Force
            Write-Host "Wrote empty SARIF to: $sarifFile"
        }
        catch {
            Write-Host "Failed to write SARIF skeleton: $_"
        }
    }
    exit $LASTEXITCODE
}
else {
    Write-Host "Dry-run: analyzer build command (no changes will be executed)."
    Write-Host $cmd
    Write-Host "To actually run analyzers, re-run with the `-Execute` switch."
    Write-Host "Plaintext log will be written to: $logFile when executed."
    Write-Host "SARIF error log will be written to: $sarifFile when executed."
}
