# Run docs index

This folder contains the runbooks and developer guidance for running, testing, and troubleshooting the project.

- RUN-LOCAL.md — Local developer runbook (how to run, test, analyze, and debug locally).
- ANALYZERS.md — Guidance and script for running Roslyn analyzers and capturing logs.
- PR-CHECKLIST.md — Short checklist to follow before opening a pull request.

Recommended flow:

1. Read `RUN-LOCAL.md` for local run & test steps.
2. Run `pwsh ./scripts/dev.ps1` helper for common flows.
3. Run analyzers with `pwsh ./scripts/run-analyzers.ps1 -Execute` (or via CI as described in `RUN-CI.md`).
4. Follow `PR-CHECKLIST.md` before opening a PR.
