# CI Pipeline Scripts

This document describes PowerShell scripts used during the continuous integration (CI) pipeline build and test stages.

## Scripts Summary

All scripts are located in `scripts/ci/` and follow a consistent pattern with parameter validation, logging, and error handling.

### CI Scripts

| Script | Purpose | Parameters | Pipeline Stage |
|--------|---------|-----------|----------------|
| **version-tag.ps1** | Auto-increment semantic version tags, generate version badges (SVG/PNG), commit and push to repository | `-BuildSourcesDirectory` (optional, defaults to current directory) | Build |
| **coverage-merge.ps1** | Merge Cobertura coverage reports from multiple test projects using ReportGenerator | `-SourcesDirectory` (optional, defaults to current directory) | Test |

## Script Details

### version-tag.ps1

**Purpose:** Automates semantic versioning by:
1. Fetching existing version tags from Git
2. Detecting manual tags on current commit (skips auto-tagging if found)
3. Incrementing patch version from last tag (or creating v0.1.0 if none exist)
4. Creating and pushing new version tag
5. Generating SVG and PNG version badges
6. Committing and pushing badges to main branch with [skip ci]

**Parameters:**
- `BuildSourcesDirectory` (optional): Path to repository root. Defaults to `$env:BUILD_SOURCESDIRECTORY` or current location.

**Pipeline Variables Set:**
- `VersionTag`: The newly created version tag (e.g., "v1.2.3")

**Example Usage:**
```powershell
# In pipeline
& .\scripts\ci\version-tag.ps1 -BuildSourcesDirectory "$(Build.SourcesDirectory)"

# Manual execution
.\scripts\ci\version-tag.ps1 -BuildSourcesDirectory "C:\code\dotnet\pto"
```

**Prerequisites:**
- Git installed and configured
- Write access to repository
- Persistent credentials enabled in Azure Pipelines checkout

**Output Artifacts:**
- New Git tag pushed to origin
- `docs/badges/version.svg` - SVG badge for GitHub/web
- `docs/badges/version.png` - PNG badge for Azure DevOps Server markdown

**Behavior Notes:**
- Only creates tags on main branch (controlled by pipeline condition)
- Skips tagging if commit already has a manual tag
- Lists last 10 tags for debugging
- Includes remote verification after push

---

### coverage-merge.ps1

**Purpose:** Merges multiple Cobertura coverage reports from different test projects into a single consolidated report for Azure DevOps code coverage display.

**Parameters:**
- `SourcesDirectory` (optional): Path to repository root. Defaults to `$env:BUILD_SOURCESDIRECTORY` or current location.

**Dependencies:**
- `dotnet-reportgenerator-globaltool` (auto-installed if missing)

**Example Usage:**
```powershell
# In pipeline
& .\scripts\ci\coverage-merge.ps1 -SourcesDirectory "$(Build.SourcesDirectory)"

# Manual execution
.\scripts\ci\coverage-merge.ps1 -SourcesDirectory "C:\code\dotnet\pto"
```

**Input Files:**
- `TestResults/**/coverage.cobertura.xml` - Coverage files from individual test runs

**Output Files:**
- `TestResults/coverage-merged/Cobertura.xml` - Merged coverage report

**Behavior Notes:**
- Automatically installs ReportGenerator if not found in PATH
- Adds `$env:USERPROFILE\.dotnet\tools` to PATH for global tool resolution
- Fails if ReportGenerator encounters errors during merge

---

## Azure Pipeline Integration

These scripts are invoked from `azure-pipelines.yml` using inline PowerShell tasks:

### Build Stage Integration
```yaml
- powershell: |
    & .\scripts\ci\version-tag.ps1 -BuildSourcesDirectory "$(Build.SourcesDirectory)"
  displayName: "Auto-increment version tag"
  condition: and(succeeded(), ne(variables['Build.Reason'], 'PullRequest'), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
```

**Conditions:**
- Only runs on main branch pushes
- Skips on pull requests
- Skips if previous build steps failed

### Test Stage Integration
```yaml
- powershell: |
    & .\scripts\ci\coverage-merge.ps1 -SourcesDirectory "$(Build.SourcesDirectory)"
  displayName: "Merge coverage with ReportGenerator"
```

---

## Script Features

All scripts include:
- ✅ **Parameter defaults**: Fallback to environment variables or current directory
- ✅ **Consistent logging**: `Write-Log` function with timestamps
- ✅ **Error handling**: try-catch blocks with detailed error messages
- ✅ **Exit codes**: 0 for success, 1 for failure
- ✅ **Azure DevOps integration**: Uses `##vso[]` logging commands where appropriate

---

## Execution Order (Pipeline Sequence)

1. **version-tag.ps1** - Creates version tag early in Build stage (before build artifacts)
2. **coverage-merge.ps1** - Runs after all test projects complete in Test stage

---

## Version Badge Details

The version badge is generated in two formats:

**SVG Badge (`docs/badges/version.svg`):**
- Preferred for GitHub, web browsers, and modern markdown renderers
- Scalable vector format (130x20px nominal)
- Left section: "version" label (gray #555)
- Right section: version number (blue #007ec6)
- Font: Verdana 11px

**PNG Badge (`docs/badges/version.png`):**
- Fallback for Azure DevOps Server (older versions)
- Raster format 130x20px
- Same color scheme as SVG
- Uses System.Drawing for GDI+ rendering

**Commit Strategy:**
- Both badges committed with `[skip ci]` message to prevent recursive builds
- Committed directly to main branch after tag creation
- Only commits if badge content changed

---

## Manual Execution Guidelines

### Testing version-tag.ps1 Locally

```powershell
# Dry-run simulation (manual review before push)
cd C:\code\dotnet\pto
git fetch --tags
git tag --list "v*" | Sort-Object | Select-Object -Last 5

# Actual execution (will create tag and push)
.\scripts\ci\version-tag.ps1
```

**⚠️ Warning:** Running `version-tag.ps1` manually will create and push a real Git tag. Use with caution outside of CI pipeline.

### Testing coverage-merge.ps1 Locally

```powershell
# Ensure test coverage files exist
cd C:\code\dotnet\pto
dotnet test --collect:"XPlat Code Coverage"

# Run merge
.\scripts\ci\coverage-merge.ps1

# Check output
Get-ChildItem .\TestResults\coverage-merged\
```

---

## Troubleshooting

### version-tag.ps1

**Problem:** "Remote does NOT show tag after push"
- **Cause:** Git push failed silently or insufficient repository permissions
- **Solution:** Verify pipeline has `persistCredentials: true` in checkout task and repo allows tag creation

**Problem:** Badge SVG/PNG not updating in repository
- **Cause:** Git working directory dirty or [skip ci] commit failing
- **Solution:** Check pipeline logs for commit errors; ensure `docs/badges/` directory exists

### coverage-merge.ps1

**Problem:** "reportgenerator not found"
- **Cause:** Global dotnet tools path not in PATH
- **Solution:** Script auto-installs; check if `$env:USERPROFILE\.dotnet\tools` is accessible

**Problem:** "No matching files found for reports pattern"
- **Cause:** Test projects didn't generate coverage.cobertura.xml files
- **Solution:** Ensure test tasks use `--collect:"XPlat Code Coverage"` argument

---

## Related Documentation

- **Deployment Scripts:** See [scripts/release/SCRIPTS.md](../release/SCRIPTS.md) for deployment-specific scripts
- **Pipeline Definition:** [azure-pipelines.yml](../../azure-pipelines.yml)
- **Test Configuration:** [run-all-tests.ps1](../../run-all-tests.ps1)
