# Security/Architecture Audit — Follow-up Tracker

This tracks the outcome of the full-codebase audit (86 findings across backend security, backend
architecture, frontend security, frontend UI/UX, and database/migrations) and what's left after
implementing the top-14 "fix first" list.

Full original writeup with exploit scenarios and per-finding fixes: the published audit artifact
(`KayCare HMS — Security, Architecture & UI Audit`, ask Claude to pull it up via
`Artifact action: list` if the link is lost). This file tracks status only — treat the artifact as
the source of truth for *why* each remaining item matters.

Last updated: 2026-08-17.

---

## Part 1 — Before deploying what was just implemented

These are not optional. The 14-item pass made backend changes that were reviewed carefully but
**never compiled** — this environment had the .NET runtime but no SDK, so `dotnet build` was never
run. **Update 2026-08-17: the SDK gap is fixed** (.NET 8 SDK installed; this machine is Windows
ARM64, so a side-by-side x64 SDK was also installed at `C:\Program Files\dotnet\x64\dotnet.exe`
since QuestPDF has no `win-arm64` native — use that path, not plain `dotnet`, for build/test/ef on
this machine). Do these in order before pushing to Production:

- [x] `dotnet build` — **done 2026-08-17.** First real build caught a genuine compile bug: 6 of the
      ~30 DB4/DB5 config files chained two `.HasCheckConstraint()` calls in one `ToTable(t => ...)`
      lambda, which doesn't compile (`TableBuilder.HasCheckConstraint` returns a
      `CheckConstraintBuilder`, not a `TableBuilder`). Fixed by splitting into separate statements.
      Build is now clean (0 errors, 2 pre-existing unrelated warnings). Confirms the JWT-cookie
      APIs (`IAntiforgery`, `CookieBuilder.SecurePolicy`, `ForwardedHeadersOptions`) compile fine.
- [x] `dotnet test` — **done 2026-08-17, 32/32 pass** (confirmed stable across two consecutive
      runs). Needed three more fixes along the way, all in test infrastructure, not production
      code: (1) local PostgreSQL 16 installed (`postgres`/`postgres`, matches the factory's
      existing default connection string); (2) `MediCloudWebAppFactory` never configured
      `Hl7:WebhookApiKey`/`MllpSharedSecret`, so the app refused to start under the H3/H4 fail-fast
      check regardless of which test ran — added test dummy values; (3) the test client used
      `http://localhost`, but `AntiforgeryOptions.Cookie.SecurePolicy=Always` (active outside
      Development) makes `IAntiforgery.GetAndStoreTokens()` throw on non-HTTPS requests instead of
      degrading — switched to `BaseAddress=https://localhost`, matching production (always HTTPS
      via Render). Also fixed one flaky/stale test:
      `TenantA_Token_IsRejectedByTenantB_Endpoints` assumed the `X-Tenant-Code` header could shift
      an authenticated request's tenant scope, which the C1 fix deliberately made impossible
      (`TenantResolutionMiddleware` ignores the header once authenticated, using the JWT's
      `tenantId` claim only) — rewrote as `TenantA_Token_IgnoresSpoofedTenantBHeader` to assert the
      actual invariant. **This means the JWT-cookie migration (login, CSRF, cookie-based auth) and
      tenant isolation are now genuinely verified**, not just hand-reviewed.
- [x] `dotnet ef migrations has-pending-model-changes` — **done 2026-08-17.** The manager-roles
      migration already existed as a real file (`20260804215200_AddDepartmentManagerRoles.cs`), so
      that item was already resolved. Found other pending changes (see below), now clean.
- [x] Generate the pending EF migration(s) — **done 2026-08-17** as a single migration
      `AddCheckConstraintsAndFixCascades` (EF diffs the whole model at once, so `FixUnsafeCascades`
      and `AddCheckConstraints` couldn't be generated as two separate migrations after the fact).
      Contains all 38 `AddCheckConstraint` calls (count verified against the 38
      `HasCheckConstraint` occurrences in source), the 2 `Cascade`→`Restrict` FK changes
      (DB2/DB3), and 4 unrelated pre-existing `AlterColumn` defaults on `Tenants` AI-tier columns
      that `has-pending-model-changes` also caught. Verified against real data: applied cleanly to
      the local `KayCareTestDb` (already populated with patients/bills/prescriptions from prior
      test runs) with zero constraint violations, 32/32 tests still pass afterward.
- [ ] `dotnet ef database update` against a real/staging Postgres — the local `KayCareTestDb`
      confirms the migration *can* apply cleanly against realistic data, but staging/production has
      its own data and hasn't been checked yet.
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
- [x] `npm run build` — re-verify after the migration work above didn't touch frontend; last run
      2026-08-15, after the UI §9 pass below —
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

**Update 2026-08-17**: everything below not requiring a live hosted target ("Tier 1" — mechanical,
bounded, no open design question) was completed and verified this session (`dotnet build`/
`dotnet test` 34/34/`npm run build` all clean throughout). See commits `0fb0b12`, `bcd4603`,
`ea0aafb`, `c7d1d0e`, `3a89b48`. Items still unchecked below ("Tier 2") are large and/or need a
design decision this session deliberately didn't make unilaterally — see "Suggested next pass"
below for how to pick these up.

### Backend security
- [x] **M1** — Rate limiting added: .NET 8 built-in `Microsoft.AspNetCore.RateLimiting`, global
      100/min per-IP plus a stricter 10/min policy on `/api/auth/login`.
- [x] **M2** — New `StrongPasswordAttribute` (3-of-4 character classes) applied alongside the
      existing 8-char minimum on all three password-setting DTOs.
- [x] **M4** — Postgres `SSL Mode=Require` + real cert validation outside Development (was
      `Prefer`/`Trust Server Certificate=true` unconditionally).
- [x] **M6** — `app.UseHsts()` outside Development.
- [x] **M9** — New `DocumentConstants` (25MB cap, PDF/JPEG/PNG/TIFF/DOC/DOCX allow-list) enforced
      in `DocumentsController.Upload`.
- [x] **L2** — `[Required]`/`[Range]` added to `CreateBillRequest`, `AddPaymentRequest`,
      `BillItemRequest` (previously zero data-annotations on any of these).
- [ ] **M3** — No token revocation/logout — Tier 2, design decision (blocklist store).
- [ ] **M7** — Patient PHI sent to third-party AI models with no de-identification — Tier 2,
      compliance-shaped design work.
- [x] **L3** — Same fix as DB16 below (Postgres trigger, DB-enforced append-only).
- [x] **L4** — `RequireRealSecret` extended to cover `Jwt:Key` (previously only the two HL7
      secrets), plus a new 32-byte minimum length check. Also generalized the placeholder-string
      check itself (was an exact match against one literal that would never have caught `Jwt:Key`'s
      actual placeholder text).
- [ ] **L5** — AI provider key fallback chain "confusing" — Tier 2, vague/subjective, needs a
      closer read before scoping, low priority.
- [x] **L6** — `AuthService.LoginAsync` now runs a dummy BCrypt verify on the not-found/inactive
      path so it costs the same ~250-300ms as a real wrong-password attempt.

### Backend architecture
- [ ] **F9.1 (partial)** — Still only the 8 billing/dispensing/admission services from Part 2;
      the other ~39 (appointments, consultations, lab, radiology, referrals, users, tenants,
      reports, etc.) are still silent — Tier 2, needs triage of which actually need audit trail vs.
      just logging before treating as one uniform extension.
- [ ] **F1.3 / F6.1** — Stock deduction silent no-op — Tier 2, real fix is a schema change
      (`PrescriptionItem`→`DrugInventory` FK) that also touches prescription creation UI/API.
- [x] **F2.1** — `LabResultService` batches the lab-catalog lookup once per HL7 message instead of
      once per observation.
- [ ] **F9.3 (partial)** — Same status as F9.1 above — Tier 2.
- [x] **F11.1** — `DbInitializer.MigrateAsync` wrapped in a new session-level Postgres advisory
      lock (`pg_advisory_lock`, not the existing transaction-scoped helper, since `MigrateAsync`
      manages its own internal per-migration transactions).
- [x] **F2.2** — `InpatientBillingService` loads the `CreatedBy` user once per batch (every charge
      shares the same user) instead of once per charge.
- [x] **F2.3** — `RadiologyOrderService` computes the starting accession sequence once per order
      and increments in-memory, instead of re-querying on every loop iteration.
- [ ] **F2.4** — Pagination in only 1 of ~46 service interfaces — Tier 2, needs a shared
      `PagedResult<T>` convention decision.
- [x] **F4.1** — `TenantsController`'s custom try/catch removed; bubbles to the global handler
      like every other controller now.
- [x] **F5.1** — New `ClaimsPrincipalExtensions.IsAdminOrSuperAdmin()` replaces the duplicated
      check in all 7 controllers; `UsersController`'s hardcoded role-name strings fixed too.
- [x] **F5.2** — New `SequenceNumberExtensions` (`GetNextSequenceAsync`/`GenerateSequenceNumberAsync`)
      replaces all 11 near-identical private methods. Also fixed `ReferralService`, which used a
      materially weaker `CountAsync`-based approach with no lookback/locking — now wrapped in the
      same transaction+advisory-lock pattern as the other 11.
- [x] **F5.3** — `PayerTariffService`'s duplicated field-assignment block extracted into one
      `ApplyFields` helper.
- [x] **F6.4** — Documented (confirmed still load-bearing — every `*Configuration.cs` file still
      writes SQL Server-syntax default-value SQL, not dead code).
- [ ] **F7.1** — Mixed authorization style — Tier 2, design decision, touches 31 controllers (found
      3 distinct patterns in play, not just 2).
- [ ] **F10.1** — `AiQuotaResetDate` bad default is a symptom; the actual monthly-quota-reset logic
      doesn't exist anywhere in the app yet (also DB21) — Tier 2, really a small feature not a bug
      fix, fixing just the default alone is low-value.
- [ ] **F3.1 (partial)** — General absence of `RowVersion`/optimistic-concurrency tokens — Tier 2,
      large, Bill/Bed's practical races are already closed via advisory locks.

### Frontend security
- [x] **10** — Real client-side MIME/size checks (not just the cosmetic `accept=` attribute) added
      to `DocumentsPage.tsx` and `FacilitySettingsPage.tsx`.
- [x] **11** — Baseline CSP added via `render.yaml`'s `headers:` block (mirrored into
      `vite.config.ts`'s `preview.headers` so it's testable locally without live hosting). Verified
      with a real headless-Chromium check — zero CSP violations, login page renders fully styled.
- [x] **4b** — `.env.production` untracked from git (`git rm --cached`; only ever held a
      placeholder, never a real secret) and `.gitignore` updated.
- [x] **7b** — `ErrorBoundary.tsx`'s raw error message/stack now gated on `import.meta.env.DEV`.

### Frontend UI/UX
- [ ] **§1** — No shared UI primitive library — Tier 2, large, explicitly design-decision-heavy.
- [ ] **§2** — No semantic color tokens — Tier 2, tied to §1.
- [ ] **§3** — 17+ files (recount — more than the original "12+") silently swallow fetch errors —
      Tier 2, mechanical (same pattern as the completed UI §9) but a large standalone lift, offered
      as an opt-in add-on rather than bundled by default.
- [ ] **§4** — Inconsistent form conventions — Tier 2, tied to §1.
- [x] **§5 (partial)** — `ConfirmDialog.tsx` (used across ~30+ pages) now has `role="dialog"`,
      `aria-modal`, `aria-labelledby`, an Escape-key handler, and a minimal focus trap. The broader
      `aria-label`/`htmlFor` sweep across ~77 individual pages remains — Tier 2, large.
- [ ] **§6** — Responsive gaps — Tier 2, design decision.
- [ ] **§8** — No shared Table component — Tier 2, large, design-heavy like §1.
- [x] **§10** — Dead code (`pages/Placeholder.tsx`) deleted — confirmed never imported.
- [ ] **§11 (partial)** — MAR secondary-review-step — Tier 2, feature design work.

### Database & migrations
- [ ] **DB6** — Tenant-scoped entities need a DB-level `TenantId` safety net — Tier 2, design
      decision (SaveChanges interceptor vs. Postgres RLS).
- [x] **DB7-9** — Composite `(TenantId, FK-column)` indexes added to `BillItem`, `PrescriptionItem`,
      `BillTemplateItem`.
- [x] **DB10** — `Bill.AdmissionId` now has a real FK constraint (`Restrict`), plus the
      `Admission` navigation property it was missing entirely.
- [x] **DB11** — Partial unique index on `(TenantId, NationalId)`, filtered `NOT NULL`. Verified
      against real seeded data — no existing duplicates.
- [x] **DB12** — `PayerTariff`'s cascades from both `Payer` and `ServiceCatalogItem` flipped to
      `Restrict`.
- [x] **DB13** — **Stale finding, corrected**: re-verified against current code and `Ward`/
      `Payer`/`Supplier` already have `IsActive` — no work was needed here. Only `Bed` genuinely
      lacks it, and `Bed` has a `Status` string instead, which is a design question (can a bed be
      "occupied" and "inactive" independently?), not an oversight — left alone.
- [x] **DB14** — `PayerTariff.TariffPrice` narrowed to `decimal(12,2)` to match every other money
      column. Verified no existing value exceeded 12 total digits first.
- [ ] **DB15** — Audit log `Details` has no structured before/after shape — Tier 2, naturally
      bundled with the F9.1/F9.3 extension since it touches the same call sites.
- [x] **DB16** — Postgres trigger (`prevent_auditlog_modification`) now rejects `UPDATE`/`DELETE`
      on `AuditLogs` at the DB level. New `AuditLogTests.cs` verifies both directions.
- [ ] **DB17** — Patient PII stored unencrypted — Tier 2, compliance-shaped, needs a key-management
      design.
- [ ] **DB18** — `Tenant.CustomOpenRouterKey` column still plaintext — Tier 2, bundle with DB17.
- [x] **DB19** — Composite indexes added to `DispenseEventItem`/`LabOrderItem`'s order-lookup path.
- [x] **DB20** — Stale `docs/schema.sql` deleted (confirmed nothing referenced it outside this
      tracker).
- [ ] **DB21** — Same as F10.1 — Tier 2.

---

## Suggested next pass, if/when picked back up

Roughly in order of bang-for-buck:
1. ~~**DB4 + DB5**~~ — done. Needs migration generation, see Part 1.
2. ~~**F9.1 + F9.3** (billing/dispensing/admission)~~ — done for the 8 named services. Remaining:
   extend the same `ILogger<T>` + `IAuditService` pattern to the other ~39 services (appointments,
   consultations, lab/radiology orders, referrals, user management, tenant admin, reports) —
   **needs triage first** (see Part 3 note) since some of those ~39 are read-only report/PDF
   generators or infra plumbing where "audit trail" doesn't obviously apply the same way; not a
   uniform mechanical extension the way it looked before actually reading all 39.
3. ~~**UI §9**~~ — done, all 30 files / 79 call sites converted. **UI §1** (shared component library:
   no shared Input/Card/Button primitives, styles copy-pasted 100+ times across 44-66 files) is
   still open — genuinely large, design-decision-heavy work, not a mechanical extension.
4. ~~**1 (JWT in localStorage)**~~ — done via a full plan-mode design pass (JWT moved to an httpOnly
   cookie, CSRF protection added from scratch, CORS tightened as a side effect). **Verified**:
   `dotnet build`/`dotnet test` (34/34) pass as of 2026-08-17 — see Part 1. Still outstanding before
   Production: the Render config checks (`Cors:AllowedOrigins`, `ASPNETCORE_ENVIRONMENT=Production`)
   and the manual browser/Safari pass, neither of which can be done from this dev environment.
5. ~~**Part 3 Tier 1**~~ — done, 2026-08-17. All ~31 mechanical/bounded Part 3 findings (rate
   limiting, password policy, CSP, sequence-number dedup, missing indexes/FKs, audit-log
   append-only enforcement, etc.) — see the checkboxes above for the full per-item list. One
   correction found along the way: DB13 was stale, no work needed. Everything verified via
   `dotnet build`/`dotnet test`/`npm run build`, plus a real headless-browser check for the CSP
   change specifically. All pushed to `origin/main`.
6. **DB17 + M7** (PII/PHI encryption at rest, AI data handling) — compliance-shaped, worth doing
   before any real patient data goes into a production instance. Tier 2 — needs a key-management
   design decision, not mechanical.
7. **M3** (token revocation/logout) — natural follow-on to the JWT-cookie migration; today logout
   only stops the current browser, a stolen cookie/token is still valid until natural expiry.
   Tier 2 — needs a revocation-store design decision.
8. **UI §1/§2/§4/§8** (shared component library, color tokens, form conventions, shared Table) —
   all explicitly Tier 2, large and design-decision-heavy, best done as one coordinated pass since
   they're interdependent (a Table component needs the same design-token decisions as everything
   else).
9. **F7.1** (standardize authorization style) and **F2.4** (pagination convention) — both Tier 2,
   each needs one upfront design decision before touching the ~30-46 files they'd otherwise touch
   mechanically once decided.
