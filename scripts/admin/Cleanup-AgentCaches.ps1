<#
.SYNOPSIS
Cleans Azure Pipelines agent workspace run folders and local NuGet/npm caches.

.DESCRIPTION
- Deletes agent run folders under the specified _work root older than -Days.
- Optionally clears NuGet global cache and npm cache for the current user.
- Safe to run when no pipeline job is executing on the agent.

.EXAMPLES
.
\Cleanup-AgentCaches.ps1 -WorkRoot "C:\azdo-agent\_work" -Days 7 -ClearNuGet -ClearNpm -WhatIf
\Cleanup-AgentCaches.ps1 -WorkRoot "C:\azdo-agent\_work" -Days 3 -ClearNuGet

.NOTES
Run from an elevated PowerShell if permissions are restricted. Uses -WhatIf for dry-run.
#>

param(
    [string]$WorkRoot = "C:\azdo-agent\_work",
    [int]$Days = 7,
    [switch]$ClearNuGet,
    [switch]$ClearNpm,
    [switch]$WhatIf,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
if ($Verbose) { $VerbosePreference = "Continue" }

if (-not (Test-Path $WorkRoot)) {
    Write-Warning "Work root not found: $WorkRoot"
}
else {
    Write-Host "Cleaning run folders older than $Days days under $WorkRoot" -ForegroundColor Cyan
    $cutoff = (Get-Date).AddDays(-$Days)
    Get-ChildItem -Path $WorkRoot -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTime -lt $cutoff } |
    ForEach-Object {
        Write-Host "Removing $_" -ForegroundColor Yellow
        Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue -WhatIf:$WhatIf
    }
}

if ($ClearNuGet) {
    Write-Host "Clearing NuGet cache..." -ForegroundColor Cyan
    dotnet nuget locals all --clear | Out-Host
}

if ($ClearNpm) {
    Write-Host "Clearing npm cache..." -ForegroundColor Cyan
    npm cache clean --force | Out-Host
}

Write-Host "Done." -ForegroundColor Green
