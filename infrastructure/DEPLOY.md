# KayCare HMS — Deployment Guide (Railway + Supabase + Vercel)

## Monthly Cost Estimate

| Service | Free Tier | Cost |
|---|---|---|
| Railway (Hobby) | $5 credit/month included | $0 |
| Supabase (Free) | 500 MB DB · 1 GB storage · 2 GB bandwidth | $0 |
| Vercel (Hobby) | Unlimited deploys · 100 GB bandwidth | $0 |
| **Total** | | **$0/month** |

---

## Prerequisites

- [Railway CLI](https://docs.railway.app/guides/cli) installed (`npm i -g @railway/cli`)
- [Vercel CLI](https://vercel.com/docs/cli) installed (`npm i -g vercel`)
- [Supabase account](https://supabase.com) (free — no card required)
- [Railway account](https://railway.app) (free hobby plan)
- `.NET 8 SDK` + `dotnet-ef` tools installed locally

---

## Step 1 — Create a Supabase Project

1. Go to [app.supabase.com](https://app.supabase.com) → **New project**
2. Set a strong database password and save it
3. After creation, go to **Settings → API** and copy:
   - **Project URL** → this is `Supabase:Url`
   - **service_role** secret key → this is `Supabase:ServiceKey`
4. Go to **Settings → Database** and copy the **Connection string (URI)** format:
   ```
   postgresql://postgres:[YOUR-PASSWORD]@db.[ref].supabase.co:5432/postgres
   ```
   Convert to Npgsql format:
   ```
   Host=db.[ref].supabase.co;Port=5432;Database=postgres;Username=postgres;Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true;
   ```
   This is your `ConnectionStrings:DefaultConnection`.

---

## Step 2 — Run Migrations Against Supabase

From the repo root (PowerShell):

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=db.[ref].supabase.co;Port=5432;Database=postgres;Username=postgres;Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true;"

dotnet ef database update `
  --project src/KayCare.Infrastructure `
  --startup-project src/KayCare.API
```

This applies the single `InitialPostgres` migration, creating all tables.

---

## Step 3 — Seed the Demo Tenant

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=db.[ref].supabase.co;Port=5432;..."

cd tools/Seeder
dotnet run
```

Demo credentials after seeding:
- **Email:** admin@demo.com
- **Password:** Admin@1234
- **Tenant Code:** demo (use `X-Tenant-Code: demo` header)

---

## Step 4 — Deploy the Backend to Railway

```powershell
# Login
railway login

# Create a new project
railway init --name kaycare-hms

# Link the service
railway link

# Set environment variables
railway variables set `
  ASPNETCORE_ENVIRONMENT=Production `
  ConnectionStrings__DefaultConnection="Host=db.[ref].supabase.co;Port=5432;Database=postgres;Username=postgres;Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true;" `
  Jwt__Key="[YOUR-JWT-SECRET-MIN-32-CHARS]" `
  Jwt__Issuer="KayCare" `
  Jwt__Audience="KayCare" `
  Supabase__Url="https://[ref].supabase.co" `
  Supabase__ServiceKey="[YOUR-SERVICE-ROLE-KEY]" `
  Cors__AllowedOrigins__0="https://[your-vercel-url].vercel.app"

# Deploy (Railway auto-detects Dockerfile)
railway up --service kaycare-api
```

After deploy, Railway will show your public URL (e.g. `https://kaycare-api.up.railway.app`).

---

## Step 5 — Deploy the Frontend to Vercel

```powershell
cd frontend

# Set the Railway API URL in production env
# Edit .env.production and replace PLACEHOLDER with your Railway URL
# e.g. VITE_API_URL=https://kaycare-api.up.railway.app/api

vercel --prod
```

Or connect via [vercel.com](https://vercel.com) → Import Git Repository → select `kaycare-hms` → set root directory to `frontend`.

---

## Step 6 — Update CORS

Once your Vercel URL is known (e.g. `https://kaycare-hms.vercel.app`), update Railway:

```powershell
railway variables set Cors__AllowedOrigins__0="https://kaycare-hms.vercel.app"
```

And update `frontend/.env.production`:
```
VITE_API_URL=https://kaycare-api.up.railway.app/api
```

---

## Step 7 — Configure GitHub Actions Secrets

In GitHub → Settings → Secrets → Actions, add:

| Secret | Value |
|---|---|
| `RAILWAY_TOKEN` | From `railway whoami --token` |
| `VERCEL_TOKEN` | From [vercel.com/account/tokens](https://vercel.com/account/tokens) |
| `VERCEL_ORG_ID` | From `.vercel/project.json` after `vercel link` |
| `VERCEL_PROJECT_ID` | From `.vercel/project.json` after `vercel link` |
| `VITE_API_URL` | `https://kaycare-api.up.railway.app/api` |

Push to `main` — both workflows auto-trigger.

---

## Generate a JWT Signing Key

```powershell
[System.Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
```

Use the output as `Jwt__Key` in Railway env vars.

---

## Architecture Diagram

```
Browser
  │
  └─► Vercel (React SPA)
          │  HTTPS API calls
          └─► Railway (ASP.NET Core 8 via Docker)
                  │  EF Core / Npgsql
                  ├─► Supabase PostgreSQL (database)
                  │  Supabase C# SDK
                  └─► Supabase Storage (patient docs, logos)
```

---

## Known Gotchas

### Railway cold starts
Railway free tier may sleep after inactivity. First request after sleep takes ~5–15 seconds. Upgrade to the Pro plan ($20/month) to avoid this.

### Supabase connection pooling
Use the **Session Mode** pooler (port 5432) for EF Core. The Transaction Mode pooler (port 6543) is incompatible with EF Core migrations and `SaveChanges`.

### MLLP Listener (HL7 port 2575)
The MLLP background service runs on TCP port 2575. Railway only exposes one public HTTP port. The MLLP port is internal only — lab equipment must reach the service via a VPN or private network tunnel. This is the expected hospital network topology.

### Supabase Storage bucket naming
Bucket names follow the same rules as before (lowercase, letters/digits/hyphens, 3-63 chars). The existing `BuildContainerName` helper in `DocumentService` and `FacilitySettingsService` is fully compatible.
