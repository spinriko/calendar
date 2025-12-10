#!/usr/bin/env pwsh
param(
    [switch]$FailFast,
    [switch]$Execute,
    [switch]$DisableAnalyzers,
    [int]$TimeoutSeconds = 120
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
# Resolve repository root (one level up from scripts)
$repoRoot = Resolve-Path (Join-Path $scriptDir '..') | Select-Object -ExpandProperty Path
$analyzerProps = ""
if ($DisableAnalyzers) { $analyzerProps = "-p:RunAnalyzersDuringBuild=false -p:EnableNETAnalyzers=false" }

function Run-Command {
    param([string]$File, [string]$Arguments, [string]$WorkingDir)
    Write-Host "Command: $File $Arguments (cwd: $WorkingDir)" -ForegroundColor Cyan
    if (-not $Execute) { Write-Host "(Dry-run) Would run above command" -ForegroundColor DarkCyan; return 0 }

    $envVars = @{}
    # Ensure tests run against Testing environment (in-memory DB registered by test factories)
    $envVars['ASPNETCORE_ENVIRONMENT'] = 'Testing'

    try {
        if ($File -ieq 'dotnet') {
            # pass environment for dotnet so tests run with Testing env
            $proc = Start-Process -FilePath $File -ArgumentList $Arguments -WorkingDirectory $WorkingDir -NoNewWindow -Wait -PassThru -Environment $envVars
        }
        else {
            # tools like npm on Windows are wrappers (npm.cmd); avoid passing -Environment to reduce platform quirks
            $proc = Start-Process -FilePath $File -ArgumentList $Arguments -WorkingDirectory $WorkingDir -NoNewWindow -Wait -PassThru
        }
        Write-Host "$File exited with code $($proc.ExitCode)" -ForegroundColor Yellow
        return $proc.ExitCode
    }
    catch {
        Write-Host ("Error running {0} {1}: {2}" -f $File, $Arguments, $_) -ForegroundColor Red
        return 1
    }
}

function Wait-For-TestHostExit {
    param([int]$MaxSeconds = 20)
    $start = Get-Date
    while ((Get-Date) - $start -lt (New-TimeSpan -Seconds $MaxSeconds)) {
        $procs = Get-Process -Name testhost -ErrorAction SilentlyContinue
        if (-not $procs) { return }
        Start-Sleep -Milliseconds 500
    }
    # if still present after timeout, warn
    Write-Host "Warning: testhost processes still running after waiting $MaxSeconds seconds" -ForegroundColor Yellow
}

$exitCode = 0

Write-Host "\nDev Test Runner - safe test execution (uses ASPNETCORE_ENVIRONMENT=Testing)\n" -ForegroundColor Cyan

# C# test projects (relative to repo root)
$csharpProjects = @(
    [System.IO.Path]::Combine($repoRoot, 'pto.track.services.tests', 'pto.track.services.tests.csproj'),
    [System.IO.Path]::Combine($repoRoot, 'pto.track.tests', 'pto.track.tests.csproj')
)

foreach ($proj in $csharpProjects) {
    $projDir = Split-Path -Parent $proj
    Write-Host "Building C# project: $proj" -ForegroundColor Green
    $buildArgs = "build `"$proj`" --verbosity minimal $analyzerProps"
    $rc = Run-Command -File "dotnet" -Arguments $buildArgs -WorkingDir $projDir
    if ($rc -ne 0) { $exitCode = $rc; Write-Host "Build failed for $proj with $rc" -ForegroundColor Red; if ($FailFast) { exit $rc } }

    Write-Host "Running C# tests: $proj" -ForegroundColor Green
    # use --no-build to avoid file-copy locking during test execution (we just built above)
    $testArgs = "test `"$proj`" --verbosity normal --no-build"
    $rc = Run-Command -File "dotnet" -Arguments $testArgs -WorkingDir $projDir
    if ($rc -ne 0) { $exitCode = $rc; Write-Host "C# tests failed with $rc" -ForegroundColor Red; if ($FailFast) { exit $rc } }

    # wait a short while for any lingering testhost processes to exit before building the next project
    Wait-For-TestHostExit -MaxSeconds 20
}

# JavaScript tests
$jsDir = [System.IO.Path]::GetFullPath("$scriptDir\..\pto.track.tests.js")
if (-not (Test-Path (Join-Path $jsDir 'node_modules'))) {
    Write-Host "Installing npm dependencies in $jsDir" -ForegroundColor Green
    $rc = Run-Command -File "npm" -Arguments "ci" -WorkingDir $jsDir
    if ($rc -ne 0) { $exitCode = $rc; if ($FailFast) { exit $rc } }
}

Write-Host "Running JS tests" -ForegroundColor Green
$rc = Run-Command -File "npm" -Arguments "test" -WorkingDir $jsDir
if ($rc -ne 0) { $exitCode = $rc; if ($FailFast) { exit $rc } }

if ($exitCode -eq 0) { Write-Host "\n✓ All dev tests passed" -ForegroundColor Green } else { Write-Host "\n✗ Some dev tests failed (exit $exitCode)" -ForegroundColor Red }
exit $exitCode
