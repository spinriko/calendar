param(
    [Parameter(Mandatory = $false)]
    [string]$SourcesDirectory = $env:BUILD_SOURCESDIRECTORY
)

function Write-Log {
    param([string]$Message)
    Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') | $Message"
}

try {
    if (-not $SourcesDirectory) {
        $SourcesDirectory = Get-Location
    }
    
    Write-Log "Coverage merge started"
    Write-Log "Sources directory: $SourcesDirectory"
    
    # Add dotnet tools to PATH
    $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
    
    # Check if reportgenerator is installed, install if not
    if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
        Write-Log "reportgenerator not found, installing dotnet-reportgenerator-globaltool"
        dotnet tool install --global dotnet-reportgenerator-globaltool
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to install dotnet-reportgenerator-globaltool"
        }
    }
    else {
        Write-Log "reportgenerator already installed"
    }
    
    # Run reportgenerator to merge coverage files
    Write-Log "Merging coverage reports with ReportGenerator"
    $reports = Join-Path $SourcesDirectory "TestResults/**/coverage.cobertura.xml"
    $targetDir = Join-Path $SourcesDirectory "TestResults/coverage-merged"
    
    Write-Log "Input pattern: $reports"
    Write-Log "Output directory: $targetDir"
    
    reportgenerator "-reports:$reports" "-targetdir:$targetDir" "-reporttypes:Cobertura"
    
    if ($LASTEXITCODE -ne 0) {
        throw "ReportGenerator failed with exit code: $LASTEXITCODE"
    }
    
    Write-Log "Coverage merge completed successfully"
    exit 0
}
catch {
    Write-Log "ERROR: $_"
    exit 1
}
