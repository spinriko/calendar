IIS release scripts

Files
- before-deploy-iis.ps1: pre-deploy checks and idempotent removal of app/app-pool when identity or path mismatches.
- finish-deploy-iis.ps1: post-deploy creation/update of app pool and application, Windows Authentication configuration, and app pool recycle.

Usage
From your deployment agent/host invoke the scripts in order:

1) Pre-deploy checks (before copying artifacts):
```powershell
.\efore-deploy-iis.ps1 -PhysicalPath 'C:\inetpub\wwwroot\pto-track' -AppPoolUser 'DOMAIN\KestrelSvc' -AppPoolPassword '<secret>'
```

2) Copy publish artifacts into the physical path (use CopyFiles task, robocopy, etc.)

3) Post-deploy configuration:
```powershell
.\inish-deploy-iis.ps1 -PhysicalPath 'C:\inetpub\wwwroot\pto-track' -AppPoolUser 'DOMAIN\KestrelSvc' -AppPoolPassword '<secret>'
```

Agent & module notes
- These scripts require administrative privileges and the ability to import the `WebAdministration` module. If running PowerShell 7, the scripts attempt compatibility and fallback to `IISAdministration`.
- Recommended: run on a self-hosted Azure DevOps agent installed on the IIS host or use WinRM `Invoke-Command` to run the scripts remotely.
- Keep secrets out of source control: use pipeline secret variables or Azure Key Vault to supply `AppPoolPassword`.

Security
- The scripts will only remove an existing app pool if the identity differs from the desired identity. They will not indiscriminately delete app pools or apps.
- Review and test scripts in staging before production.
