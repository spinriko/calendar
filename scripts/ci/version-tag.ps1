param(
    [Parameter(Mandatory = $false)]
    [string]$BuildSourcesDirectory = $env:BUILD_SOURCESDIRECTORY
)

function Write-Log {
    param([string]$Message)
    Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') | $Message"
}

try {
    if (-not $BuildSourcesDirectory) {
        $BuildSourcesDirectory = Get-Location
    }
    
    Write-Log "Auto-increment version tag started"
    Write-Log "Sources directory: $BuildSourcesDirectory"
    
    # Git configuration
    git config user.email "devops@example.com"
    git config user.name "Azure DevOps"
    
    # Ensure remote tags are available locally
    git fetch --tags --prune origin
    $fetchExit = $LASTEXITCODE
    Write-Host "##[section]git fetch --tags exit code: $fetchExit"
    git tag --list "v*" | Sort-Object | Select-Object -Last 10 | ForEach-Object { Write-Host "tag: $_" }
    
    # Check if current commit already has a tag (manual override)
    $currentTag = git tag --points-at HEAD
    if ($currentTag) {
        Write-Host "##[section]Manual tag detected: $currentTag. Skipping auto-tag."
        Write-Log "Manual tag found; exiting early"
        exit 0
    }
    
    # Get the latest version tag (reachable or not)
    $ErrorActionPreference = 'Continue'
    $lastTag = $null
    # Prefer describe on latest tag commit, regardless of reachability
    $latestTagCommit = git rev-list --tags --max-count=1
    if ($latestTagCommit) {
        $lastTag = git describe --tags --abbrev=0 --match "v*.*.*" $latestTagCommit 2>&1
    }
    else {
        $lastTag = git describe --tags --abbrev=0 --match "v*.*.*" 2>&1
    }
    $describeExit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    Write-Host "##[section]git describe output: $lastTag"
    Write-Host "##[section]git describe exit code: $describeExit"
    
    if ($LASTEXITCODE -ne 0 -or -not $lastTag) {
        Write-Host "##[warning]No version tags found. Creating initial tag v0.1.0"
        $newTag = "v0.1.0"
    }
    else {
        Write-Host "##[section]Last tag: $lastTag"
        
        # Parse version (v1.2.3 -> 1.2.3)
        $version = $lastTag -replace '^v', ''
        $parts = $version -split '\.'
        $major = [int]$parts[0]
        $minor = [int]$parts[1]
        $patch = [int]$parts[2]
        
        # Increment patch
        $patch++
        $newTag = "v$major.$minor.$patch"
    }
    
    Write-Host "##[section]Creating and pushing tag: $newTag"
    git tag -a $newTag -m "Auto-increment patch version"
    git push origin $newTag

    # Verify tag exists on remote
    $remoteTags = git ls-remote --tags origin
    if ($remoteTags -match [Regex]::Escape($newTag)) {
        Write-Host "##[section]Verified remote contains tag $newTag"
    }
    else {
        Write-Warning "Remote does NOT show tag $newTag after push. Subsequent builds may re-create initial tag. Check repo permissions."
    }
    
    Write-Host "##vso[task.setvariable variable=VersionTag]$newTag"

    # Generate version badge SVG in repo (docs/badges/version.svg)
    $badgeDir = Join-Path $BuildSourcesDirectory "docs/badges"
    if (-not (Test-Path $badgeDir)) { New-Item -ItemType Directory -Path $badgeDir -Force | Out-Null }
    $badgePath = Join-Path $badgeDir "version.svg"
    $svg = @"
<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="130" height="20" role="img" aria-label="version: $newTag">
  <linearGradient id="s" x2="0" y2="100%">
    <stop offset="0" stop-color="#bbb" stop-opacity=".1"/>
    <stop offset="1" stop-opacity=".1"/>
  </linearGradient>
  <mask id="m"><rect width="130" height="20" rx="3" fill="#fff"/></mask>
  <g mask="url(#m)">
    <rect width="60" height="20" fill="#555"/>
    <rect x="60" width="70" height="20" fill="#007ec6"/>
    <rect width="130" height="20" fill="url(#s)"/>
  </g>
  <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" font-size="11">
    <text x="30" y="15">version</text>
    <text x="94" y="15">$newTag</text>
  </g>
</svg>
"@
    Set-Content -Path $badgePath -Value $svg -Encoding UTF8

    # Commit and push the updated badge to main (skip CI)
    try {
        git add $badgePath
        $staged = git diff --name-only --cached
        if ($staged) {
            $msg = "chore(badges): update version badge to $newTag [skip ci]"
            git commit -m $msg
            git push origin HEAD:main
            Write-Host "##[section]Pushed updated version badge to main"
        }
        else {
            Write-Host "##[section]No changes to version badge detected; skipping commit"
        }
    }
    catch {
        Write-Warning "Failed to commit/push version badge: $($_.Exception.Message)"
    }

    # Also generate a PNG badge for Azure DevOps Server markdown rendering
    try {
        Add-Type -AssemblyName System.Drawing
        $width = 130; $height = 20
        $bmp = New-Object System.Drawing.Bitmap($width, $height)
        $gfx = [System.Drawing.Graphics]::FromImage($bmp)
        $gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $leftBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0x55, 0x55, 0x55)) # #555
        $rightBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0x00, 0x7e, 0xc6)) # #007ec6
        $overlay = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(26, 0, 0, 0))
        $gfx.FillRectangle($leftBrush, 0, 0, 60, $height)
        $gfx.FillRectangle($rightBrush, 60, 0, ($width - 60), $height)
        # light gradient overlay approximation omitted for simplicity
        $font = New-Object System.Drawing.Font('Verdana', 9, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $gfx.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
        $gfx.DrawString('version', $font, $white, (New-Object System.Drawing.RectangleF(0, 0, 60, $height)), $format)
        $gfx.DrawString($newTag, $font, $white, (New-Object System.Drawing.RectangleF(60, 0, ($width - 60), $height)), $format)
        $pngPath = Join-Path $badgeDir 'version.png'
        $bmp.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $gfx.Dispose(); $bmp.Dispose(); $font.Dispose(); $white.Dispose(); $leftBrush.Dispose(); $rightBrush.Dispose(); $overlay.Dispose()
        git add $pngPath
        $stagedPng = git diff --name-only --cached
        if ($stagedPng) {
            $msg2 = "chore(badges): update PNG version badge to $newTag [skip ci]"
            git commit -m $msg2
            git push origin HEAD:main
            Write-Host "##[section]Pushed updated PNG version badge to main"
        }
        else {
            Write-Host "##[section]No changes to PNG version badge detected; skipping commit"
        }
    }
    catch {
        Write-Warning "Failed to generate/commit PNG badge: $($_.Exception.Message)"
    }

    Write-Log "Version tag creation completed successfully: $newTag"
    exit 0
}
catch {
    Write-Log "ERROR: $_"
    exit 1
}
