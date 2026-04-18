# KayCare HMS — Azure Deployment Guide (Free Tier)

## Monthly Cost Estimate

| Resource | Tier | Cost |
|---|---|---|
| App Service Plan F1 | Free | $0 |
| Azure SQL Serverless GP | Free offer (100k vCore-sec/month) | $0* |
| Blob Storage LRS | Pay-as-you-go | ~$1 |
| Key Vault | Standard | ~$0.50 |
| Static Web Apps | Free | $0 |
| **Total** | | **~$1.50/month** |

*SQL free offer covers 100,000 vCore-seconds/month. If exhausted the DB auto-pauses instead of billing.

---

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- `az login` completed
- Resource group `kaycare-rg` already exists (created previously)

---

## Step 1 — Generate a JWT Signing Key (if you don't have one)

```powershell
[System.Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
```

Save the output — you'll need it at deploy time.

---

## Step 2 — Deploy All Azure Resources

Run from the repo root:

```powershell
az deployment group create `
  --resource-group kaycare-rg `
  --template-file infrastructure/bicep/main-consolidated.bicep `
  --parameters sqlAdminPassword="<YourSQLPassword>" `
               jwtKey="<YourJwtKey>" `
  --output table
```

This creates (or updates) all resources in one idempotent deployment:
- Key Vault `kaycare-prod-kv` — stores SQL password, JWT key, connection strings
- SQL Server `kaycare-prod-sql` + Serverless database `KayCareDb`
- Blob Storage `kaycareprodstorstor`
- App Service Plan `kaycare-prod-plan` (F1 Free) + API app `kaycare-prod-api`
- Static Web App `kaycare-prod-web`

**Note on SQL free offer:** The `useFreeLimit: true` flag only applies to the first Azure SQL Serverless database per subscription. If you have other Serverless databases, this one may not qualify.

---

## Step 3 — Set the Connection String Directly in App Service

The Key Vault reference for the connection string has a known resolution lag. Set it directly as a fallback:

```powershell
az webapp config connection-string set `
  --name kaycare-prod-api `
  --resource-group kaycare-rg `
  --settings DefaultConnection="Server=tcp:kaycare-prod-sql.database.windows.net,1433;Initial Catalog=KayCareDb;Persist Security Info=False;User ID=kaycare_admin;Password=<YourSQLPassword>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" `
  --connection-string-type SQLAzure
```

---

## Step 4 — Run All EF Core Migrations Against Azure SQL

Run from the repo root (PowerShell):

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=tcp:kaycare-prod-sql.database.windows.net,1433;Initial Catalog=KayCareDb;User ID=kaycare_admin;Password=<YourSQLPassword>;Encrypt=True;Connection Timeout=30;"

dotnet ef database update `
  --project src/KayCare.Infrastructure `
  --startup-project src/KayCare.API
```

This applies all migrations including:
`AddPharmacyInventory`, `AddPurchaseOrders`, `AddInpatientBilling`, `AddDischargeSummaryFields`,
`AddNursingModule`, `AddReferrals`, `AddIcdCodes`, `AddLabTechnicianRole`, `AddBillingOfficerRole`

---

## Step 5 — Seed the Demo Tenant

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=tcp:kaycare-prod-sql.database.windows.net,1433;Initial Catalog=KayCareDb;User ID=kaycare_admin;Password=<YourSQLPassword>;Encrypt=True;Connection Timeout=30;"

cd tools/Seeder
dotnet run
```

Demo credentials after seeding:
- **Email:** admin@demo.com
- **Password:** Admin@1234
- **Tenant Code:** demo (use `X-Tenant-Code: demo` header)

---

## Step 6 — Configure GitHub Actions Secrets

### Backend — App Service publish profile

```powershell
az webapp deployment list-publishing-profiles `
  --name kaycare-prod-api `
  --resource-group kaycare-rg `
  --xml
```

Copy the full XML output. In GitHub → Settings → Secrets → Actions, update secret:
- `AZURE_WEBAPP_PUBLISH_PROFILE` = (paste XML)

### Frontend — Static Web App deploy token

```powershell
az deployment group show `
  --resource-group kaycare-rg `
  --name main-consolidated `
  --query properties.outputs.staticWebDeployToken.value `
  --output tsv
```

Update GitHub secret:
- `AZURE_STATIC_WEB_APPS_API_TOKEN` = (paste token)

---

## Step 7 — Trigger CI/CD

```powershell
# Trigger backend deploy
gh workflow run backend.yml

# Trigger frontend deploy
gh workflow run frontend.yml
```

Or push a commit to main — both workflows auto-trigger.

---

## Deployment Outputs

After `az deployment group create` completes:

| Output | Value |
|---|---|
| `apiUrl` | `https://kaycare-prod-api.azurewebsites.net` |
| `staticWebUrl` | `https://kaycare-prod-web.azurestaticapps.net` (or assigned URL) |
| `keyVaultName` | `kaycare-prod-kv` |
| `sqlServerFqdn` | `kaycare-prod-sql.database.windows.net` |

---

## Known Issues & Workarounds

### Key Vault reference not resolving for connection string
The App Service managed identity correctly has Key Vault Secrets User role, but the
`@Microsoft.KeyVault(...)` reference syntax sometimes takes 5–10 minutes to resolve after
first deployment. The direct connection string set in Step 3 bypasses this entirely.

### Tenant middleware on .azurewebsites.net
`TenantResolutionMiddleware` is already patched to skip subdomain resolution for
`.azurewebsites.net` and `.azurestaticapps.net` hosts.

### F1 Free tier cold starts
The F1 plan does not support Always On. Expect 10–30 second cold start on first request
after inactivity. This is expected behaviour on the free tier.

### SQL Serverless auto-pause
The database auto-pauses after 60 minutes of inactivity. First query after pause takes
~30 seconds to resume. Acceptable for dev/demo; upgrade to Basic DTU ($5/month) for
production traffic that cannot tolerate resume latency.
