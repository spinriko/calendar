# Identity Sync Plan (AD + ADP)

## Goals
- Use existing corporate services (AD-connected service + ADP data mart) as the sole identity source.
- Avoid direct AD queries from this app; all resolution goes through pre-synced data.
- Guarantee only synced users can sign in; block if not yet synced.
- Keep group/role assignments authoritative from the sync source, not from arbitrary claims.

## Data Sources
- **Identity service**: exposes AD identities (immutable ID/objectId, email, displayName, roles/groups).
- **ADP data mart**: exposes employeeNumber and org hierarchy; combined upstream into the identity service output or a local staging table.

## Runtime Flow (request time)
1) User authenticates (Windows/AD now; later AAD/JWT in containers).
2) Claims extracted: immutable ID (objectGuid/oid), email, employeeNumber (if present).
3) `UserSync` resolves the user **only** against the pre-synced identity store (DB/cache):
   - Match by immutable ID first, then email, then employeeNumber (if stable).
   - If not found → reject/unauthenticate with message: "Your account has not been synced yet."
4) If found, update freshness fields (LastSeen/LastSync) and apply roles/groups from the synced record (not from claims).
5) Proceed with app authorization using the stored roles/groups.

## Sync Pipeline (offline)
- Scheduled job (or event-driven) pulls from identity service + ADP mart.
- Normalize and materialize into app DB tables: `Users`, `Groups`, `UserGroups`, `Roles`.
- Maintain immutable keys; allow email/manager/department changes.
- Emit audit trail (sync timestamp, counts, errors, skipped users).

## `UserSyncService` changes
- Require at least one stable identifier (prefer immutable AD ID; fallback email/employeeNumber if policy allows).
- Resolve user from local identity tables (synced data), not by creating on-the-fly from claims.
- On miss → return unauthorized with a clear "not yet synced" message.
- On hit → update `LastSeenUtc` and ensure roles/groups reflect synced data.
- Map app roles from the synced role/group tables; do not infer from transient role claims.

## Claim & ID mapping (priority)
1) Immutable ID: `objectGUID` / `oid` / `http://schemas.microsoft.com/identity/claims/objectidentifier`
2) Email: `email` / `mail` / `ClaimTypes.Email`
3) Employee number: `employeeNumber` / `employeeID` / `ClaimTypes.NameIdentifier` (only if policy allows)

## Failure behavior
- If no usable identifier in claims → reject with message: "Cannot identify your account (missing ID/email)."
- If identifiers present but user not in synced store → reject with message: "Your account has not been synced yet."
- Log metrics: missing-id count, not-synced count, success count; include identifiers for diagnosis (PII-safe logging per policy).

## Security & Governance
- Trust boundary: synced identity tables are authoritative; claims are only for locating the user record.
- Role/group changes happen in the upstream identity pipeline, not in the web app.
- Consider feature flag to allow temporary fallback to claim-derived roles only in lower environments.

## Container-ready notes
- Move auth to AAD/JWT when containerized; keep the same sync/resolve flow.
- Swap to `pwsh` scripts and Linux agents; parameterize RID (win-x64 now, linux-x64 later).

## Next steps
- Add identity sync job spec (source endpoints, schemas, schedule, error handling).
- Update `UserSyncService` to resolve-only and reject-unsynced behavior.
- Add metrics/logging for missing/unsynced users.
- Add feature flag for claim-derived roles in non-prod if needed.

---

## In-App Worker vs Dedicated Service
- In-app BackgroundService is fine for lower environments to smoke-test the sync logic.
- IIS app pool idle/recycle can pause/kill background work; production should prefer a dedicated worker (Windows Service/Task Scheduler or containerized job) unless reliability requirements are relaxed.
- Keep the sync logic in a shared library so both the web app and worker use the same core code.

## Scheduling & Scale-Out
- Use `PeriodicTimer` or a cron library to schedule runs; guarantee single-run with a DB advisory lock or distributed lock.
- In scale-out scenarios, run exactly once across the fleet via leader election or lock; avoid N workers competing.
- Implement idempotent sync steps and checkpointing to resume after partial failures.

## Config Keys & Defaults
- `IdentitySync:Enabled` (bool): gate execution of the worker.
- `IdentitySync:IntervalMinutes` or `IdentitySync:Cron` for schedule.
- `IdentitySync:WindowStart` / `IdentitySync:WindowEnd` to constrain when runs are allowed.
- `IdentitySync:MaxConcurrency` (default 1) and `IdentitySync:RetryPolicy` (max retries/backoff).
- `IdentitySync:SourceEndpoints` (identity service, ADP mart URIs) and `IdentitySync:Auth` settings.
- `HostRole`: `web` | `sync` to decide which hosts run the worker.

## Safeguards & Observability
- Single-instance lock across nodes; enforce mutual exclusion for each run.
- Emit metrics: last successful sync UTC, duration, processed/updated counts, error counts, and currently-running flag.
- Log PII-safe identifiers when rejecting users (per policy) to aid diagnosis.
- Health checks expose readiness based on last successful sync time and error thresholds.

## IIS Caveats
- App pool idle/recycle may interrupt schedules; background tasks can be missed.
- If the worker must run under IIS, set app pool to "always on" and tune idle/recycle settings; still consider a dedicated worker for robustness.

## Phased Adoption Plan
- Short term: enable in-app worker only in lower environments (`IdentitySync:Enabled=true`) to validate logic and metrics.
- Production: deploy a dedicated worker (Windows Service/Task Scheduler now; CronJob/container later) with the same sync library.
- Web app continues to resolve users from the synced store and rejects "not yet synced" accounts with a friendly message.

---

## Implementation Checklist
- Define data contracts for synced users/groups/roles (immutable IDs first).
- Implement sync library (pull → normalize → upsert) with idempotency.
- Choose scheduler: in-app (lower envs) vs dedicated worker (prod).
- Add single-run lock (DB/distributed) and retry/backoff policy.
- Emit metrics/logs and health checks (last success time, counts).
- Gate web auth: resolve only from synced store; friendly reject if missing.
- Configure environment flags and secrets; document run windows.
