param([switch]$IncludeVS)
$targets = @('dotnet', 'MSBuild', 'VBCSCompiler', 'devenv')
if ($IncludeVS) { $targets += 'Code' }
foreach ($name in $targets) {
    Get-Process -Name $name -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}
dotnet build-server shutdown | Out-Null
