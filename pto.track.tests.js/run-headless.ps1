# Run JavaScript tests in headless Edge on Windows
# Usage: .\run-headless.ps1

$ErrorActionPreference = "Stop"

# Get the directory where this script is located
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "Starting temporary web server on port 9999..." -ForegroundColor Cyan
$serverJob = Start-Job -ScriptBlock {
    param($dir)
    Set-Location $dir
    python -m http.server 9999
} -ArgumentList $ScriptDir

# Give server time to start
Start-Sleep -Seconds 2

# Cleanup function
$cleanup = {
    Write-Host "Stopping web server..." -ForegroundColor Cyan
    Stop-Job -Job $serverJob
    Remove-Job -Job $serverJob
}

# Register cleanup on exit
Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action $cleanup | Out-Null

try {
    # Find Edge
    $edgePath = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
    if (-not (Test-Path $edgePath)) {
        $edgePath = "C:\Program Files\Microsoft\Edge\Application\msedge.exe"
    }
    
    if (-not (Test-Path $edgePath)) {
        Write-Host "Error: Microsoft Edge not found" -ForegroundColor Red
        Write-Host "Checked:" -ForegroundColor Yellow
        Write-Host "  - C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" -ForegroundColor Yellow
        Write-Host "  - C:\Program Files\Microsoft\Edge\Application\msedge.exe" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "Running JavaScript tests in headless Edge..." -ForegroundColor Cyan
    $testUrl = "http://localhost:9999/test-runner.html"
    Write-Host "Test URL: $testUrl" -ForegroundColor Gray
    
    # Run tests in headless mode
    $outputFile = "$env:TEMP\test-output.html"
    $process = Start-Process -FilePath $edgePath -ArgumentList @(
        "--headless=new",
        "--disable-gpu",
        "--disable-software-rasterizer",
        "--disable-dev-shm-usage",
        "--no-sandbox",
        "--disable-extensions",
        "--virtual-time-budget=10000",
        "--dump-dom",
        $testUrl
    ) -NoNewWindow -Wait -PassThru -RedirectStandardOutput $outputFile
    
    if ($process.ExitCode -ne 0) {
        Write-Host "✗ Error running tests" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ Tests completed" -ForegroundColor Green
    
    # Parse results from output
    $content = Get-Content $outputFile -Raw
    
    if ($content -match '(\d+) tests.*?(\d+) passed.*?(\d+) failed') {
        $total = $Matches[1]
        $passed = $Matches[2]
        $failed = $Matches[3]
        
        Write-Host "Results: $passed passed, $failed failed out of $total tests" -ForegroundColor Cyan
        
        if ($failed -eq "0") {
            Write-Host "✓ All tests passed" -ForegroundColor Green
            exit 0
        } else {
            Write-Host "✗ Some tests failed" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "✗ Could not parse test results" -ForegroundColor Red
        exit 1
    }
}
finally {
    & $cleanup
}
