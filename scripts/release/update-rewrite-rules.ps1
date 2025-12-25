param(
    [Parameter(Mandatory = $true)][string]$WebConfigPath,
    [string]$ForwardHttpUrl = "http://localhost:5139",
    [string]$ForwardHttpsUrl = "https://localhost:7241"
)

function Write-Log($m) { Write-Host "[update-rewrite-rules] $m" }

Write-Log "Loading web.config from $WebConfigPath..."

try {
    if (-not (Test-Path $WebConfigPath)) {
        Write-Error "web.config not found at $WebConfigPath"
        exit 1
    }
    
    [xml]$webConfigXml = Get-Content $WebConfigPath -Raw
    
    # Ensure the XML structure exists
    Write-Log "Building XML structure for rewrite rules..."
    $rulesNode = $webConfigXml.configuration.location.'system.webServer'.rewrite.rules
    if (-not $rulesNode) {
        $rewriteNode = $webConfigXml.configuration.location.'system.webServer'.rewrite
        if (-not $rewriteNode) {
            $systemWebServerNode = $webConfigXml.configuration.location.'system.webServer'
            $rewriteNode = $webConfigXml.CreateElement("rewrite")
            $systemWebServerNode.AppendChild($rewriteNode) | Out-Null
        }
        $rulesNode = $webConfigXml.CreateElement("rules")
        $rewriteNode.AppendChild($rulesNode) | Out-Null
    }
    
    # Add HTTP rewrite rule
    Write-Log "Adding HTTP forward rule → $ForwardHttpUrl"
    $httpRule = $webConfigXml.CreateElement("rule")
    $httpRule.SetAttribute('name', 'Forward HTTP')
    $httpRule.SetAttribute('stopProcessing', 'true')
    $httpAction = $webConfigXml.CreateElement('action')
    $httpAction.SetAttribute('type', 'Rewrite')
    $httpAction.SetAttribute('url', $ForwardHttpUrl)
    $httpRule.AppendChild($httpAction) | Out-Null
    $rulesNode.AppendChild($httpRule) | Out-Null
    
    # Add HTTPS rewrite rule
    Write-Log "Adding HTTPS forward rule → $ForwardHttpsUrl"
    $httpsRule = $webConfigXml.CreateElement('rule')
    $httpsRule.SetAttribute('name', 'Forward HTTPS')
    $httpsRule.SetAttribute('stopProcessing', 'true')
    $httpsAction = $webConfigXml.CreateElement('action')
    $httpsAction.SetAttribute('type', 'Rewrite')
    $httpsAction.SetAttribute('url', $ForwardHttpsUrl)
    $httpsRule.AppendChild($httpsAction) | Out-Null
    $rulesNode.AppendChild($httpsRule) | Out-Null
    
    # Save the updated XML
    Write-Log "Saving updated web.config..."
    $webConfigXml.Save($WebConfigPath)
    
    Write-Log "✓ Rewrite rules added successfully"
    exit 0
} catch {
    Write-Error "Error updating rewrite rules: $_"
    exit 1
}
