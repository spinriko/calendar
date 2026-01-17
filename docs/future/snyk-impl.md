# Snyk CLI Implementation Plan

## Purpose
This document provides a clear, actionable plan for integrating **Snyk CLI** into the build pipeline to deliver immediate vulnerability visibility and enforceable security gates with minimal operational overhead.

---

# 1. Objectives

- Introduce automated vulnerability scanning into every build.
- Provide SecOps with consistent, auditable security reporting.
- Enforce fail conditions based on vulnerability severity.
- Implement a low-friction, low-cost control that requires no new infrastructure.

---

# 2. Prerequisites

## 2.1 Snyk Account
- Create a Snyk account (free tier is sufficient for vulnerability scanning).
- Retrieve the **Snyk API token** from:  
  *Account Settings → API Token*

## 2.2 Build Agent Preparation
- Download the Snyk CLI binary for your OS.
- Place it in a stable directory on each build agent, e.g.:  
  `C:\Tools\Snyk\` or `/opt/snyk/`
- Add the directory to the system PATH.

## 2.3 Secure Pipeline Variable
- Store the Snyk API token as a **secret variable** in the pipeline environment.  
  Example variable name: `SNYK_TOKEN`

---

# 3. Pipeline Integration

## 3.1 Authentication Step
Add a step early in the pipeline to authenticate the CLI:

snyk auth $env:SNYK_TOKEN

## 3.2 Vulnerability Scan Step
Run a dependency vulnerability scan:

snyk test --severity-threshold=high


This enforces a fail condition if any vulnerability at or above the threshold is detected.

## 3.3 Optional: Monitor for Reporting
If long-term reporting is desired:

snyk monitor


This uploads the dependency snapshot to Snyk for dashboard visibility.

---

# 4. Build Fail Conditions

## 4.1 Recommended Threshold
- `--severity-threshold=high`  
  Blocks builds only on high/critical issues.

## 4.2 Alternative Thresholds
- `--severity-threshold=medium`
- `--severity-threshold=low`

Choose based on SecOps policy and tolerance.

---

# 5. Operational Model

## 5.1 No Infrastructure Required
- Snyk CLI runs entirely on existing build agents.
- No servers, databases, or services to maintain.

## 5.2 Minimal Maintenance
- Update the CLI binary periodically (quarterly recommended).
- Rotate the Snyk API token per standard credential hygiene.

## 5.3 Reporting
- Build logs contain scan results.
- Optional Snyk dashboard provides historical visibility.

---

# 6. Security Considerations

- API token is scoped only to Snyk; it does not grant access to internal systems.
- CLI performs outbound HTTPS calls only.
- No persistent services or background processes are installed.
- All scanning occurs within the pipeline execution context.

---

# 7. Rollout Plan

## Phase 1 — Lab Validation
- Install CLI on lab agents.
- Run scans on representative projects.
- Validate thresholds and build behavior.

## Phase 2 — Controlled Adoption
- Enable Snyk scanning on selected pipelines.
- Review results with SecOps.

## Phase 3 — Broad Rollout
- Add Snyk scanning to all pipelines.
- Standardize thresholds and reporting expectations.

---

# 8. Summary

Implementing Snyk CLI in the build pipeline provides:
- Immediate vulnerability detection
- Enforceable security gates
- Zero infrastructure overhead
- A defensible, low-cost security control

This plan enables rapid adoption while maintaining operational simplicity and alignment with SecOps expectations.

