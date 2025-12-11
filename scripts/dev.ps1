param(
    [Parameter(Position = 0)]
    [ValidateSet('help', 'build', 'run', 'frontend', 'fixtures', 'test', 'analyzers', 'clean', 'all')]
    [string]$Command = 'help',
    [switch]$Execute,
    [string]$Configuration = 'Debug'
)

function Write-Usage {
    @"
Usage: pwsh ./scripts/dev.ps1 [command] [-Execute] [-Configuration <Debug|Release>]

Commands:
  help        Show this help
  build       Build the solution (`pto.track.sln`)
  run         Run the web app (uses `dotnet watch run`)
  frontend    Run frontend npm install + build (prefix: `pto.track`)
  fixtures    Update headless fixtures from manifest
  test        Run unit tests (dotnet tests + npm tests)
  analyzers   Run analyzers via `scripts/run-analyzers.ps1` (requires -Execute to actually run)
  clean       Clean workspace (git clean -fdx + dotnet clean) -- requires -Execute
  all         Full flow: frontend -> fixtures -> build -> test

Examples:
  pwsh ./scripts/dev.ps1 frontend
  pwsh ./scripts/dev.ps1 fixtures
  pwsh ./scripts/dev.ps1 build -Configuration Release
  pwsh ./scripts/dev.ps1 analyzers -Execute
  pwsh ./scripts/dev.ps1 clean -Execute
"@
}

function Run-Frontend {
    Write-Host "Running frontend build (npm install && npm run build) in 'pto.track'..."
    & npm install --prefix pto.track
    if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
    & npm run build --prefix pto.track
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }
}

function Update-Fixtures {
    $script = Join-Path $PSScriptRoot '..\pto.track\scripts\update-fixtures-from-manifest.js'
    if (-not (Test-Path $script)) { Write-Warning "Fixture update script not found: $script"; return }
    Write-Host "Updating fixtures from manifest using: $script"
    & node $script
    if ($LASTEXITCODE -ne 0) { throw "Fixture updater failed" }
}

function Run-Build {
    Write-Host "Building solution 'pto.track.sln' (Configuration: $Configuration)"
    & dotnet build pto.track.sln -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
}

function Run-Tests {
    Write-Host "Running .NET tests with analyzers disabled (safe default)"
    & dotnet test .\pto.track.services.tests\pto.track.services.tests.csproj /p:RunAnalyzersDuringBuild=false -v minimal
    & dotnet test .\pto.track.tests\pto.track.tests.csproj /p:RunAnalyzersDuringBuild=false -v minimal
    & dotnet test .\pto.track.data.tests\pto.track.data.tests.csproj /p:RunAnalyzersDuringBuild=false -v minimal
    Write-Host "Running JS tests (if present)"
    if (Test-Path "pto.track.tests.js\package.json") {
        & npm install --prefix pto.track.tests.js
        & npm test --prefix pto.track.tests.js
    }
    else { Write-Host "No JS test project found at pto.track.tests.js" }
}

function Run-Analyzers {
    $script = Join-Path $PSScriptRoot 'run-analyzers.ps1'
    if (-not (Test-Path $script)) { Write-Warning "Analyzers runner not found: $script"; return }
    if ($Execute) {
        & pwsh $script -Execute
        exit $LASTEXITCODE
    }
    else {
        Write-Host "Analyzer script dry-run. Re-run with -Execute to actually run analyzers."
        & pwsh $script
    }
}

function Do-Clean {
    if (-not $Execute) { throw "Clean is destructive. Re-run with -Execute to perform clean." }
    Write-Host "Cleaning workspace (git clean -fdx)..."
    & git clean -fdx
    Write-Host "Running dotnet clean"
    & dotnet clean pto.track.sln
}

try {
    switch ($Command) {
        'help' { Write-Usage }
        'frontend' { Run-Frontend }
        'fixtures' { Update-Fixtures }
        'build' { Run-Build }
        'run' {
            Write-Host "Starting app with dotnet watch run..."
            & dotnet watch run --project pto.track/pto.track.csproj
        }
        'test' { Run-Tests }
        'analyzers' { Run-Analyzers }
        'clean' { Do-Clean }
        'all' {
            Run-Frontend
            Update-Fixtures
            Run-Build
            Run-Tests
        }
        default { Write-Usage }
    }
}
catch {
    Write-Error "Command failed: $_"
    exit 1
}
