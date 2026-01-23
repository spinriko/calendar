# Node.js Setup in a Locked-Down Corporate Environment

This document explains how Node.js is configured for this project inside a restricted corporate Windows environment. The goal is to give you a deterministic, portable setup that works consistently across PowerShell 7, Windows PowerShell 5.1, and VS Code — without relying on system-installed Node or admin rights.

This setup avoids all the usual corporate blockers: redirected Documents folders, OneDrive/DFS virtualization, blocked installers, locked-down PATH, MITM HTTPS inspection, and GPO-controlled PowerShell behavior.

## Why We Use a Portable Node Setup

Corporate Windows environments often enforce:

- Redirected Documents folders (UNC paths)
- OneDrive/DFS/Offline Files virtualization
- Blocked installers
- Locked-down PATH
- MITM HTTPS inspection
- GPO-controlled $PSModulePath
- No admin rights

Traditional Node installation methods (MSI installers, nvm-windows, volta, fnm) are unreliable or unusable under these constraints.

To avoid all of that, we use:

- Portable Node versions stored under the user profile
- A PowerShell module (pwsh-nvm) for version switching
- A dispatcher function (nvm) for a clean CLI
- Explicit file-path imports (no reliance on $PSModulePath)
- NODE_EXTRA_CA_CERTS for corporate TLS interception

This gives us a stable, predictable Node environment that works everywhere.

# 1. Portable Node Layout

All Node versions live under a user-controlled directory:

```
C:\Users\<you>\.node\
    18.19.0\
        node.exe
        npm
        npx
    20.11.1\
        node.exe
        npm
        npx
```

Each version is self-contained and does not modify system directories or PATH.

# 2. Corporate TLS: NODE_EXTRA_CA_CERTS

The corporate network performs HTTPS interception using a private root CA.  
Node.js does not use the Windows certificate store, so it must be explicitly told to trust the corporate CA.

Set this User Environment Variable:

```
NODE_EXTRA_CA_CERTS=C:\admin\certs\asb-root.cer
```

This ensures:

- npm install works
- npx works
- esbuild/vite/webpack downloads work
- Any Node HTTPS request succeeds

No system-wide trust store changes are required.

# 3. The pwsh-nvm PowerShell Module

The custom module provides:

- Get-NodeVersions
- Set-NodeVersion
- Get-CurrentNodeVersion
- (future) Install-NodeVersion

It lives here:

```
C:\Users\<you>\Documents\PowerShell\Modules\pwsh-nvm\
```

Because $PSModulePath is unreliable in this environment, the module is imported by absolute file path in the PowerShell profile.

# 4. PowerShell Profile Integration

In your PowerShell 7 profile:

```powershell
$modulePath = "$env:USERPROFILE\Documents\PowerShell\Modules\pwsh-nvm\pwsh-nvm.psm1"
if (Test-Path $modulePath) {
    Import-Module $modulePath -Force
}
```

This avoids issues with:

- Redirected profiles
- $PSModulePath being wiped by GPO
- VS Code terminal inconsistencies

# 5. The nvm Dispatcher Function

PowerShell cannot alias multi-word commands like "nvm list", so we use a dispatcher function:

```powershell
function nvm {
    param(
        [Parameter(ValueFromRemainingArguments)]
        [string[]]$Args
    )

    switch ($Args[0]) {
        'list'     { Get-NodeVersions }
        'use'      { Set-NodeVersion $Args[1] }
        'current'  { Get-CurrentNodeVersion }
        default    { Write-Error "Unknown nvm command: $($Args[0])" }
    }
}
```

This gives you a familiar UX:

```
nvm list
nvm use 20
nvm current
```

# 6. How Node Version Switching Works

When running:

```
nvm use 20
```

The module:

1. Sets $env:NODE_PATH to the selected version  
2. Prepends that version's bin directory to $env:PATH (session-only)  
3. Ensures node, npm, and npx resolve to the portable version  

No system PATH changes.  
No admin rights required.

# 7. VS Code Behavior

VS Code launches two shells:

## PowerShell Extension Host
- Used for IntelliSense and debugging  
- Does not load your profile  
- Should be ignored for actual work  

## Your PowerShell Terminal
- Loads your profile  
- Loads pwsh-nvm  
- Supports nvm commands  
- Should be used for all Node tasks  

Always run Node commands in your terminal, not the extension's.

# 8. Running the Web App

After selecting a Node version:

```
nvm use 20
npm install
npm run dev
```

Everything runs against the portable Node version.

# 9. Troubleshooting

## Node version not switching

```
nvm current
```

If empty:

```
. $PROFILE
```

## VS Code using the wrong Node

Use:

```
Terminal → New Terminal → PowerShell
```

Do not use the "PowerShell Extension" terminal.

## Module not loading

```
Test-Path "$env:USERPROFILE\Documents\PowerShell\Modules\pwsh-nvm"
```

# 10. Future Enhancements

The module is designed to support:

- nvm install <version>
- Automatic downloads
- Version caching
- Tab-completion for subcommands
- Version completion for nvm use

# Summary

This setup provides:

- A deterministic Node environment  
- No reliance on system installs  
- No PATH modifications  
- No admin rights required  
- A clean nvm-style CLI  
- Compatibility with pwsh 7 and 5.1  
- Stability inside a heavily restricted corporate environment  
- TLS compatibility via NODE_EXTRA_CA_CERTS  
