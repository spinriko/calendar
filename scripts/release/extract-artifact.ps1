param(
    [Parameter(Mandatory = $true)][string]$SourceFolder,
    [Parameter(Mandatory = $true)][string]$DestinationFolder
)

function Write-Log($m) { Write-Host "[extract-artifact] $m" }

Write-Log "Looking for ZIP files in $SourceFolder..."

try {
    $zipFiles = Get-ChildItem -Path $SourceFolder -Recurse -Filter "*.zip" -ErrorAction SilentlyContinue
    
    if (-not $zipFiles) {
        Write-Error "No ZIP files found in $SourceFolder"
        exit 1
    }
    
    foreach ($zip in $zipFiles) {
        Write-Log "Extracting $($zip.Name) to $DestinationFolder..."
        Expand-Archive -Path $zip.FullName -DestinationPath $DestinationFolder -Force
        Remove-Item -Path $zip.FullName -Force
        Write-Log "✓ Extracted and removed $($zip.Name)"
    }
    
    Write-Log "Done."
    exit 0
} catch {
    Write-Error "Error extracting archive: $_"
    exit 1
}
