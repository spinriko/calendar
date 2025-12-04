# Syncing Corrected Git History to Corp Remote

## Background
The commit history was rewritten to fix incorrect author email addresses:
- `pavan.sss1991@gmail.com` → `jakodo@spinriko.com`
- `tlittle@asbhawaii` → `tlittle@asbhawaii.com`

This rewrite changed all commit hashes, requiring a force-push to sync remotes.

## Prerequisites
- Temporarily remove branch protection policy on `main` branch in corp remote
- Ensure no one else is actively working on the repository

## Steps to Sync Corp Remote

### 1. Fetch Corrected History from GitHub
```powershell
git fetch origin
git checkout main
git reset --hard origin/main
```

### 2. Force-Push to Corp Remote
```powershell
git push --force <corp-remote-name> main
git push --force <corp-remote-name> bugfix/impersonation-test
git push --force <corp-remote-name> bugfix/null-warnings
```

### 3. Re-enable Branch Protection
Restore branch protection policy on `main` branch in corp remote settings.

## Post-Sync Actions
⚠️ **Important:** Notify anyone else with clones of this repository that the history has been rewritten. They will need to reset their local branches:

```powershell
git fetch origin
git checkout main
git reset --hard origin/main
```

## Verification
Confirm corrected email addresses in history:
```powershell
git log --pretty=format:"%ae" | Sort-Object -Unique
```

Expected emails:
- `code@daypilot.org`
- `jakodo@spinriko.com`
- `spinriko@users.noreply.github.com`
- `tim@spinriko.com`
- `tlittle@asbhawaii.com`
