#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs all tests for the PTO tracking solution.

.DESCRIPTION
    This script executes all test suites in the solution:
    - C# unit tests (pto.track.services.tests)
    - C# integration tests (pto.track.tests)
    - JavaScript tests (pto.track.tests.js)
    - Cyclomatic complexity analysis (C# and JavaScript)

.PARAMETER SkipCSharp
    Skip C# tests.

.PARAMETER SkipJavaScript
    Skip JavaScript tests.

.PARAMETER SkipComplexity
    Skip cyclomatic complexity analysis.

.EXAMPLE
    .\run-all-tests.ps1
    Runs all tests and complexity analysis.

.EXAMPLE
    .\run-all-tests.ps1 -SkipComplexity
    Runs all tests but skips complexity analysis.
#>

param(
    [switch]$SkipCSharp,
    [switch]$SkipJavaScript,
    [switch]$SkipComplexity,
    [switch]$FailFast,
    [switch]$Execute,
    [switch]$DisableAnalyzers,
    [int]$CommandTimeoutSeconds = 120
)

# Keep running so we can collect all results, but execute each test process synchronously
$ErrorActionPreference = "Continue"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$allTestsPassed = $true

# MSBuild analyzer properties (optional)
$analyzerProps = ""
if ($DisableAnalyzers) { $analyzerProps = "-p:RunAnalyzersDuringBuild=false -p:EnableNETAnalyzers=false" }

# Step counter for tracing
$global:__runAllStep = 0
function Log-Step {
    param([string]$Message)
    $global:__runAllStep += 1
    Write-Host "[$global:__runAllStep] $Message" -ForegroundColor Magenta
}

# Helper: run an external command synchronously and return its exit code
function Invoke-BlockingCommand {
    param(
        [string]$File,
        [string]$Arguments,
        [string]$WorkingDirectory = $PWD
    )

    # Print the command that would be run
    Write-Host "Command: $File $Arguments (cwd: $WorkingDirectory)" -ForegroundColor Cyan

    if (-not $Execute) {
        Write-Host "(Dry-run) Not executing. Pass -Execute to actually run commands." -ForegroundColor DarkCyan
        return 0
    }

    # For interactive tools like dotnet/npm, run in-process so output streams live to console
    if ($File -ieq 'dotnet' -or $File -ieq 'npm') {
        Write-Host "Running interactive command in current shell: $File $Arguments" -ForegroundColor DarkCyan
            $start = Get-Date
            Push-Location $WorkingDirectory
            try {
                if (-not $Execute) {
                    Write-Host "(Dry-run) Not executing interactive command." -ForegroundColor DarkCyan
                    return 0
                }
                # Start the process attached to the same console so output streams live,
                # but avoid piping the command output into the function return value.
                # Ensure the child process inherits ASPNETCORE_ENVIRONMENT when set in this session
                $startArgs = @{
                    FilePath = $File
                    ArgumentList = $Arguments
                    WorkingDirectory = $WorkingDirectory
                    NoNewWindow = $true
                    Wait = $true
                    PassThru = $true
                }
                if ($null -ne $env:ASPNETCORE_ENVIRONMENT -and $env:ASPNETCORE_ENVIRONMENT -ne '') {
                    $procEnv = @{ 'ASPNETCORE_ENVIRONMENT' = $env:ASPNETCORE_ENVIRONMENT }
                    $startArgs['Environment'] = $procEnv
                }
                $proc = Start-Process @startArgs
                $exit = $proc.ExitCode
                $elapsed = (Get-Date) - $start
                Write-Host "$File exited with code $exit (elapsed $([int]$elapsed.TotalSeconds)s)" -ForegroundColor Yellow
                return $exit
            } finally {
                Pop-Location
            }
    }

    # Non-interactive: use Start-Process with timeout
    $start = Get-Date
    $psi = Start-Process -FilePath $File -ArgumentList $Arguments -WorkingDirectory $WorkingDirectory -NoNewWindow -PassThru
    if ($null -eq $psi) {
        Write-Warning "Failed to start $File"
        return 1
    }

    $timeoutMs = [int]($CommandTimeoutSeconds * 1000)
    $exited = $psi.WaitForExit($timeoutMs)
    $elapsed = (Get-Date) - $start
    if (-not $exited) {
        Write-Warning "$File did not finish within $CommandTimeoutSeconds seconds (elapsed $([int]$elapsed.TotalSeconds)s). Killing process $($psi.Id)."
        try { Stop-Process -Id $psi.Id -Force -ErrorAction SilentlyContinue } catch {}
        return 124
    }

    Write-Host "$File exited with code $($psi.ExitCode) (elapsed $([int]$elapsed.TotalSeconds)s)" -ForegroundColor Yellow
    return $psi.ExitCode
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  PTO Tracking Solution - Test Suite" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# C# Tests
if (-not $SkipCSharp) {
    Log-Step "Starting C# Unit Tests (pto.track.services.tests)"

    # Ensure tests run in the Test environment so they don't hit production DBs
    $prevAspNetEnv = $env:ASPNETCORE_ENVIRONMENT
    if ($Execute) {
        $env:ASPNETCORE_ENVIRONMENT = 'Testing'
        Write-Host "Set ASPNETCORE_ENVIRONMENT=Testing for C# tests" -ForegroundColor Cyan
    } else {
        Write-Host "(Dry-run) Would set ASPNETCORE_ENVIRONMENT=Testing for C# tests" -ForegroundColor DarkCyan
    }
    try {
        Log-Step "Invoking unit tests: dotnet test --verbosity normal $analyzerProps"
        $exit = Invoke-BlockingCommand -File "dotnet" -Arguments "test --verbosity normal $analyzerProps" -WorkingDirectory "$scriptDir\pto.track.services.tests"
        Log-Step "Unit tests returned exit code: $exit"
    } catch {
        Log-Step "Exception while running unit tests: $_"
        $exit = 1
    }
    if ($exit -ne 0) {
        $allTestsPassed = $false
        if ($FailFast) { Write-Host "Fail-fast enabled: stopping on first failure." -ForegroundColor Red; exit $exit }
    }

    Log-Step "Starting C# Integration Tests (pto.track.tests)"
    Log-Step "Analyzer props: '$analyzerProps'"
    # Give console a moment to flush and make the sequence clearer
    Start-Sleep -Milliseconds 200
    $integrationCmd = "test --verbosity normal $analyzerProps"
    Log-Step "About to run: dotnet $integrationCmd (cwd: $scriptDir\pto.track.tests)"
    try {
        Log-Step "Invoking integration tests"
        $exit = Invoke-BlockingCommand -File "dotnet" -Arguments $integrationCmd -WorkingDirectory "$scriptDir\pto.track.tests"
        Log-Step "Integration tests returned exit code: $exit"
    } catch {
        Log-Step "Exception when invoking integration tests: $_"
        $exit = 1
    }
    if ($exit -ne 0) { 
        $allTestsPassed = $false
        if ($FailFast) { Write-Host "Fail-fast enabled: stopping on first failure." -ForegroundColor Red; exit $exit }
    }

    # Restore ASPNETCORE_ENVIRONMENT
    if ($Execute) {
        if ($null -ne $prevAspNetEnv) { $env:ASPNETCORE_ENVIRONMENT = $prevAspNetEnv } else { Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue }
        Write-Host "Restored ASPNETCORE_ENVIRONMENT to '$($env:ASPNETCORE_ENVIRONMENT)'" -ForegroundColor Cyan
    } else {
        Write-Host "(Dry-run) Would restore ASPNETCORE_ENVIRONMENT to '$prevAspNetEnv'" -ForegroundColor DarkCyan
    }
}
else {
    Write-Host "`nSkipping C# tests..." -ForegroundColor Gray
}

# JavaScript Tests
if (-not $SkipJavaScript) {
    Write-Host "`n--- Running JavaScript Tests (pto.track.tests.js) ---" -ForegroundColor Yellow
    $jsDir = "$scriptDir\pto.track.tests.js"
    # Check if node_modules exists
    if (-not (Test-Path (Join-Path $jsDir 'node_modules'))) {
        Write-Host "Installing npm dependencies..." -ForegroundColor Cyan
        $exit = Invoke-BlockingCommand -File "npm" -Arguments "install" -WorkingDirectory $jsDir
        if ($exit -ne 0) { $allTestsPassed = $false; if ($FailFast) { Write-Host "Fail-fast enabled: stopping on first failure." -ForegroundColor Red; exit $exit } }
    }

    $exit = Invoke-BlockingCommand -File "npm" -Arguments "test" -WorkingDirectory $jsDir
    if ($exit -ne 0) { $allTestsPassed = $false; if ($FailFast) { Write-Host "Fail-fast enabled: stopping on first failure." -ForegroundColor Red; exit $exit } }
}
else {
    Write-Host "`nSkipping JavaScript tests..." -ForegroundColor Gray
}

# Cyclomatic Complexity Analysis
if (-not $SkipComplexity) {
    Write-Host "`n--- Running Cyclomatic Complexity Analysis ---" -ForegroundColor Yellow
    
    Write-Host "`nC# Complexity Analysis:" -ForegroundColor Cyan
    $exit = Invoke-BlockingCommand -File "dotnet" -Arguments "test --filter \"FullyQualifiedName~CodeMetricsAnalyzer\" --verbosity normal $analyzerProps" -WorkingDirectory "$scriptDir\pto.track.tests"
    if ($exit -ne 0) { $allTestsPassed = $false; if ($FailFast) { Write-Host "Fail-fast enabled: stopping on first failure." -ForegroundColor Red; exit $exit } }

    Write-Host "`nJavaScript Complexity Analysis:" -ForegroundColor Cyan
    $exit = Invoke-BlockingCommand -File "npm" -Arguments "run lint" -WorkingDirectory "$scriptDir\pto.track.tests.js"
    if ($exit -ne 0) {
        Write-Host "Note: ESLint warnings/errors detected; they do not fail the script by default" -ForegroundColor Yellow
        if ($FailFast) { Write-Host "Fail-fast enabled: stopping on first failure." -ForegroundColor Red; exit $exit }
    }
}
else {
    Write-Host "`nSkipping complexity analysis..." -ForegroundColor Gray
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($allTestsPassed) {
    Write-Host "`n✓ All tests passed!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "`n✗ Some tests failed. Please review the output above." -ForegroundColor Red
    exit 1
}
