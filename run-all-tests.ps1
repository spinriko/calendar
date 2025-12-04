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
    [switch]$SkipComplexity
)

$ErrorActionPreference = "Continue"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$allTestsPassed = $true

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  PTO Tracking Solution - Test Suite" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# C# Tests
if (-not $SkipCSharp) {
    Write-Host "`n--- Running C# Unit Tests (pto.track.services.tests) ---" -ForegroundColor Yellow
    Push-Location "$scriptDir\pto.track.services.tests"
    dotnet test --verbosity normal
    if ($LASTEXITCODE -ne 0) { $allTestsPassed = $false }
    Pop-Location

    Write-Host "`n--- Running C# Integration Tests (pto.track.tests) ---" -ForegroundColor Yellow
    Push-Location "$scriptDir\pto.track.tests"
    dotnet test --verbosity normal
    if ($LASTEXITCODE -ne 0) { $allTestsPassed = $false }
    Pop-Location
}
else {
    Write-Host "`nSkipping C# tests..." -ForegroundColor Gray
}

# JavaScript Tests
if (-not $SkipJavaScript) {
    Write-Host "`n--- Running JavaScript Tests (pto.track.tests.js) ---" -ForegroundColor Yellow
    Push-Location "$scriptDir\pto.track.tests.js"
    
    # Check if node_modules exists
    if (-not (Test-Path "node_modules")) {
        Write-Host "Installing npm dependencies..." -ForegroundColor Cyan
        npm install
    }
    
    npm test
    if ($LASTEXITCODE -ne 0) { $allTestsPassed = $false }
    Pop-Location
}
else {
    Write-Host "`nSkipping JavaScript tests..." -ForegroundColor Gray
}

# Cyclomatic Complexity Analysis
if (-not $SkipComplexity) {
    Write-Host "`n--- Running Cyclomatic Complexity Analysis ---" -ForegroundColor Yellow
    
    Write-Host "`nC# Complexity Analysis:" -ForegroundColor Cyan
    Push-Location "$scriptDir\pto.track.tests"
    dotnet test --filter "FullyQualifiedName~CodeMetricsAnalyzer" --verbosity normal
    if ($LASTEXITCODE -ne 0) { $allTestsPassed = $false }
    Pop-Location
    
    Write-Host "`nJavaScript Complexity Analysis:" -ForegroundColor Cyan
    Push-Location "$scriptDir\pto.track.tests.js"
    npm run lint
    if ($LASTEXITCODE -ne 0) { 
        Write-Host "Note: ESLint warnings do not fail the build" -ForegroundColor Yellow
    }
    Pop-Location
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
