# Windows Auth Debugging & Future Identity Enricher

## What changed in this spike
- Added Negotiate (Windows) auth when `Authentication:Mode` is `Windows` or `ActiveDirectory` (see pto.track/AppServiceExtensions.cs and package Microsoft.AspNetCore.Authentication.Negotiate).
- Added `/api/currentuser/debug/claims` to force a Negotiate challenge when unauthenticated and return claims, identity info, and a normalized identity key.
- Normalized identity helper strips `DOMAIN\` and lowercases (for stable lookups).
- Added `IIdentityEnricher` + `NoOpIdentityEnricher` placeholder; wired into the debug endpoint.
- Updated VS Code launch configs: one for normal debug (DLL) and one for watch (hot reload), using the http launch profile.

## Using the debug endpoint
- Run with `Authentication:Mode=Windows` (or `ActiveDirectory`) locally.
- Hit `http://localhost:5139/pto-track/api/currentuser/debug/claims`.
- If unauthenticated, the endpoint sends `WWW-Authenticate: Negotiate`; your browser should then send `Authorization: Negotiate ...`.
- Response includes: `IdentityName`, `NormalizedIdentity`, `AuthenticationType`, `IsAuthenticated`, `Claims`, `ClaimCount`, and `Enriched` (currently empty).
- Only the debug endpoint currently issues the Negotiate challenge. The main `/api/currentuser` action still returns 401 when unauthenticated; when we adopt Windows/AD auth broadly, plan to add `[Authorize(AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme)]` (or equivalent global config) so the challenge happens automatically.

## Planned Identity Enricher
Goal: Populate richer attributes (display name, email, employee ID, roles/groups) without depending on AD claims being present.

Approach options (swap into `IIdentityEnricher`):
1) Internal directory service API: call your internal service with `NormalizedIdentity` and map returned attributes.
2) Synced tables (groups/resources): look up by normalized key; merge roles/resources from your local tables.
3) Hybrid: check local cache/tables first, fall back to internal service, cache results with short TTL.

Implementation notes:
- Keep `IIdentityEnricher` fast and resilient; prefer timeouts and graceful degradation (return partial/empty attributes, never fail the request pipeline).
- Normalize key once (already provided) to avoid case/domain drift.
- Consider memoizing per-request or short-lived caching to reduce chatter to upstream services.
- Add logging for misses or multiples to catch directory drift early.

## InfoSec considerations for Windows auth pass-through

### Architecture (IIS → Kestrel)
- **Deployment model**: IIS acts as a reverse proxy using URL Rewrite rules. Traffic to `/pto-track` is forwarded to Kestrel running on localhost:5139 (HTTP). Kestrel runs independently (e.g., as a Windows Service or standalone process).
- **Protocol**: Negotiate (Kerberos/NTLM) handled by Kestrel via `Microsoft.AspNetCore.Authentication.Negotiate`.
- **TLS**: Enforced at IIS; Kestrel runs HTTP on localhost. No end-to-end encryption between IIS/Kestrel (acceptable on same machine).

### Key security points
- **IIS configuration (URL Rewrite reverse proxy)**: IIS must forward Windows authentication headers without stripping them. Steps:
  1. **Enable Anonymous Authentication** for the IIS site (IIS is just a proxy; Kestrel handles auth).
  2. **Disable Windows Authentication** for the IIS site (recommended to prevent IIS from intercepting auth; not strictly required if Anonymous Auth is enabled and headers flow through).
  3. **Configure URL Rewrite rule** to preserve authentication headers:
     - In IIS Manager → URL Rewrite → select the `/pto-track` rule.
     - Under "Server Variables", ensure `HTTP_AUTHORIZATION` is **not** being cleared or rewritten.
     - If using Application Request Routing (ARR), verify "Preserve host header" is enabled.
     - Common issue: ARR by default does **not** forward `Authorization` headers. Add a server variable to explicitly pass it:
       - Name: `HTTP_AUTHORIZATION`
       - Value: `{HTTP_AUTHORIZATION}` (preserve incoming value)
       - Or use `web.config`:
         ```xml
         <rewrite>
           <rules>
             <rule name="pto-track proxy">
               <match url="^pto-track/(.*)" />
               <action type="Rewrite" url="http://localhost:5139/pto-track/{R:1}" />
               <serverVariables>
                 <set name="HTTP_AUTHORIZATION" value="{HTTP_AUTHORIZATION}" />
               </serverVariables>
             </rule>
           </rules>
         </rewrite>
         ```
  4. **Test**: Hit `https://yourserver/pto-track/api/currentuser/debug/claims` and verify the `WWW-Authenticate: Negotiate` challenge reaches the browser and `Authorization: Negotiate ...` is forwarded to Kestrel.
- **Kerberos vs NTLM**: Negotiate allows both. Kerberos is preferred (mutual auth, ticket-based). NTLM is weaker (challenge-response, no mutual auth) but auto-fallback. InfoSec may want NTLM disabled or audited; configure via group policy or registry if needed.
- **Kerberos-only option**: If InfoSec requires no NTLM fallback, enforce it at the OS/GPO level (e.g., `RestrictSendingNTLMTraffic = 2` / "Deny all" outgoing NTLM). App-level: you can reject requests where `AuthenticationType` is `NTLM`, but the authoritative control is via GPO/registry. Ensure SPNs are set correctly so Kerberos succeeds; otherwise auth will fail when NTLM is blocked.
- **SPNs/Delegation**: For single-hop (browser → IIS → Kestrel on same machine), no delegation needed. If future architecture requires impersonation across network hops, set SPNs on the service account and use constrained delegation. Document delegation stance: currently off.
- **Service account**: Run Kestrel under a low-privilege, managed service account (not domain admin). Ensure credential rotation policy and no interactive logon rights.
- **CORS with credentials**: Allowing credentials is necessary for Windows auth when APIs are called from a different origin. Lock down allowed origins in non-dev (currently localhost for dev only).
- **Logging/PII**: Claims contain identifiers (UPN, SIDs, group SIDs). Debug endpoints are dev-only. Production logs should not dump full claim sets; filter PII or use structured logging with redaction.
- **Replay/windowing**: Kerberos mitigates replay via tickets; NTLM is vulnerable to relay attacks. Minimize NTLM usage and ensure TLS at IIS to protect credentials in transit.
- **Audit/monitoring**: Log failed auth attempts, NTLM fallback events, and unusual source IPs. Consider SIEM hooks for anomaly detection.
- **Browser/zone (dev)**: Adding localhost to Local Intranet and enabling automatic logon is expected for dev. Not required in production (users hit the public IIS endpoint with corp intranet zone policies).

## Next steps (when ready to implement)
- Replace `NoOpIdentityEnricher` registration with a real implementation that calls your internal directory service or synced tables.
- Add unit/integration tests for enricher mapping and failure/miss behavior.
- Optionally expose enriched attributes on existing APIs once the pipeline is reliable.
- Consider short TTL caching to reduce upstream load and latency.
