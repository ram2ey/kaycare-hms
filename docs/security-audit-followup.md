# Security/Architecture Audit — Follow-up Tracker

This tracks the outcome of the full-codebase audit (86 findings across backend security, backend
architecture, frontend security, frontend UI/UX, and database/migrations) and what's left after
implementing the top-14 "fix first" list.

Full original writeup with exploit scenarios and per-finding fixes: the published audit artifact
(`KayCare HMS — Security, Architecture & UI Audit`, ask Claude to pull it up via
`Artifact action: list` if the link is lost). This file tracks status only — treat the artifact as
the source of truth for *why* each remaining item matters.

Last updated: 2026-08-15.

---

## Part 1 — Before deploying what was just implemented

These are not optional. The 14-item pass made backend changes that were reviewed carefully but
**never compiled** — this environment has the .NET runtime but no SDK, so `dotnet build` was never
run. Do these in order before pushing to Production:

- [ ] `dotnet build` — confirm the whole solution compiles. This matters more than usual for the
      JWT-cookie migration below: several ASP.NET Core APIs used there (`IAntiforgery`,
      `CookieBuilder.SecurePolicy`, `ForwardedHeadersOptions`) were hand-verified against
      documentation/existing patterns but never compiled.
- [ ] `dotnet test` — **there is a real integration test suite** at `src/KayCare.Tests/`
      (`Auth/AuthTests.cs`, `Patients/PatientTests.cs`, `Billing/BillingTests.cs`,
      `TenantIsolation/TenantIsolationTests.cs`) that spins up a real Postgres test DB via
      `WebApplicationFactory`. The JWT-cookie migration below broke `MediCloudWebAppFactory.
      CreateAuthenticatedClientAsync` (it read the JWT from the login response body, which no
      longer exists) — already fixed to extract it from the `Set-Cookie` header instead, and
      `AuthTests.Login_WithValidCredentials_Returns200AndToken` was renamed/updated to assert the
      cookie is set and the body has no `token` key. Both fixes are unverified — needs `dotnet
      test` (or at minimum `dotnet build` on the `KayCare.Tests` project) to confirm they compile
      and the whole suite still passes, since none of it could be run in this environment.
- [ ] `dotnet ef migrations has-pending-model-changes --project src/KayCare.Infrastructure --startup-project src/KayCare.API`
      (or a trial `dotnet ef migrations add TestSync` that should come out empty) — confirms the
      hand-patched `AppDbContextModelSnapshot.cs` (added the 5 manager roles) actually matches the
      model now. If it's not clean, fix the snapshot before doing anything else below.
- [ ] `dotnet ef migrations add FixUnsafeCascades --project src/KayCare.Infrastructure --startup-project src/KayCare.API`
      — generates the actual migration for the two `Cascade` → `Restrict` config changes
      (`LabOrderItemConfiguration.cs`, `InpatientChargeConfiguration.cs`). This migration was never
      generated; only the config source was edited.
- [ ] `dotnet ef migrations add AddCheckConstraints --project src/KayCare.Infrastructure --startup-project src/KayCare.API`
      — generates the migration for the DB4 (non-negative monetary/quantity) and DB5 (status/state
      allow-list) `HasCheckConstraint` calls added across ~30 `*Configuration.cs` files. Also never
      generated; only the config source was edited (same situation as `FixUnsafeCascades` above — no
      .NET SDK in this environment to run `dotnet ef`). Before applying to any environment with real
      rows, run a check first (e.g. `SELECT * FROM "Bills" WHERE NOT ("TotalAmount" >= 0 AND ...)`
      per constraint) — if the demo/seed data or any existing row violates a new constraint, the
      migration will fail to apply until that row is fixed.
- [ ] `dotnet ef database update` against a real/staging Postgres — confirms all three pending
      migrations (`FixUnsafeCascades`, `AddCheckConstraints`, and the manager-roles snapshot fix)
      apply cleanly.
- [ ] In the Render dashboard, set real values for `Hl7__WebhookApiKey` and `Hl7__MllpSharedSecret`
      (env var entries were added to `render.yaml` with `sync: false`, so Render will prompt for
      them). The app now **refuses to start** if these are missing or left as the placeholder.
- [ ] If anything feeds HL7 messages into the MLLP listener on port 2575 (lab analyzer, Mirth
      Connect, etc.), reconfigure it to send the `Hl7__MllpSharedSecret` value as the first bytes
      on every connection — the listener now rejects unauthenticated connections. This is a
      breaking change to that integration.
- [ ] Confirm `Seeding__EnableDemoData` is `false` (or unset) in Production's Render env vars — the
      demo tenant/accounts will not be seeded/reset without it now, which is correct, but double
      check nothing depended on the old always-on behavior.
- [x] `npm run build` in `frontend/` was run and passed (2026-08-15, after the UI §9 pass below) —
      `tsc -b && vite build` completes with zero TS errors. One real issue was caught this way that
      individual files' `tsc --noEmit` checks missed (an unused parameter left over from an
      `alert()`→banner conversion in `PayersPage.tsx`) — fixed. Re-run this after any further
      frontend changes; a clean per-file `tsc --noEmit` is not sufficient on its own, `npm run build`
      (the project-references build) is the real gate.
- [ ] **New, for the JWT-cookie migration:** confirm `Cors:AllowedOrigins` in the Render dashboard
      for `kaycare-hms-api` contains the *real* deployed frontend URL. The `.onrender.com` wildcard
      that used to paper over a missing/wrong value is gone (finding L1) — CORS will silently
      reject the real frontend if this isn't set correctly, and there's no more fallback.
- [ ] **New, for the JWT-cookie migration:** confirm `ASPNETCORE_ENVIRONMENT=Production` is
      actually set on the live `kaycare-hms-api` Render service. That variable used to only gate
      Swagger UI visibility (cosmetic); it's now security-load-bearing — if unset, the auth/CSRF
      cookies silently get `Secure=false; SameSite=Lax`, which cross-site browsers simply refuse
      to send back to the API, breaking auth outright (fails closed, but worth verifying rather
      than discovering it live).
- [ ] **New, for the JWT-cookie migration:** manual end-to-end auth test in a real browser once
      deployed — login (check dev tools: no `token` in the response body, `Set-Cookie:
      auth_token=...; HttpOnly` present), refresh the page and confirm still logged in (via
      `/api/auth/me`, not a flash-redirect to `/login`), change password and confirm the app stops
      prompting without a hard reload, logout clears the cookie, a raw request without the
      `X-XSRF-TOKEN` header against a mutating endpoint gets 403. Do an explicit pass in **Safari**
      in addition to Chrome/Firefox — flagged as the highest cross-site-cookie risk browser given
      the `SameSite=None` requirement (frontend and API are cross-*site*, not just cross-origin).
- [ ] **Expected one-time disruption:** every currently-logged-in user's `localStorage`-held JWT
      becomes inert the moment the new frontend ships (the old flow was fully removed, not kept as
      a fallback) — they'll simply need to log in again.

---

## Part 2 — Resolved by the 14-item pass

| # | Finding | Status |
|---|---|---|
| C1 | Tenant isolation bypass via `X-Tenant-Code` header | Fixed |
| C2 | Demo backdoor self-heals in Production | Fixed |
| 2a | Stored XSS via `document.write` on lab labels | Fixed |
| DB1 | Migration snapshot drift (manager roles) | Fixed — **needs verification, see Part 1** |
| F1.1 | Tariff pricing never applied to bills | Fixed |
| F1.2 / F3.3 | Bill mutations race with no transaction/lock | Fixed |
| F3.2 | Bed double-booking race | Fixed |
| F1.4 | `TransferAsync` had no transaction | Fixed |
| F4.2 / F9.2 | Global exception handler never logged | Fixed |
| M5 | Swagger public in prod + used as health check | Fixed |
| H5 | Exception messages leaked to client | Fixed |
| H3 | Unauthenticated HL7 MLLP listener | Fixed — **breaking change, see Part 1** |
| H4 | Single global HL7 webhook key / placeholder fallback | Fixed |
| H1 | Predictable per-year temp password | Fixed |
| H2 | `mustChangePassword` not enforced | Fixed |
| UI §7 | Broken nav for 5 manager roles | Fixed |
| DB2 | `CriticalCallLog` cascade delete | Fixed — **needs migration, see Part 1** |
| DB3 | `InpatientCharge` cascade delete | Fixed — **needs migration, see Part 1** |
| 3b / F12A | Client-trusted bill prices | Fixed (server re-prices from catalog/tariff) |
| 4a | AI key round-tripped in cleartext to browser | Fixed (masked; DB column itself still plaintext — see DB18) |
| M8 | Cross-tenant AI key/quota abuse | Resolved as a side effect of the C1 fix |
| UI §11 (billing) | `confirm()`/`alert()` on Bill/CreditNote/Refund actions | Fixed |
| UI §3 (CriticalAlertsWidget) | Silent fetch-failure on critical lab alerts | Fixed |
| DB4 | Zero CHECK constraints on monetary/quantity columns | Fixed — **needs migration, see Part 1** |
| DB5 | Unconstrained status/state string columns | Fixed — **needs migration, see Part 1** |
| F9.1 (partial) | No structured logging in billing/dispensing/admission services | Fixed for 8 services — see scope note |
| F9.3 (partial) | Audit trail not called from billing/dispensing/admission | Fixed for 8 services — see scope note |
| UI §9 | `alert()`/`confirm()` remained in ~30 files | Fixed — all 30 files, 79 call sites converted |
| Frontend security #1 | JWT stored in `localStorage` | Fixed — see scope note; **unverified, see Part 1** |
| L1 | Overly permissive CORS (`*.onrender.com` wildcard) | Fixed as part of the above — wildcard dropped |

DB4/DB5 scope note: constraints were added for the 18 entities with clear-cut non-negative
monetary/quantity columns and the 17 `Status`-named columns (verified each `IN (...)` list against
the actual `KayCare.Core.Constants.*Status` classes, and spot-checked service-layer code to confirm
every constraint matches an existing app-level guard — this is defense-in-depth, not a behavior
change). Deliberately **not** touched, to keep this pass reviewable and low-risk:
- `BillAdjustment.Amount` / `Bill.AdjustmentTotal` / `Bill.BalanceDue` — can legitimately go
  negative (credits, overpayment), so no non-negative check applies.
- Vitals range columns (`VitalSigns`/`Consultation` temperature/weight/height/O2 sat) — flagged in
  the original DB4 finding language as "monetary/quantity" but these are really clinical ranges;
  picking wrong bounds risks rejecting real patient data, so left for a dedicated pass.
- ~13 adjacent "kind"-style string columns with their own constants classes but not named
  `Status`/`State` (`Payer.Type`, `Payment.PaymentMethod`, `Refund.RefundMethod`,
  `StockMovement.MovementType`, `Ward.WardType`, `Referral.ReferralType`, `Admission.DischargeType`,
  `Admission.DischargeCondition`, `Appointment.AppointmentType`, `PatientAllergy.AllergyType`,
  `NursingNote.NoteType`, `BillItem.SourceType`, `Tenant.Type`) — same architecture problem as DB5,
  natural mechanical follow-on if picked back up.

F9.1/F9.3 scope note: instrumented the 8 services the findings call out by name — `BillingService`,
`CreditNoteService`, `RefundService`, `InsuranceClaimService`, `ChargeCaptureService`,
`StockMovementService`, `PrescriptionService`, `AdmissionService` — covering every state-mutating
method (create/issue/pay/discount/adjust/write-off/cancel/void for bills; create/approve/apply/void
for credit notes; create/process/cancel for refunds; create/submit/approve/reject/cancel for claims;
charge capture on consultation/lab/dispense; stock record + dispense-deduction; prescription
create/dispense/partial-dispense/cancel; admit/discharge/transfer/update-discharge-summary). Each
mutating method now: (1) calls `_audit.LogAsync(...)` with a new `AuditActions` constant, the
mutated entity, and a `patientId` for patient-centric queries, using the exact "audit before commit,
inside the same DB transaction" convention already established by `PatientService`; and (2) logs via
newly-injected `ILogger<T>`, structured with named `{PascalCase}` placeholders matching the existing
`MllpListenerService`/`TenantResolutionMiddleware`/global-exception-handler style — `LogInformation`
for normal progression, `LogWarning` for cancellations/voids/rejections/write-offs. Both
`IAuditService` and `ILogger<T>` resolve via existing DI registrations with no wiring changes needed.
Confirmed via `DependencyInjection.cs` that no service outside billing/dispensing/admission was
touched — the other ~40 services (appointments, consultations, lab/radiology orders, referrals, user
management, tenant admin, reports, etc.) remain exactly as before, tracked as F9.1/F9.3 "(partial)"
in Part 3.

UI §9 scope note: all 79 `alert()`/`confirm()` call sites across the 30 files the finding named were
converted to the `ConfirmDialog` component + inline banner-state pattern already established by
`BillDetailPage.tsx`/`CreditNoteDetailPage.tsx`/`RefundDetailPage.tsx`. Work was split across 4
parallel passes by file group; each pass verified its own files with `tsc --noEmit` and a per-file
grep for leftover `alert(`/`confirm(`. A follow-up repo-wide grep and a full `npm run build` (the
real gate — see Part 1) caught one thing the per-file checks missed (an unused parameter in
`PayersPage.tsx`), which was fixed; both now confirm zero native dialogs remain anywhere in
`frontend/src`. One genuine bug was caught and fixed during conversion: `TenantsPage.tsx`'s
double-`confirm()` delete flow (delete → stronger re-confirmation) was converted to two chained
`confirmAction` states, but the naive `onConfirm={() => { confirmAction?.run(); setConfirmAction(null); }}`
pattern used everywhere else silently swallowed the second dialog — React batches `run()`'s
`setConfirmAction(secondStep)` with the trailing `setConfirmAction(null)`, and `null` wins. Fixed
with a functional updater that only clears state if `run()` didn't already replace it. UI §1 (shared
component library) was **not** touched — it's a much larger, design-decision-heavy undertaking, not
a mechanical extension like §9 was.

**JWT-cookie migration scope note** (frontend security #1, plus L1 as a side effect): the JWT moved
from `localStorage` to an httpOnly cookie, fully closing the XSS-token-theft path. This was
previously descoped from every pass this session specifically because it touches the whole login
flow at once — went through a full design/plan-mode pass (two rounds of research: current-code
audit, then a second pass that stress-tested the design against real ASP.NET Core API behavior and
caught a real bug in the first draft — `document.cookie` can't read a cross-*site* API-set cookie,
so the CSRF token is delivered via the JSON response body instead of a JS-readable cookie).

What changed: login sets an `HttpOnly; SameSite=None; Secure` cookie (`auth_token`) instead of
returning the token in the response body (`LoginResponse.Token` is now `[JsonIgnore]`); new
`GET /api/auth/me` (SPA boot "am I logged in" check, since the cookie can't be read by JS) and
`POST /api/auth/logout` endpoints; `ChangePasswordAsync` now reissues the cookie server-side
instead of the frontend hand-patching `localStorage` and hard-reloading; CSRF protection was added
from scratch (there was none before) via ASP.NET Core's built-in `IAntiforgery`, double-submit
pattern, gated on "cookie-authenticated AND no explicit Bearer header" so Swagger/tooling/an
in-flight old frontend build during a rolling deploy stay working; CORS gained `AllowCredentials()`
and lost the `.onrender.com` wildcard (L1) since that combination would otherwise let any other
onrender.com-hosted site send cookie-authenticated requests to this API.

**Discovered along the way and fixed**: a real integration test suite exists at
`src/KayCare.Tests/` that nothing in this session had touched before — `MediCloudWebAppFactory.
CreateAuthenticatedClientAsync` (used by virtually every integration test) read the JWT from the
login response body, which no longer exists; updated it to extract the token from the `Set-Cookie`
header instead and continue authenticating via an explicit `Authorization: Bearer` header (the
same CSRF-exempt path Swagger/tooling use). `AuthTests.cs`'s happy-path test was similarly updated,
and now also asserts the response body contains no `token` key at all — a regression guard for the
actual security boundary of this whole change.

**Unverified — no .NET SDK in this environment**: every backend file was hand-written against the
actual current source (not from memory) and cross-checked twice for ASP.NET Core API correctness,
but none of it has compiled. See the new Part 1 checklist items (`dotnet build`, `dotnet test`,
`Cors:AllowedOrigins`/`ASPNETCORE_ENVIRONMENT` checks, manual browser test including Safari) before
this ships — this is the highest-risk change made in this whole follow-up effort, since a mistake
here means either broken login for every user or a reopened security hole.

**Explicitly out of scope**: **M3** (token revocation/blocklist) — logout only stops the current
browser from sending the cookie; a stolen cookie remains valid until natural JWT expiry, unchanged
from today's stolen-bearer-token risk. **Same-site custom domains** — the whole reason
`SameSite=None` (and thus mandatory CSRF protection) is needed at all is that frontend and API sit
on different `onrender.com` subdomains, which browsers treat as cross-*site*. Moving both under one
registrable domain via Render custom domains would allow `SameSite=Lax`, sidestep Safari
ITP/Firefox-ETP-style cross-site cookie hardening, and simplify the CSRF story — worth a near-term
follow-up, not part of this change.

---

## Part 3 — Still open

Organized by track, most-severe first within each. None of these were touched.

### Backend security
- [ ] **M1** — No rate limiting anywhere (login or otherwise). `Program.cs`.
- [ ] **M2** — Password policy is an 8-char minimum only, no complexity check. `ChangePasswordRequest.cs:10`.
- [ ] **M3** — No token revocation/logout; stateless 8h JWTs can't be invalidated early. `TokenService.cs`.
- [ ] **M4** — Postgres connection allows unencrypted fallback (`SSL Mode=Prefer`) and skips cert validation (`Trust Server Certificate=true`). `DependencyInjection.cs:116`.
- [ ] **M6** — Missing HSTS. `Program.cs`.
- [ ] **M7** — Patient PHI sent to free-tier third-party AI models with no de-identification. `AiController.cs`.
- [ ] **M9** — Document uploads have no file-type allow-list or enforced size cap. `DocumentsController.cs`, `DocumentService.cs`.
- [ ] **L2** — Financial DTOs (`CreateBillRequest`, `AddPaymentRequest`) have no `[Range]` validation as a backstop (service layer already guards, this is defense-in-depth only).
- [ ] **L3** — Audit log has no tamper-evidence beyond normal DB ACLs (same issue as DB16 below). `AuditService.cs`.
- [ ] **L4** — JWT signing key strength never validated at startup. `Program.cs`.
- [ ] **L5** — AI provider key fallback chain (OpenRouter → Gemini) is confusing. `AiController.cs`.
- [ ] **L6** — Minor timing side-channel on login (user-not-found skips BCrypt verify). `AuthService.cs`.

### Backend architecture
- [ ] **F9.1 (partial)** — Structured logging added to the 8 billing/dispensing/admission services this
      pass (see Part 2); the other ~40 services (appointments, consultations, lab, radiology,
      referrals, users, tenants, reports, etc.) are still silent.
- [ ] **F1.3 / F6.1** — Stock deduction can silently no-op on a name mismatch while the dispense/bill still record success; root cause is `PrescriptionItem`→`DrugInventory` linked by string, not FK. `StockMovementService.cs`, `PrescriptionItem.cs`.
- [ ] **F2.1** — Per-observation DB query on every HL7 lab result ingested. `LabResultService.cs:207-211`.
- [ ] **F9.3 (partial)** — Audit trail expanded to billing/dispensing/admission this pass (see Part 2);
      still not called from ~39 other services (appointments, consultations, lab/radiology orders,
      referrals, user management, tenant admin, etc.).
- [ ] **F11.1 (partial)** — `MigrateAsync()` itself still has no advisory lock around it; a race remains if the app is ever scaled to 2+ instances.
- [ ] **F2.2** — N+1 via per-entity `Reference().LoadAsync()`. `InpatientBillingService.cs:188-189`.
- [ ] **F2.3** — Accession numbers generated one-by-one in a loop. `RadiologyOrderService.cs`.
- [ ] **F2.4** — Pagination implemented in only 1 of 32 service interfaces (`IPatientService`).
- [ ] **F4.1** — `TenantsController` has its own try/catch producing a different error JSON shape than the global handler.
- [ ] **F5.1** — Admin-role check duplicated 7×; `UsersController.cs:31` hardcodes role-name strings instead of using the shared `Roles` constants.
- [ ] **F5.2** — Sequence-number generation logic (bill/admission/MRN/accession/refund numbers) duplicated ~10×.
- [ ] **F5.3** — `PayerTariffService.UpdateAsync`/`UpsertAsync` duplicate a field-assignment block.
- [ ] **F6.4** — Undocumented SQL Server → Postgres translation shim in `AppDbContext.OnModelCreating`.
- [ ] **F7.1** — Mixed authorization style (attribute-only vs. attribute + inline `IsInRole` checks); will get harder to keep consistent as more manager roles are added.
- [ ] **F10.1** — `AiQuotaResetDate` defaults to year-1 `DateTime` (also DB21, same issue).
- [ ] **F3.1 (partial)** — No `RowVersion`/optimistic-concurrency tokens exist anywhere in the schema. The practical races on `Bill` and `Bed` are now closed via advisory locks, but the general absence of concurrency tokens elsewhere is still true.

### Frontend security
- [ ] **10** — File-upload client-side checks are cosmetic-only or absent. `DocumentsPage.tsx`, `FacilitySettingsPage.tsx`.
- [ ] **11** — No Content-Security-Policy anywhere in the project.
- [ ] **4b** — `.env.production` is committed to git (currently a placeholder only).
- [ ] **7b** — Full error stack traces render to the screen. `ErrorBoundary.tsx`.

### Frontend UI/UX
- [ ] **§1** — No shared UI primitive library; input/card styles copy-pasted 100+ times across 44-66 files.
- [ ] **§2** — No semantic color tokens; "primary" button color varies across 64 files.
- [ ] **§3** — 12+ other files still silently swallow fetch errors into an empty state (only `CriticalAlertsWidget` was fixed).
- [ ] **§4** — Inconsistent form conventions (required-field marker, validation error display).
- [ ] **§5** — Accessibility gaps: no `aria-label` on icon-only buttons, no modal focus-trap/Escape/`role=dialog`, `htmlFor`/`id` used in only 10/87 pages.
- [ ] **§6** — Responsive gaps: fixed 256px sidebar with no collapse, patient registration form not responsive.
- [ ] **§8** — No shared Table component; pagination in only 3/~20 list pages.
- [ ] **§10** — Dead code: `pages/Placeholder.tsx`, never imported.
- [ ] **§11 (partial)** — Medication administration (MAR) recording still has no secondary review step before submit.

### Database & migrations
- [ ] **DB6** — ~15 tenant-scoped entities require manual `TenantId` assignment in service code, with no DB-level safety net if a developer forgets.
- [ ] **DB7-9** — `BillItem`, `PrescriptionItem`, `BillTemplateItem` have no index beyond the implicit FK.
- [ ] **DB10** — `Bill.AdmissionId` has an index but no FK constraint.
- [ ] **DB11** — `Patient.NationalId` has no per-tenant uniqueness (even filtered).
- [ ] **DB12** — `PayerTariff` cascades from both `Payer` and `ServiceCatalogItem` (should be `Restrict`).
- [ ] **DB13** — Soft-delete pattern (`IsActive`) is inconsistent — present on some entities, missing on others (`Bed`, `Ward`, `Payer`, `Supplier`).
- [ ] **DB14** — `PayerTariff.TariffPrice` uses `HasPrecision(18,2)` vs. `decimal(12,2)` everywhere else.
- [ ] **DB15** — Audit log `Details` has no structured before/after value shape.
- [ ] **DB16** — Audit log "append-only" is convention only, not DB-enforced (same as L3 above).
- [ ] **DB17** — Patient PII (NationalId, phone, address, NHIS number, insurance policy number) stored unencrypted.
- [ ] **DB18** — `Tenant.CustomOpenRouterKey` is masked from the API now, but the database column itself is still plaintext.
- [ ] **DB19** — Minor indexing gaps on `DispenseEventItem` and `LabOrderItem`'s order-lookup path.
- [ ] **DB20** — `docs/schema.sql` is a stale T-SQL artifact describing a schema the app no longer has.
- [ ] **DB21** — Same as F10.1 (`AiQuotaResetDate` default).

---

## Suggested next pass, if/when picked back up

Roughly in order of bang-for-buck:
1. ~~**DB4 + DB5**~~ — done. Needs migration generation, see Part 1.
2. ~~**F9.1 + F9.3** (billing/dispensing/admission)~~ — done for the 8 named services. Remaining:
   extend the same `ILogger<T>` + `IAuditService` pattern to the other ~40 services (appointments,
   consultations, lab/radiology orders, referrals, user management, tenant admin, reports) — purely
   mechanical repetition of what was just done, no new design decisions needed.
3. ~~**UI §9**~~ — done, all 30 files / 79 call sites converted. **UI §1** (shared component library:
   no shared Input/Card/Button primitives, styles copy-pasted 100+ times across 44-66 files) is
   still open — genuinely large, design-decision-heavy work, not a mechanical extension.
4. ~~**1 (JWT in localStorage)**~~ — done via a full plan-mode design pass (JWT moved to an httpOnly
   cookie, CSRF protection added from scratch, CORS tightened as a side effect). **Unverified — see
   Part 1**: this is the highest-risk unverified change in the tracker (no .NET SDK here to compile
   or run the newly-discovered `src/KayCare.Tests/` suite), prioritize `dotnet build` + `dotnet
   test` + the manual browser/Safari pass before anything else in Part 1.
5. **DB17 + M7** (PII/PHI encryption at rest, AI data handling) — compliance-shaped, worth doing before any real patient data goes into a production instance.
6. **M3** (token revocation/logout) — natural follow-on to the JWT-cookie migration; today logout
   only stops the current browser, a stolen cookie/token is still valid until natural expiry.
