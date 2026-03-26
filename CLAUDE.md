# MediCloud EMR — Project Specification

## What We're Building
Multi-tenant Electronic Medical Record SaaS hosted on Microsoft Azure,
sold to multiple hospitals and clinics in Ghana and beyond.

## Technology Stack
- Backend: .NET 8 (C#) — ASP.NET Core Web API
- Frontend: React + TypeScript + Vite + Tailwind CSS
- Database: Azure SQL with Entity Framework Core (code-first migrations)
- Storage: Azure Blob Storage (lab results, documents, prescriptions)
- Auth: JWT with bcrypt (cost factor 12), account lockout
- Hosting: Azure App Service + Azure Static Web Apps
- Secrets: Azure Key Vault (never hardcode secrets)
- CI/CD: GitHub Actions
- IaC: Azure Bicep

## Solution Structure
MediCloud.sln
├── src/
│   ├── MediCloud.API           ← ASP.NET Core Web API (controllers, middleware, program.cs)
│   ├── MediCloud.Core          ← Entities, interfaces, DTOs, exceptions (no Azure deps)
│   ├── MediCloud.Infrastructure ← EF Core, services, Azure Blob, auth
│   └── MediCloud.Tests         ← xUnit tests
├── frontend/                   ← React + TypeScript + Vite + Tailwind
├── infrastructure/             ← Azure Bicep files
│   └── bicep/
├── docs/
│   ├── schema.sql
│   ├── architecture.md
│   └── lab-integration.md
└── CLAUDE.md

## Multi-Tenancy Rules
- Shared schema with TenantId (UNIQUEIDENTIFIER) on every tenant-scoped table
- EF Core global query filters enforce tenant isolation on every query automatically
- Azure SQL Row-Level Security as second enforcement layer
- Tenant resolved from subdomain on every HTTP request (stmarys.medicloud.com)
- X-Tenant-Code header used for local development and API clients
- TenantId auto-injected in SaveChangesAsync — developers cannot forget it

## Roles
SuperAdmin, Admin, Doctor, Nurse, Receptionist, Pharmacist

## Modules (build in this order)
1. Auth — JWT login, bcrypt, 5-attempt lockout, 8hr token expiry, MustChangePassword flag
2. Patients — registration, MRN (MRN-YYYY-NNNNN), search, allergies
3. Appointments — scheduling, status workflow, doctor calendar view
4. Consultations — SOAP notes, vitals, ICD-10 diagnosis codes, sign-off
5. Prescriptions — medication line items, pharmacist dispensing workflow
6. Billing — invoices (INV-YYYY-NNNNN), bill items, payments, computed balance
7. Documents — Azure Blob upload, SAS token download (15min expiry), per-tenant containers
8. Lab Results — HL7 MLLP listener port 2575, parse ORU^R01, notify ordering doctor
9. Audit Logs — HIPAA, every PHI read/write logged, immutable, never deleted

## Lab Equipment Integration
- Beckman Coulter DxC 500 AU — Chemistry analyzer (ASTM/HL7 over TCP/IP)
- Beckman Coulter DxH 560 AL — Hematology 5-part diff (ASTM TCP/IP, bidirectional)
- Roche cobas e411 — Immunoassay (ASTM over RS-232 → serial-to-Ethernet converter)
- Middleware: Mirth Connect (open source) → HL7 ORU^R01 → MediCloud HL7 listener

## Database Rules
- All PKs: UNIQUEIDENTIFIER with NEWSEQUENTIALID()
- All tenant tables inherit from TenantEntity base class (TenantId, CreatedAt, UpdatedAt)
- AuditLogs: BIGINT IDENTITY PK (high volume), no FK on TenantId (survives tenant deletion)
- Patients MRN: MRN-{YEAR}-{5-digit zero-padded sequential}
- Bills: INV-{YEAR}-{5-digit zero-padded sequential}
- All money fields: DECIMAL(12,2)
- Passwords: bcrypt hash only — never store plaintext

## Security (HIPAA)
- bcrypt cost factor 12
- Account lockout: 5 failed attempts = 30min lock
- JWT: 8hr expiry, ClockSkew = zero
- HTTPS enforced at infrastructure level
- Blob Storage: never public access, SAS tokens only
- Key Vault for all secrets in production
- Audit every Patient.View, Patient.Create, Patient.Update
- Global query filter + RLS = double tenant isolation

## Local Development Setup
- SQL Server: SQL Server 2022 Express (`.\SQLEXPRESS`, Windows auth) — no Docker needed on this machine
- Blob Storage: Azurite emulator (Docker), ports 10000-10002 — only needed for Documents module testing
- Secrets: appsettings.Development.json (never committed to git)
- Migrations: dotnet ef migrations add + dotnet ef database update

## NuGet Packages Required
MediCloud.Infrastructure:
- Microsoft.EntityFrameworkCore.SqlServer 8.0.0
- Microsoft.EntityFrameworkCore.Tools 8.0.0
- Azure.Storage.Blobs 12.19.0
- Azure.Security.KeyVault.Secrets 4.5.0
- BCrypt.Net-Next 4.0.3

MediCloud.API:
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0
- Swashbuckle.AspNetCore 6.5.0

## Current Build Status
Last successful build: `dotnet build` → **0 errors, 0 warnings**.

### Modules completed
| # | Module           | Status    |
|---|------------------|-----------|
| 1 | Auth             | ✅ Done   |
| 2 | Patients         | ✅ Done   |
| 3 | Appointments     | ✅ Done   |
| 4 | Consultations    | ✅ Done   |
| 5 | Prescriptions    | ✅ Done   |
| 6 | Billing          | ✅ Done   |
| 7 | Documents        | ✅ Done   |
| 8 | Lab Results      | ✅ Done   |
| 9 | Audit Logs       | ✅ Done   |
| 10| Lab Order Module | ✅ Done   |

### Next step
**Lab Order Module complete.** Remaining tracks:
1. **UI improvements** — user identified missing/incomplete features during first live test (to be listed next session)
2. **Bicep IaC** — Azure infrastructure as code (can be done independently of UI work)

### Local dev run instructions
To start the app locally:
```bash
# Terminal 1 — Backend (from repo root)
dotnet run --project src/MediCloud.API --urls "http://localhost:5000"

# Terminal 2 — Frontend
cd frontend && npm run dev
```
- Frontend: http://localhost:5173
- Swagger: http://localhost:5000/swagger
- If port 2575 is already in use (MLLP conflict), kill it: `powershell -Command "Stop-Process -Id (netstat -ano | Select-String ':2575').ToString().Trim().Split()[-1] -Force"`

### Dev DB seed (MediCloudDb — already seeded)
Demo tenant + admin user created via `tools/Seeder/` console project.
- **Email:** admin@demo.com
- **Password:** Admin@1234
- **Tenant Code:** demo
- Re-run seed: `cd tools/Seeder && dotnet run`

### Bugs fixed
- `frontend/src/api/auth.ts` — login request now passes `X-Tenant-Code` header explicitly (tenant middleware reads header, not body; header was missing before login so localStorage was empty)

### Integration test suite
Test project: `src/MediCloud.Tests/` — xUnit + `Microsoft.AspNetCore.Mvc.Testing`
Test database: `MediCloudTestDb` on local SQL Server Express (separate from dev DB)
Run with: `dotnet test src/MediCloud.Tests`

**Infrastructure files:**
```
src/MediCloud.Tests/Infrastructure/MediCloudWebAppFactory.cs  ← WebApplicationFactory<Program>; overrides conn string, removes MLLP service, runs migrations, seeds tenants
src/MediCloud.Tests/Infrastructure/TestSeeder.cs              ← seeds TenantA + TenantB (Admin + Doctor each); GUID suffix per run prevents collisions
src/MediCloud.Tests/Infrastructure/TestTenant.cs              ← record: TenantId, TenantCode, AdminEmail, DoctorEmail
```

**Test suites (27 tests):**
```
src/MediCloud.Tests/Auth/AuthTests.cs                         ← login, wrong password, lockout (5 attempts → 423), cross-tenant credentials, unknown tenant
src/MediCloud.Tests/Patients/PatientTests.cs                  ← MRN format, sequential numbering, search, 401 without auth
src/MediCloud.Tests/TenantIsolation/TenantIsolationTests.cs   ← patient/bill 404 cross-tenant, list returns empty cross-tenant, MRN counters independent
src/MediCloud.Tests/Billing/BillingTests.cs                   ← INV format, Draft→Issued→Paid state machine, overpayment 400, invalid transitions 409
```

**Key decisions:**
- `[Collection("Integration")]` on all test classes — prevents parallel execution against shared DB
- Unique GUID suffix per test run → no test-run collisions on MRN/INV sequential counters
- `CreateThrowawayUserAsync()` on factory — lockout tests use a fresh user, not shared admin
- bcrypt work-factor 4 in seeder (vs 12 in prod) — fast seeding without compromising real auth tests
- MLLP `BackgroundService` removed in factory — avoids port 2575 conflict during test runs
- `public partial class Program {}` added to `Program.cs` — required for `WebApplicationFactory<Program>`

### NuGet packages installed (actual versions in .csproj)

**MediCloud.Infrastructure:**
- Microsoft.EntityFrameworkCore.SqlServer 8.0.0
- Microsoft.EntityFrameworkCore.Tools 8.0.0
- Azure.Storage.Blobs 12.19.0
- Azure.Security.KeyVault.Secrets 4.5.0
- BCrypt.Net-Next 4.0.3
- System.IdentityModel.Tokens.Jwt 8.17.0 ← added for TokenService
- FrameworkReference: Microsoft.AspNetCore.App ← added for IHttpContextAccessor / middleware types

**MediCloud.API:**
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0
- Microsoft.AspNetCore.OpenApi 8.0.25 ← pulled in by template
- Swashbuckle.AspNetCore 6.5.0
- Microsoft.EntityFrameworkCore.Design 8.0.0 ← added for EF CLI (dotnet ef) support

### All files created

**MediCloud.API**
```
src/MediCloud.API/Program.cs                              ← JWT auth, Swagger, tenant middleware, global exception handler
src/MediCloud.API/appsettings.json                        ← Jwt:Key placeholder, Issuer, Audience, ExpiryHours:8
src/MediCloud.API/appsettings.Development.json            ← ConnectionStrings:DefaultConnection (never commit)
src/MediCloud.API/Controllers/AuthController.cs           ← POST /api/auth/login
src/MediCloud.API/Controllers/PatientsController.cs       ← CRUD + search + allergies
src/MediCloud.API/Controllers/AppointmentsController.cs   ← schedule, status transitions, calendar
src/MediCloud.API/Controllers/ConsultationsController.cs  ← SOAP, vitals, ICD-10, sign-off
src/MediCloud.API/Controllers/PrescriptionsController.cs  ← create, dispense, cancel, pending queue
src/MediCloud.API/Controllers/BillsController.cs          ← create, issue, payment, cancel, void, outstanding
src/MediCloud.API/Controllers/DocumentsController.cs      ← upload (multipart), list by patient, download SAS URL, delete
src/MediCloud.API/Controllers/LabResultsController.cs     ← list by patient, get by accession, get by ID
src/MediCloud.API/Controllers/LabOrdersController.cs      ← catalog, waiting list, place order, receive, manual result, sign
src/MediCloud.API/Controllers/AuditLogsController.cs      ← query by patient/user/action/date range (SuperAdmin, Admin only)
```

**MediCloud.Core — Entities**
```
src/MediCloud.Core/Entities/TenantEntity.cs       ← abstract base: TenantId, CreatedAt, UpdatedAt
src/MediCloud.Core/Entities/Tenant.cs
src/MediCloud.Core/Entities/Role.cs
src/MediCloud.Core/Entities/User.cs               ← inherits TenantEntity; lockout fields
src/MediCloud.Core/Entities/Patient.cs            ← inherits TenantEntity; MRN, allergy/chronic flags
src/MediCloud.Core/Entities/PatientAllergy.cs     ← standalone (no UpdatedAt); TenantId set manually
src/MediCloud.Core/Entities/Appointment.cs        ← inherits TenantEntity; status workflow
src/MediCloud.Core/Entities/Consultation.cs       ← inherits TenantEntity; SOAP + vitals + ICD-10 JSON
src/MediCloud.Core/Entities/Prescription.cs       ← inherits TenantEntity; dispensing workflow
src/MediCloud.Core/Entities/PrescriptionItem.cs   ← standalone (no timestamps); TenantId set manually
src/MediCloud.Core/Entities/PatientDocument.cs    ← inherits TenantEntity; blobPath, containerName, category
src/MediCloud.Core/Entities/Bill.cs               ← inherits TenantEntity; INV number, computed BalanceDue
src/MediCloud.Core/Entities/BillItem.cs           ← standalone (no timestamps); computed TotalPrice
src/MediCloud.Core/Entities/Payment.cs            ← inherits TenantEntity; method, reference, received by
src/MediCloud.Core/Entities/LabResult.cs          ← inherits TenantEntity; accession, OBR fields, RawHl7; optional LabOrderItemId FK
src/MediCloud.Core/Entities/LabObservation.cs     ← standalone (no timestamps); OBX fields; TenantId set manually
src/MediCloud.Core/Entities/LabTestCatalog.cs     ← global (no TenantId); 19 seeded tests; mapped to instruments
src/MediCloud.Core/Entities/LabOrder.cs           ← inherits TenantEntity; patient+doctor+bill+organisation+status
src/MediCloud.Core/Entities/LabOrderItem.cs       ← standalone (no timestamps); accession, TAT, status, manual result; TenantId set manually
src/MediCloud.Core/Entities/AuditLog.cs           ← BIGINT IDENTITY PK; no FK on TenantId/UserId; immutable
```

**MediCloud.Core — Constants**
```
src/MediCloud.Core/Constants/Roles.cs                ← SuperAdmin Admin Doctor Nurse Receptionist Pharmacist
src/MediCloud.Core/Constants/AppointmentStatus.cs    ← transition table + CanTransition()
src/MediCloud.Core/Constants/AppointmentTypes.cs
src/MediCloud.Core/Constants/ConsultationStatus.cs   ← Draft | Signed
src/MediCloud.Core/Constants/PrescriptionStatus.cs   ← Active | Dispensed | Cancelled
src/MediCloud.Core/Constants/BillStatus.cs           ← Draft | Issued | PartiallyPaid | Paid | Cancelled | Void
src/MediCloud.Core/Constants/LabResultStatus.cs      ← Received | Verified
src/MediCloud.Core/Constants/LabOrderStatus.cs       ← Pending | Active | PartiallyCompleted | Completed | Signed
src/MediCloud.Core/Constants/LabOrderItemStatus.cs   ← Ordered | SampleReceived | Resulted | Signed
src/MediCloud.Core/Constants/AuditActions.cs         ← Patient.View | Patient.Create | Patient.Update
```

**MediCloud.Core — Document Categories** (string constants, no separate file; inline in UploadDocumentRequest)
`LabResult | Prescription | Referral | Consent | Report | Other`

**MediCloud.Core — Interfaces**
```
src/MediCloud.Core/Interfaces/ITenantContext.cs
src/MediCloud.Core/Interfaces/ICurrentUserService.cs
src/MediCloud.Core/Interfaces/ITokenService.cs
src/MediCloud.Core/Interfaces/IAuthService.cs
src/MediCloud.Core/Interfaces/IPatientService.cs
src/MediCloud.Core/Interfaces/IAppointmentService.cs
src/MediCloud.Core/Interfaces/IConsultationService.cs
src/MediCloud.Core/Interfaces/IPrescriptionService.cs
src/MediCloud.Core/Interfaces/IBillingService.cs
src/MediCloud.Core/Interfaces/IBlobStorageService.cs
src/MediCloud.Core/Interfaces/IDocumentService.cs
src/MediCloud.Core/Interfaces/ILabResultService.cs
src/MediCloud.Core/Interfaces/ILabOrderService.cs
src/MediCloud.Core/Interfaces/IAuditService.cs
```

**MediCloud.Core — Exceptions**
```
src/MediCloud.Core/Exceptions/AppException.cs           ← base; carries StatusCode
src/MediCloud.Core/Exceptions/UnauthorizedException.cs  ← 401
src/MediCloud.Core/Exceptions/AccountLockedException.cs ← 423; carries LockedUntil
src/MediCloud.Core/Exceptions/NotFoundException.cs      ← 404
src/MediCloud.Core/Exceptions/TenantNotFoundException.cs← 404
```

**MediCloud.Core — DTOs**
```
src/MediCloud.Core/DTOs/Common/PagedResult.cs
src/MediCloud.Core/DTOs/Auth/LoginRequest.cs
src/MediCloud.Core/DTOs/Auth/LoginResponse.cs
src/MediCloud.Core/DTOs/Patients/CreatePatientRequest.cs
src/MediCloud.Core/DTOs/Patients/UpdatePatientRequest.cs
src/MediCloud.Core/DTOs/Patients/PatientResponse.cs
src/MediCloud.Core/DTOs/Patients/PatientDetailResponse.cs
src/MediCloud.Core/DTOs/Patients/PatientSearchRequest.cs
src/MediCloud.Core/DTOs/Patients/AddAllergyRequest.cs
src/MediCloud.Core/DTOs/Patients/AllergyResponse.cs
src/MediCloud.Core/DTOs/Appointments/CreateAppointmentRequest.cs
src/MediCloud.Core/DTOs/Appointments/UpdateAppointmentRequest.cs
src/MediCloud.Core/DTOs/Appointments/AppointmentResponse.cs
src/MediCloud.Core/DTOs/Appointments/AppointmentDetailResponse.cs
src/MediCloud.Core/DTOs/Appointments/CalendarRequest.cs
src/MediCloud.Core/DTOs/Appointments/CancelAppointmentRequest.cs
src/MediCloud.Core/DTOs/Consultations/CreateConsultationRequest.cs
src/MediCloud.Core/DTOs/Consultations/UpdateConsultationRequest.cs
src/MediCloud.Core/DTOs/Consultations/ConsultationSummaryResponse.cs
src/MediCloud.Core/DTOs/Consultations/ConsultationDetailResponse.cs
src/MediCloud.Core/DTOs/Consultations/DiagnosisDto.cs
src/MediCloud.Core/DTOs/Prescriptions/CreatePrescriptionRequest.cs
src/MediCloud.Core/DTOs/Prescriptions/PrescriptionItemRequest.cs
src/MediCloud.Core/DTOs/Prescriptions/DispensePrescriptionRequest.cs
src/MediCloud.Core/DTOs/Prescriptions/PrescriptionResponse.cs
src/MediCloud.Core/DTOs/Prescriptions/PrescriptionDetailResponse.cs
src/MediCloud.Core/DTOs/Prescriptions/PrescriptionItemResponse.cs
src/MediCloud.Core/DTOs/Billing/CreateBillRequest.cs
src/MediCloud.Core/DTOs/Billing/BillItemRequest.cs
src/MediCloud.Core/DTOs/Billing/AddPaymentRequest.cs
src/MediCloud.Core/DTOs/Billing/BillResponse.cs
src/MediCloud.Core/DTOs/Billing/BillDetailResponse.cs
src/MediCloud.Core/DTOs/Billing/BillItemResponse.cs
src/MediCloud.Core/DTOs/Billing/PaymentResponse.cs
src/MediCloud.Core/DTOs/Documents/UploadDocumentRequest.cs
src/MediCloud.Core/DTOs/Documents/DocumentResponse.cs
src/MediCloud.Core/DTOs/Documents/FileUploadInfo.cs         ← record; carries Stream + metadata; keeps IFormFile out of Core
src/MediCloud.Core/DTOs/LabResults/LabObservationResponse.cs
src/MediCloud.Core/DTOs/LabResults/LabResultResponse.cs
src/MediCloud.Core/DTOs/LabResults/LabResultDetailResponse.cs
src/MediCloud.Core/DTOs/LabOrders/LabTestCatalogResponse.cs
src/MediCloud.Core/DTOs/LabOrders/CreateLabOrderRequest.cs
src/MediCloud.Core/DTOs/LabOrders/LabOrderItemResponse.cs
src/MediCloud.Core/DTOs/LabOrders/LabOrderResponse.cs
src/MediCloud.Core/DTOs/LabOrders/LabOrderDetailResponse.cs
src/MediCloud.Core/DTOs/LabOrders/ManualResultRequest.cs
src/MediCloud.Core/DTOs/Audit/AuditLogResponse.cs
src/MediCloud.Core/DTOs/Audit/AuditLogQueryRequest.cs
```

**MediCloud.Infrastructure**
```
src/MediCloud.Infrastructure/DependencyInjection.cs                          ← AddInfrastructure() extension
src/MediCloud.Infrastructure/Data/AppDbContext.cs                            ← global query filters + SaveChangesAsync TenantId injection
src/MediCloud.Infrastructure/Data/Configurations/TenantConfiguration.cs
src/MediCloud.Infrastructure/Data/Configurations/RoleConfiguration.cs       ← seeds 6 roles
src/MediCloud.Infrastructure/Data/Configurations/UserConfiguration.cs
src/MediCloud.Infrastructure/Data/Configurations/PatientConfiguration.cs
src/MediCloud.Infrastructure/Data/Configurations/PatientAllergyConfiguration.cs
src/MediCloud.Infrastructure/Data/Configurations/AppointmentConfiguration.cs
src/MediCloud.Infrastructure/Data/Configurations/ConsultationConfiguration.cs
src/MediCloud.Infrastructure/Data/Configurations/PrescriptionConfiguration.cs
src/MediCloud.Infrastructure/Data/Configurations/PrescriptionItemConfiguration.cs
src/MediCloud.Infrastructure/Data/Configurations/BillConfiguration.cs       ← DECIMAL(12,2), computed BalanceDue
src/MediCloud.Infrastructure/Data/Configurations/BillItemConfiguration.cs   ← computed TotalPrice
src/MediCloud.Infrastructure/Data/Configurations/PaymentConfiguration.cs
src/MediCloud.Infrastructure/Data/Configurations/PatientDocumentConfiguration.cs
src/MediCloud.Infrastructure/Data/Configurations/LabResultConfiguration.cs  ← unique index (TenantId, AccessionNumber)
src/MediCloud.Infrastructure/Data/Configurations/LabObservationConfiguration.cs
src/MediCloud.Infrastructure/Middleware/TenantResolutionMiddleware.cs        ← subdomain + X-Tenant-Code header
src/MediCloud.Infrastructure/Services/TenantContext.cs
src/MediCloud.Infrastructure/Services/CurrentUserService.cs
src/MediCloud.Infrastructure/Services/TokenService.cs                        ← JWT generation; 8hr expiry; ClockSkew=zero
src/MediCloud.Infrastructure/Services/AuthService.cs                         ← bcrypt verify; 5-attempt lockout; 30min lock
src/MediCloud.Infrastructure/Services/PatientService.cs                      ← MRN generation; search; allergy management
src/MediCloud.Infrastructure/Services/AppointmentService.cs                  ← availability check; status transitions
src/MediCloud.Infrastructure/Services/ConsultationService.cs                 ← SOAP; sign-off; auto-advance appointment
src/MediCloud.Infrastructure/Services/PrescriptionService.cs                 ← dispense workflow; pending queue
src/MediCloud.Infrastructure/Services/BillingService.cs                      ← INV generation; payment; status workflow
src/MediCloud.Infrastructure/Services/BlobStorageService.cs                  ← upload, delete, SAS URI (15-min expiry)
src/MediCloud.Infrastructure/Services/DocumentService.cs                     ← per-tenant containers, blob path routing
src/MediCloud.Infrastructure/Services/Hl7Parser.cs                           ← static; parses ORU^R01 → ParsedOruR01 record
src/MediCloud.Infrastructure/Services/MllpListenerService.cs                 ← BackgroundService; TCP port 2575; MLLP framing
src/MediCloud.Infrastructure/Services/LabResultService.cs                    ← GetByPatient, GetByAccession, GetById
src/MediCloud.Infrastructure/Services/LabOrderService.cs                     ← catalog, place order, waiting list, receive sample, manual result, sign; accession ACC-{YEAR}-{D5}
src/MediCloud.Infrastructure/Services/AuditService.cs                        ← writes AuditLog; captures UserId, Email, IP
src/MediCloud.Infrastructure/Data/Configurations/AuditLogConfiguration.cs   ← BIGINT IDENTITY; no FK; 3 indexes
src/MediCloud.Infrastructure/Data/Configurations/LabTestCatalogConfiguration.cs ← global; seeds 19 tests mapped to instruments
src/MediCloud.Infrastructure/Data/Configurations/LabOrderConfiguration.cs   ← indexes on TenantId+PatientId, Status, CreatedAt
src/MediCloud.Infrastructure/Data/Configurations/LabOrderItemConfiguration.cs ← unique index on (TenantId, AccessionNumber) filtered NOT NULL
src/MediCloud.Infrastructure/Data/AppDbContextFactory.cs                     ← IDesignTimeDbContextFactory; used only by EF CLI
src/MediCloud.Infrastructure/Data/Migrations/                                ← EF Core migration files (InitialSchema + LabOrderModule)
```

### Key architectural decisions made
- `TenantEntity` base class: `TenantId` auto-injected in `SaveChangesAsync` for all Added entities
- `PatientAllergy` and `PrescriptionItem` do NOT inherit TenantEntity (no timestamps in schema) — TenantId set manually in service
- `SecondaryDiagnoses` on Consultation stored as raw JSON string (`nvarchar(max)`) — serialized/deserialized in ConsultationService using `System.Text.Json`
- MRN format: `MRN-{YEAR}-{D5}` — sequential within tenant+year, unique constraint guards concurrent inserts
- `AppointmentStatus.CanTransition()` encodes the full state machine; invalid transitions return `409`
- Sign-off on consultation auto-completes the linked appointment
- Creating a consultation auto-advances appointment to InProgress
- `appsettings.Development.json` has the dev DB connection string — must NOT be committed to git
- `BillItem` does NOT inherit TenantEntity (no timestamps) — TenantId set manually in BillingService (same pattern as PrescriptionItem)
- `BalanceDue` and `TotalPrice` are SQL computed columns mapped with `.HasComputedColumnSql(..., stored: true)`
- Payment overpayment is blocked in service: `req.Amount > bill.BalanceDue` → 400
- Bill INV format: `INV-{YEAR}-{D5}` — sequential within tenant+year, unique constraint guards concurrent inserts
- Blob container per tenant: `tenant-{sanitized-tenantCode}` — built in `DocumentService.BuildContainerName()`; containers created with `PublicAccessType.None`
- Blob path: `patients/{patientId}/{documentId}/{sanitized-filename}` — documentId in path guarantees no collisions on re-upload
- SAS URIs have 15-minute expiry (read-only); generated in `BlobStorageService.GenerateSasUri()` using account-key credential (works for Azurite + Azure)
- `BlobServiceClient` registered as **singleton**; `DocumentService` is **scoped** (needs tenant context)
- `BlobStorage:ConnectionString` placeholder in `appsettings.json`; real value comes from Key Vault / user-secrets
- `LabObservation` does NOT inherit TenantEntity (no timestamps) — TenantId set manually in MllpListenerService (same pattern as BillItem/PrescriptionItem)
- `MllpListenerService` is a `BackgroundService` (singleton); uses `IServiceScopeFactory` to create a scoped `AppDbContext` per message
- Tenant resolved in MLLP from MSH-4 (SendingFacility = TenantCode); `TenantContext` properties set before tenant-filtered queries
- Ordering doctor resolved from OBR-16 component 0 as a Guid UserId — Mirth Connect must be configured to populate this field with the internal UserId
- Doctor notification implemented as structured `ILogger` output; NOTIFICATION: prefix makes it queryable in log aggregators; seam for future email/push
- Duplicate accession guard uses the global query filter (tenant-scoped) before insert; unique index on (TenantId, AccessionNumber) as DB-level safety net
- Raw HL7 payload stored in `RawHl7` (nvarchar(max)) for audit and replay purposes
- `LabTestCatalog` is global (no TenantId, no global query filter) — all tenants share the same test catalog; seeded via `HasData()` in `LabTestCatalogConfiguration`
- `LabOrderItem` does NOT inherit TenantEntity (no timestamps) — TenantId set manually in LabOrderService (same pattern as PrescriptionItem/BillItem)
- `LabResult.LabOrderItemId` (nullable FK) — set by `MllpListenerService` when incoming HL7 accession matches a pending `LabOrderItem`; links automated results back to the order
- Accession numbers: `ACC-{YEAR}-{NNNNN}` — generated in `LabOrderService.GenerateAccessionNumberAsync()` on phlebotomist "Received" click; sequential per tenant per year; unique index on `(TenantId, AccessionNumber)` filtered `NOT NULL`
- `MllpListenerService` updated: on HL7 receive, looks up `LabOrderItem` by AccessionNumber → sets status to Resulted, links LabResultId, recalculates parent `LabOrder.Status`
- `LabOrder.Status` computed from items: any incomplete → Active/PartiallyCompleted; all Resulted → Completed; all Signed → Signed
- `LabOrderItem.IsTatExceeded` computed at query time: `SampleReceivedAt + TatHours < UtcNow` and not yet resulted/signed
- Barcode printing: browser-based via `window.open()` print dialog — label shows patient name, MRN, test name, accession number; no external library needed
- `AuditLog` does NOT inherit TenantEntity — BIGINT IDENTITY PK, no FK on TenantId/UserId (survives deletions), TenantId set manually in AuditService
- `AuditLog.TenantId` has a global query filter for tenant isolation at query time
- `AuditService` injected into `PatientService`; logs Patient.Create, Patient.View, Patient.Update immediately after each DB save
- `ICurrentUserService` extended with `Email` property (reads `JwtRegisteredClaimNames.Email` claim) — used for denormalized UserEmail field on audit log
- `AuditLogsController` restricted to SuperAdmin + Admin roles; supports filter by patientId, userId, action, date range; paginated newest-first
- `Patient.RegisteredAt` property was REMOVED — it conflicted with `PatientConfiguration` mapping `CreatedAt → RegisteredAt` column; `CreatedAt` is the single source of truth; all response DTOs read `p.CreatedAt`
- `AppDbContextFactory` (IDesignTimeDbContextFactory) passes an empty `TenantContext` to the constructor — safe at design time because global query filters are not evaluated during migrations
- EF CLI tool: `dotnet-ef` v10.0.5 installed globally
- `Microsoft.EntityFrameworkCore.Design 8.0.0` added to `MediCloud.API.csproj` (required by EF CLI)
- Migration applied to local **SQL Server 2022 Express** (`.\SQLEXPRESS`, Windows auth, `TrustServerCertificate=True`) — no Docker needed on this machine
- `appsettings.Development.json` connection string: `Server=.\SQLEXPRESS;Database=MediCloudDb;Integrated Security=True;TrustServerCertificate=True;`
- Database verified: 16 tables originally; LabOrderModule migration adds `LabTestCatalog`, `LabOrders`, `LabOrderItems` → 19 tables total
- To run future migrations: `dotnet ef migrations add <Name> --project src/MediCloud.Infrastructure --startup-project src/MediCloud.API --output-dir Data/Migrations`
- To apply: `dotnet ef database update --project src/MediCloud.Infrastructure --startup-project src/MediCloud.API`

---

## React Frontend

### Status
**All modules complete including Lab Order Module.** Last successful build: `npm run build` → 0 errors, 0 warnings.

### Tech versions installed
- Vite 8.0.2, React 19, TypeScript
- Tailwind CSS v4 (via `@tailwindcss/vite` plugin — no `tailwind.config.js`, uses `@import "tailwindcss"` in CSS)
- react-router-dom, axios

### Dev setup
```
cd frontend
npm run dev        # starts on http://localhost:5173, proxies /api → http://localhost:5000
npm run build      # production build to dist/
```

### Key architectural decisions
- JWT + tenantCode stored in `localStorage` under key `auth` as JSON (`AuthUser`)
- Axios interceptor auto-attaches `Authorization: Bearer <token>` and `X-Tenant-Code` on every request
- 401 response → clears localStorage and redirects to `/login`
- `AuthContext` holds user state; `ProtectedRoute` wraps all authenticated routes with optional role check
- `Layout` sidebar filters nav items by role (e.g. Audit Logs only shown to SuperAdmin/Admin)
- Vite dev proxy: `/api` → `localhost:5000` (configured in `vite.config.ts`)
- `GET /api/users?role=Doctor` endpoint added to backend (`UsersController.cs`) for doctor dropdowns
- All money values displayed as `GHS {amount}` (Ghana Cedis)
- Patient search widget is a reusable pattern used across Consultations, Prescriptions, Billing, Documents, Lab Results, Audit Logs pages

### Frontend file structure
```
frontend/
├── src/
│   ├── api/
│   │   ├── client.ts           ← axios instance; JWT + tenant interceptors; 401 redirect
│   │   ├── auth.ts             ← POST /api/auth/login
│   │   ├── patients.ts         ← search, get, create, allergies CRUD
│   │   ├── appointments.ts     ← calendar, CRUD, all status transitions, listDoctors
│   │   ├── consultations.ts    ← get, create, update, sign
│   │   ├── prescriptions.ts    ← pending queue, patient history, get, create, dispense, cancel
│   │   ├── billing.ts          ← outstanding, patient bills, get, create, issue, payment, cancel, void
│   │   ├── documents.ts        ← list, upload (multipart/form-data), download URL (SAS), delete
│   │   ├── labResults.ts       ← by patient, by ID, by accession number
│   │   ├── labOrders.ts        ← catalog, waiting list, place order, receive, manual result, sign
│   │   └── audit.ts            ← paginated query with filters
│   ├── components/
│   │   ├── Layout.tsx          ← sidebar nav (role-filtered) + main content outlet
│   │   └── ProtectedRoute.tsx  ← redirects to /login or /unauthorized
│   ├── contexts/
│   │   └── AuthContext.tsx     ← login/logout, persists to localStorage
│   ├── pages/
│   │   ├── auth/LoginPage.tsx
│   │   ├── patients/           ← PatientsListPage, PatientDetailPage, CreatePatientPage
│   │   ├── appointments/       ← AppointmentsPage (calendar+list), AppointmentDetailPage, CreateAppointmentPage
│   │   ├── consultations/      ← ConsultationsPage, ConsultationDetailPage, CreateConsultationPage
│   │   ├── prescriptions/      ← PrescriptionsPage, PrescriptionDetailPage, CreatePrescriptionPage
│   │   ├── billing/            ← BillingPage, BillDetailPage, CreateBillPage
│   │   ├── documents/          ← DocumentsPage (upload + list + download)
│   │   ├── lab-results/        ← LabResultsPage, LabResultDetailPage
│   │   ├── lab-orders/         ← LabWaitingListPage, PlaceLabOrderPage, LabOrderDetailPage
│   │   └── audit-logs/         ← AuditLogsPage
│   ├── types/
│   │   ├── index.ts            ← AuthUser, PagedResult, Roles
│   │   ├── patients.ts
│   │   ├── appointments.ts     ← status colors, transition map, AppointmentTypes
│   │   ├── consultations.ts
│   │   ├── prescriptions.ts    ← dosage forms, frequencies
│   │   ├── billing.ts          ← status colors, payment methods, bill categories
│   │   ├── documents.ts        ← categories, formatBytes()
│   │   ├── labResults.ts
│   │   ├── labOrders.ts        ← LabOrder, LabOrderItem, LabTestCatalog, status colors, DEPARTMENTS
│   │   └── audit.ts            ← AUDIT_ACTIONS
│   ├── App.tsx                 ← BrowserRouter + all routes
│   ├── main.tsx
│   └── index.css               ← @import "tailwindcss"
└── vite.config.ts              ← @tailwindcss/vite plugin + /api proxy
```

### Backend additions made during frontend builds
- `src/MediCloud.API/Controllers/UsersController.cs` — `GET /api/users?role=<roleName>`; returns active users filtered by role; used by appointment scheduling for doctor dropdown

### Lab Order Module — workflow implemented (2026-03-26)
Modelled on CrelioHealth waiting list UI observed at a target clinic in Ghana.

**Full workflow:**
```
Doctor places order (consultation or standalone)
    → Phlebotomist clicks "Received" → AccessionNumber generated (ACC-YYYY-NNNNN)
    → Barcode label printed (browser print dialog, no external library)
    → Tube sent to lab machine → barcode scanned in
    → Machine transmits HL7 ORU^R01 via Mirth Connect → MLLP port 2575
    → MllpListenerService matches AccessionNumber → LabOrderItem marked Resulted
    → Doctor sees result in Lab Results + Lab Worklist
    [MANUAL TESTS: malaria, WIDAL, urinalysis etc. → lab tech enters result directly]
    → Doctor/admin signs off each item → Order marked Signed
```

**19 tests seeded in LabTestCatalog** (global, not tenant-scoped):
- Haematology/DxH560: FBC, ESR
- Haematology/Manual: MPS (Malaria film)
- Chemistry/DxC500: BUE, LFT, Magnesium, Calcium, FBG, RBG, Lipid Profile
- Immunology/CobasE411: TFT, Vitamin B12, Folate, HbA1c
- Serology/Manual: Typhoid IgG/IgM, Widal, HBsAg, HIV
- Urinalysis/Manual: Urine Routine Exam

**Frontend pages:**
- `/lab-orders` — Waiting List (CrelioHealth-style): left status sidebar, date+department filters, expandable patient rows with per-test actions (Received, Print, Enter Result, Sign), Incomplete/Completed/Signed counts
- `/lab-orders/new` — Place Order: patient search, test catalog grouped by department, organisation field
- `/lab-orders/:id` — Order Detail: per-test status timeline, barcode print, manual entry modal, link to HL7 result