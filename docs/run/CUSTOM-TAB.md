# Add a Custom Metrics Tab in Azure DevOps Server 2022

Use this to surface the metrics HTML report produced in the Analyze stage.

## Prerequisites
- Pipeline publishes the `metrics-report` artifact containing `index.html` (created by `scripts/ci/metrics-report.ps1`).
- You have permissions to view builds and artifacts.

## Steps (per build)
1) Open the completed build in ADO Server 2022.
2) In the Summary view, click the tab bar ellipsis (…) and choose **Add build tab** (or **Add artifact tab** depending on your version).
3) Pick the **Custom** tab type.
4) Point it to the HTML file inside the artifact. Example path: `#/artifacts?path=metrics-report%2Findex.html` (slashed path URL-encoded).
5) Save. A new tab will render the metrics report.

## Tips
- Keep the report self-contained (inline CSS/JS). The generated `index.html` already is.
- If the tab shows a blank page, verify the artifact exists for that build and that the URL path matches.
- Each build has its own artifact URL; add the tab on the build you want to inspect.
