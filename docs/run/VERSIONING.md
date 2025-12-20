# Semantic Versioning with MinVer

Status: **Implemented** (as of December 19, 2025)

This document describes the implemented Semantic Versioning (SemVer) strategy for `pto.track` using MinVer and Azure DevOps pipeline auto-tagging.

## 1. Strategy: Git Tags as Source of Truth

We use **Git Tags** to drive versioning. The version in compiled binaries always matches the release state in the repository.

- **Format**: `vX.Y.Z` (e.g., `v0.1.0`, `v1.0.0`, `v1.1.0`)
- **Tag Type**: Annotated tags (lightweight tags are ignored)
- **Prefix**: `v` (e.g., `v0.1.0` not `0.1.0`)

## 2. Tooling: MinVer

[MinVer](https://github.com/adamralph/minver) automatically reads git tags and sets .NET version properties during build.

### Why MinVer?
- **Zero Config**: Works out of the box with standard git tags.
- **SDK Integration**: Just a NuGet package; no external CLI tools needed in pipeline.
- **Prerelease Support**: Handles dev/alpha/beta tags automatically.
- **Multi-project Support**: Applies same version to all projects in solution.

### Version Output Format

**Example**: Tag `v0.1.0` at commit `533e7db`

```
ProductVersion: 0.1.0+533e7db56c1a80d238320ff7810a25a291628db2
FileVersion:    0.1.0.0
AssemblyVersion: 0.1.0.0
```

Without a tag, versions default to:
```
ProductVersion: 0.0.0-alpha.0.27+533e7db  (27 commits since last tag)
```

## 3. Implementation

### A. Application Setup (Completed)

MinVer is configured in `Directory.Build.props`:

```xml
<!-- MinVer: Automatic versioning from git tags -->
<PackageReference Include="MinVer" Version="6.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>

<!-- MinVer: Use 'v' prefix for tags (e.g., v1.0.0) -->
<MinVerTagPrefix>v</MinVerTagPrefix>
```

All projects (`pto.track`, `pto.track.services`, `pto.track.data`) automatically inherit this configuration.

### B. Pipeline Integration (Completed)

The Azure DevOps pipeline automatically creates version tags on `main` branch merges.

#### Auto-Tagging Logic

**Location**: `azure-pipelines.yml` Build stage

**Behavior**:
1. Runs **only** on main branch after checkout
2. Checks if current commit has a manual tag
   - **Yes** → Skip (manual override; e.g., `v1.0.0` for major/minor release)
   - **No** → Continue to step 3
3. Gets last version tag from repo
4. Parses and increments patch version
5. Creates annotated tag and pushes to remote

**Example progression**:
```
v0.1.0 (initial or manual)
  ↓ (PR merged)
v0.1.1 (auto-incremented)
  ↓ (PR merged)
v0.1.2 (auto-incremented)
  ↓ (manual major bump)
v1.0.0 (pre-tagged before merge)
  ↓ (PR merged)
v1.0.1 (auto-incremented)
```

#### Pipeline Requirements

- **Permissions**: Build service account must have "Contribute" permission on repo to push tags
- **Fetch Behavior**: `persistCredentials: true`, `fetchDepth: 0`, `fetchTags: true` (enabled in pipeline)
- **Git Config**: Pipeline sets user.email/user.name for tag creation

## 4. Versioning Rules (SemVer 2.0.0)

- **Major (X.0.0)**: Incompatible API changes; manually set before merge
- **Minor (0.Y.0)**: Backwards-compatible functionality added; manually set before merge
- **Patch (0.0.Z)**: Backwards-compatible bug fixes; auto-incremented by pipeline

## 5. Multi-Project Versioning Strategy

### Lockstep Versioning (Implemented)

All projects share the **same version number**:
- `pto.track` v0.1.0
- `pto.track.services` v0.1.0
- `pto.track.data` v0.1.0

**Rationale**:
1. Services and data layers are internal to the web app (not published as standalone packages)
2. Single version simplifies deployment, bug tracking, and communication
3. MinVer naturally applies one git tag version to all projects

## 6. Workflow for Developers

### Normal Feature Development

```powershell
# Create feature branch and develop
git checkout -b feature/my-work
# ... make changes, commit, push to corp PR ...

# After PR merges
git pull origin main  # Includes new auto-created tag
```

### Manual Major/Minor Release

Before merging PR to main:

```powershell
# Tag with specific version
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0

# Now merge PR - pipeline detects tag and skips auto-increment
```

### Dual-Remote Workflow (AI Assistance)

See [docs/run/AI-WORKFLOW.md](AI-WORKFLOW.md) for complete workflow using corp (source of truth) and GitHub (workspace).

**Key points**:
- Corp is single source of truth for tags
- Local main tracks corp/main
- After corp PR merges, sync tags: `git fetch corp --tags` then `git push github --tags --force`

## 7. Accessing Version Information

### In Application Code

```csharp
var version = System.Reflection.Assembly.GetEntryAssembly()
    ?.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion;
// Returns: "0.1.0+533e7db..." or "0.0.0-alpha.0.27+..."
```

### In File System

```powershell
(Get-Item "pto.track/bin/Release/net9.0/pto.track.dll").VersionInfo.ProductVersion
# Returns: "0.1.0+533e7db..."
```

### Local Build (No Tags)

```powershell
dotnet build
# MinVer auto-calculates: 0.0.0-alpha.0.X (safe for local dev)
```

## 8. Troubleshooting

### Version not updating in DLL

- **Cause**: Cached build files
- **Fix**: `dotnet clean && dotnet build`

### Pipeline fails to push tag

- **Cause**: Build account lacks "Contribute" permission
- **Fix**: Grant permission in Azure DevOps project settings

### Tags out of sync between corp and GitHub

- **Cause**: Forgot to sync after corp merge
- **Fix**: `git fetch corp --tags && git push github --tags --force`

## 9. Future Enhancements (Optional)

- Add version display to application UI (About page, API endpoint)
- Tag retention/cleanup policy if repo accumulates too many tags
- Prerelease tags (`v1.0.0-rc.1`) for release candidates
