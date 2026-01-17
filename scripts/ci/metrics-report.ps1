param(
    [Parameter(Mandatory = $false)][string]$MetricsJsonPath,
    [Parameter(Mandatory = $false)][string]$OutputDirectory,
    [Parameter(Mandatory = $false)][int]$TopFiles = 50
)

function Write-Log {
    param([string]$Message)
    Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') | $Message"
}

try {
    if (-not $MetricsJsonPath) {
        $MetricsJsonPath = Join-Path (Get-Location) "artifacts/metrics/metrics.json"
    }
    if (-not $OutputDirectory) {
        $OutputDirectory = Join-Path (Get-Location) "artifacts/metrics-report"
    }

    Write-Log "Metrics JSON: $MetricsJsonPath"
    Write-Log "Output directory: $OutputDirectory"

    if (-not (Test-Path $MetricsJsonPath)) {
        throw "metrics.json not found at $MetricsJsonPath"
    }

    $metrics = Get-Content -Path $MetricsJsonPath -Raw | ConvertFrom-Json
    if (-not $metrics) {
        throw "Failed to parse metrics.json"
    }

    $summary = [ordered]@{
        "Generated (UTC)"       = [DateTime]::Parse($metrics.generatedAt).ToString("u")
        "Projects"              = $metrics.projectCount
        "Files"                 = $metrics.totalFiles
        "Lines"                 = $metrics.totalLines
        "Avg Lines per File"    = "{0:N2}" -f $metrics.avgLinesPerFile
        "Cyclomatic (sum)"      = $metrics.aggregatedCyclomatic
        "Halstead Volume (sum)" = "{0:N2}" -f $metrics.aggregatedHalstead
        "Maintainability Index" = "{0:N2}" -f $metrics.maintainabilityIndex
    }

    $fileList = @($metrics.files)
    $top = $fileList | Sort-Object { $_.cyclomatic } -Descending | Select-Object -First $TopFiles
    $lowMi = $fileList | Sort-Object { $_.maintainabilityIndex } | Select-Object -First $TopFiles

    $sb = [System.Text.StringBuilder]::new()

    $null = $sb.AppendLine('<!DOCTYPE html>')
    $null = $sb.AppendLine('<html lang="en">')
    $null = $sb.AppendLine('<head>')
    $null = $sb.AppendLine('  <meta charset="utf-8" />')
    $null = $sb.AppendLine('  <title>Code Metrics Report</title>')
    $null = $sb.AppendLine('  <style>')
    $null = $sb.AppendLine('    body { font-family: "Segoe UI", sans-serif; margin: 32px; background: #f7f7f7; color: #222; }')
    $null = $sb.AppendLine('    h1 { margin-top: 0; }')
    $null = $sb.AppendLine('    .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 12px; margin: 20px 0 32px; }')
    $null = $sb.AppendLine('    .card { background: #fff; padding: 14px 16px; border: 1px solid #ddd; border-radius: 6px; box-shadow: 0 1px 2px rgba(0,0,0,0.04); }')
    $null = $sb.AppendLine('    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #ddd; }')
    $null = $sb.AppendLine('    th, td { padding: 8px 10px; border-bottom: 1px solid #eee; text-align: left; font-size: 13px; }')
    $null = $sb.AppendLine('    th { background: #fafafa; font-weight: 600; }')
    $null = $sb.AppendLine('    tr:nth-child(even) { background: #fbfbfb; }')
    $null = $sb.AppendLine('    .path { word-break: break-all; }')
    $null = $sb.AppendLine('    .muted { color: #666; font-size: 12px; }')
    $null = $sb.AppendLine('  </style>')
    $null = $sb.AppendLine('</head>')
    $null = $sb.AppendLine('<body>')
    $null = $sb.AppendLine('  <h1>Code Metrics Report</h1>')
    $null = $sb.AppendLine('  <div class="muted">Source: metrics.json (metrics-runner)</div>')
    $null = $sb.AppendLine('  <div class="summary">')
    foreach ($k in $summary.Keys) {
        $v = $summary[$k]
        $null = $sb.AppendLine("    <div class='card'><div class='muted'>$k</div><div style='font-size:16px;font-weight:600;'>$v</div></div>")
    }
    $null = $sb.AppendLine('  </div>')

    $null = $sb.AppendLine("  <h2>Top $TopFiles Files by Cyclomatic Complexity</h2>")
    $null = $sb.AppendLine('  <table>')
    $null = $sb.AppendLine('    <thead><tr><th>#</th><th>File</th><th>Lines</th><th>Cyclomatic</th><th>Halstead</th><th>MI</th></tr></thead>')
    $null = $sb.AppendLine('    <tbody>')
    $row = 0
    foreach ($f in $top) {
        $row++
        $path = $f.path
        $lines = $f.lines
        $cyc = $f.cyclomatic
        $hal = "{0:N2}" -f $f.halsteadVolume
        $mi = "{0:N2}" -f $f.maintainabilityIndex
        $null = $sb.AppendLine("      <tr><td>$row</td><td class='path'>$path</td><td>$lines</td><td>$cyc</td><td>$hal</td><td>$mi</td></tr>")
    }
    $null = $sb.AppendLine('    </tbody>')
    $null = $sb.AppendLine('  </table>')

    $null = $sb.AppendLine("  <h2>Bottom $TopFiles Files by Maintainability Index</h2>")
    $null = $sb.AppendLine('  <table>')
    $null = $sb.AppendLine('    <thead><tr><th>#</th><th>File</th><th>Lines</th><th>Cyclomatic</th><th>Halstead</th><th>MI</th></tr></thead>')
    $null = $sb.AppendLine('    <tbody>')
    $row = 0
    foreach ($f in $lowMi) {
        $row++
        $path = $f.path
        $lines = $f.lines
        $cyc = $f.cyclomatic
        $hal = "{0:N2}" -f $f.halsteadVolume
        $mi = "{0:N2}" -f $f.maintainabilityIndex
        $null = $sb.AppendLine("      <tr><td>$row</td><td class='path'>$path</td><td>$lines</td><td>$cyc</td><td>$hal</td><td>$mi</td></tr>")
    }
    $null = $sb.AppendLine('    </tbody>')
    $null = $sb.AppendLine('  </table>')

    $null = $sb.AppendLine('  <p class="muted" style="margin-top:18px;">MI = Maintainability Index (0-100). Higher is better.</p>')
    $null = $sb.AppendLine('</body>')
    $null = $sb.AppendLine('</html>')

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $outPath = Join-Path $OutputDirectory "index.html"
    $sb.ToString() | Set-Content -Path $outPath -Encoding UTF8
    Write-Log "Wrote HTML report to $outPath"
    exit 0
}
catch {
    Write-Log "ERROR: $_"
    exit 1
}
