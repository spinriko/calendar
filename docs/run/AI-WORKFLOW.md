# AI-Assisted Development Workflow

This document describes the workflow for developing with AI assistance on a personal GitHub remote while maintaining corp as the source of truth.

## Overview

- **Corp Remote**: Source of truth; pipeline runs here and auto-creates version tags
- **GitHub Remote**: Development workspace for AI-assisted feature work
- **Local Main**: Tracks corp/main

## One-Time Setup

```powershell
# Ensure remotes are configured
git remote add corp <corp-ado-url>
git remote add github git@github.com:spinriko/calendar.git

# Set local main to track corp
git checkout main
git branch --set-upstream-to=corp/main
```

## Development Workflow

### 1. Start Feature Branch

On corp or home machine:

```powershell
git checkout -b feature/my-work
git push github feature/my-work
```

### 2. Develop with AI (Home Machine)

```powershell
# Check out feature branch
git checkout feature/my-work
git branch --set-upstream-to=github/feature/my-work

# Develop with AI assistance
# ... make changes, commit ...
git commit -am "Add feature"
git push  # Pushes to GitHub
```

### 3. Move to Corp Machine

```powershell
# Fetch changes from GitHub
git fetch github
git checkout feature/my-work
git pull github feature/my-work  # Get all commits from GitHub
```

### 4. Submit to Corp

```powershell
# Push feature branch to corp
git push corp feature/my-work

# Create PR on corp: feature/my-work → main
# Get PR reviewed and merged
# Pipeline runs on merge and auto-creates version tag
```

### 5. Sync Back After Merge

After corp PR merges and pipeline completes:

```powershell
# Update local main from corp (includes new tags)
git checkout main
git pull corp main

# Mirror corp main and tags to GitHub
git push github main --force
git push github --tags --force
```

## Version Tag Flow

```
Corp Pipeline → Auto-creates tag (e.g., v0.1.1)
              ↓
Local Machine → git pull corp main (gets tags)
              ↓
GitHub Remote → git push github --tags --force (syncs tags)
```

## Manual Version Tags

To manually set a major or minor version (e.g., `v1.0.0` or `v1.1.0`):

1. Create annotated tag locally:
   ```powershell
   git tag -a v1.0.0 -m "Release 1.0.0"
   ```

2. Push to corp before merging PR:
   ```powershell
   git push corp v1.0.0
   ```

3. Merge PR - pipeline will detect tag and skip auto-increment

4. Sync to GitHub:
   ```powershell
   git checkout main
   git pull corp main
   git push github main --force
   git push github --tags --force
   ```

## Key Principles

- **Never force-push to corp main** (use PRs)
- **Corp is single source of truth** for tags and main branch
- **GitHub is your workspace** for AI-assisted development
- **Always sync tags** after corp merges (corp → local → GitHub)
- **Feature branches can live on GitHub** until ready for corp PR

## Troubleshooting

### Tags out of sync

```powershell
# Re-sync tags from corp
git fetch corp --tags
git push github --tags --force
```

### Main branches diverged

```powershell
# Reset local and GitHub to match corp
git checkout main
git fetch corp
git reset --hard corp/main
git push github main --force
```
