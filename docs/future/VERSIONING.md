# Semantic Versioning Plan

This document outlines the plan to implement Semantic Versioning (SemVer) for the `pto.track` application.

## 1. Strategy: Git Tags as Source of Truth

We will use **Git Tags** to drive the versioning. This ensures that the version in the codebase always matches the release state in the repository.

- **Format**: `vX.Y.Z` (e.g., `v1.0.0`, `v1.1.0-rc.1`)
- **Workflow**:
    1.  Develop features on branches.
    2.  Merge to `main`.
    3.  Tag the commit on `main` with the new version.
    4.  The build pipeline triggers and produces artifacts with that version.

## 2. Tooling: MinVer

We will use [MinVer](https://github.com/adamralph/minver), a simple tool that reads git tags and sets the .NET version properties (`Version`, `AssemblyVersion`, `FileVersion`, `InformationalVersion`) automatically during the build.

### Why MinVer?
- **Zero Config**: Works out of the box with standard git tags.
- **SDK Integration**: It's just a NuGet package. No external CLI tools needed in the pipeline (unlike GitVersion).
- **Prerelease Support**: Handles alpha/beta tags automatically (e.g., `1.0.0-alpha.1`).

## 3. Implementation Steps

### A. Application Setup (One-time)

1.  **Add NuGet Package**:
    Add `MinVer` to the main project file (`pto.track/pto.track.csproj`).
    ```xml
    <ItemGroup>
      <PackageReference Include="MinVer" Version="6.0.0">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      </PackageReference>
    </ItemGroup>
    ```

2.  **Expose Version in UI (Optional but Recommended)**:
    Update `_Layout.cshtml` or a specific "About" page to display the version.
    ```csharp
    // In a Razor view or Controller
    var version = System.Reflection.Assembly.GetEntryAssembly()
                    ?.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion;
    ```

3.  **API Endpoint (Optional)**:
    Create a `VersionController` or add to `HomeController` to return the current version for monitoring/diagnostics.

### B. Pipeline Integration

1.  **Fetch Tags**:
    Ensure the CI/CD pipeline performs a deep fetch or specifically fetches tags.
    *   *GitHub Actions*: `fetch-depth: 0`
    *   *Azure DevOps*: Ensure "Sync tags" is enabled.

2.  **Build**:
    No changes needed to the `dotnet build` command. MinVer runs automatically.
    ```bash
    dotnet publish -c Release
    ```
    The resulting DLLs will have the correct file version.

3.  **Docker / Artifacts**:
    Use the calculated version to tag Docker images or NuGet packages.
    You can extract the version using a simple script or by inspecting the built assembly if the pipeline needs the string for tagging external artifacts.

## 4. Versioning Rules (SemVer 2.0.0)

- **Major (X.0.0)**: Incompatible API changes.
- **Minor (0.Y.0)**: Backwards-compatible functionality added.
- **Patch (0.0.Z)**: Backwards-compatible bug fixes.

## 5. Next Steps

1.  Install `MinVer` package.
2.  Create an initial tag (e.g., `v0.1.0`) on the current commit.
3.  Verify `dotnet build` produces a binary with version `0.1.0`.

## 6. Multi-Project Versioning Strategy

Since the solution contains multiple projects (`pto.track`, `pto.track.services`, `pto.track.data`), we need to decide how they are versioned relative to each other.

### Recommendation: Lockstep Versioning (Unified)

We should version all projects with the **same version number**.

**Reasoning:**
1.  **Internal Dependencies**: The `services` and `data` layers are currently internal implementation details of the `pto.track` application. They are not distributed as standalone NuGet packages for third-party consumption.
2.  **Simplicity**: Managing a single version number (e.g., "The Release 1.2.0") is significantly easier for deployment, bug tracking, and communication than tracking "Web 1.2.0, Services 1.1.5, Data 1.0.2".
3.  **MinVer Default**: By default, MinVer applies the git tag version to all projects in the build.

### Implementation for Lockstep

To ensure all projects get the version, we should move the `MinVer` package reference to a solution-level configuration rather than just the web project.

1.  **Create `Directory.Build.props`** in the solution root:
    ```xml
    <Project>
      <ItemGroup>
        <PackageReference Include="MinVer" Version="6.0.0">
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
      </ItemGroup>
    </Project>
    ```
2.  This ensures `pto.track`, `pto.track.services`, and `pto.track.data` all automatically inherit the versioning logic.

### Alternative: Independent Versioning (Not Recommended)

If we ever decide to publish `pto.track.services` as a library for *other* applications to use, we would switch to independent versioning. This would require:
- Tagging with prefixes (e.g., `services-v1.0.0`, `app-v2.0.0`).
- Configuring MinVer to look for specific tag prefixes per project.
- **Why avoid now?**: It adds significant complexity to the release process and is unnecessary while the code exists in a monorepo serving a single product.

## 7. Automating Version Increments (CI/CD)

To achieve auto-incrementing versions on `main` (e.g., `1.0.1` -> `1.0.2`) while allowing manual overrides for Major/Minor releases, we can implement a **Tag-on-Merge** workflow.

### The Workflow

MinVer relies on tags. To get a new "official" version, we must create a tag. We can automate this in the pipeline.

1.  **Manual Overrides (Major/Minor)**:
    When you want a specific version (e.g., `v2.0.0` or `v1.1.0`), you manually push that tag to the commit *before* or *as* you merge.
    *   `git tag v1.1.0`
    *   `git push origin v1.1.0`
    *   The pipeline builds `1.1.0`.

2.  **Auto-Increment (Patch)**:
    If *no* tag is present on the merge commit, the pipeline script detects the last tag (e.g., `v1.1.0`), increments the patch (to `1.1.1`), and pushes the new tag automatically.

### Implementation Logic (Pseudo-code for Pipeline)

```yaml
# After checkout
- name: Calculate and Push Tag
  if: github.ref == 'refs/heads/main'
  run: |
    # 1. Get latest tag
    LAST_TAG=$(git describe --tags --abbrev=0)
    # e.g., v1.0.0

    # 2. Check if current commit is already tagged (Manual Override)
    CURRENT_TAG=$(git tag --points-at HEAD)
    if [ -n "$CURRENT_TAG" ]; then
      echo "Manual tag detected: $CURRENT_TAG. Skipping auto-tag."
      exit 0
    fi

    # 3. Increment Patch
    # Parse v1.0.0 -> 1.0.1
    # (Scripting required here, e.g., using a simple node/powershell script)
    NEW_TAG="v1.0.1"

    # 4. Push Tag
    git tag $NEW_TAG
    git push origin $NEW_TAG
```

### Resulting Version History
*   `v1.0.0` (Manual)
*   Merge PR A -> Auto-tags `v1.0.1`
*   Merge PR B -> Auto-tags `v1.0.2`
*   `v1.1.0` (Manual Push)
*   Merge PR C -> Auto-tags `v1.1.1`

This satisfies the requirement:
*   **Auto-increment**: Happens via the pipeline script bumping the patch number.
*   **Manual changes**: Happens by manually pushing a tag for Major/Minor updates.
