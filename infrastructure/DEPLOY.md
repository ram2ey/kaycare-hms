# KayCare HMS — Deployment Guide (Render + Supabase Storage)

## Stack

| Role | Service |
|---|---|
| Backend API | Render Web Service (Docker, ASP.NET Core 8) |
| Frontend | Render Static Site (React / Vite) |
| Database | Render PostgreSQL |
| File Storage | Supabase Storage (documents, logos, lab reports) |

## Monthly Cost Estimate (free tier)

| Service | Free Tier |
|---|---|
| Render Web Service | Free (sleeps after 15 min inactivity) |
| Render PostgreSQL | Free for 90 days → Starter $7/month |
| Render Static Site | Always free |
| Supabase (Storage only) | 1 GB storage · 2 GB bandwidth |
| **Total** | **$0** (or $7/month for persistent DB) |

---

## Phase A — Before You Touch Render

### A1 — Verify Prerequisites

- [ ] .NET 8 SDK installed → `dotnet --version` (must show `8.x.x`)
- [ ] `dotnet-ef` tool installed:
  ```powershell
  dotnet tool install --global dotnet-ef
  dotnet ef --version   # confirm output
  ```
- [ ] Code is committed and pushed to GitHub (`main` branch)
- [ ] Render account created at [render.com](https://render.com) (free, no card needed)
- [ ] Supabase account created at [supabase.com](https://supabase.com) (free, no card needed)

---

### A2 — Generate a JWT Signing Key (save it — you'll need it in Phase B)

Run this in PowerShell and copy the output:

```powershell
[System.Convert]::ToBase64String(
  (1..64 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]]
)
```

Save the output string somewhere safe (e.g. a Notepad file). It must be at least 32 characters.

---

## Phase B — Supabase Setup (Storage Only)

You only need Supabase for file/document storage. Skip any database sections on their site.

### B1 — Create a Supabase Project

1. Go to [app.supabase.com](https://app.supabase.com)
2. Click **New project**
3. Choose or create an **Organization**
4. Fill in:
   - **Name:** `kaycare-hms` (or any name)
   - **Database password:** Set a strong password — **save it**, even though you won't use it for DB access
   - **Region:** Pick the closest to your users
5. Click **Create new project**
6. Wait ~2 minutes for the project to finish provisioning (green checkmark)

### B2 — Copy Your Supabase Credentials

1. In your project, go to **Settings** (gear icon in the left sidebar)
2. Click **API**
3. Under **Project URL**, copy the URL — this is your `Supabase__Url`
   - It looks like: `https://abcdefghijklmno.supabase.co`
4. Under **Project API Keys**, find the **service_role** row
5. Click **Reveal** then copy the key — this is your `Supabase__ServiceKey`

> ⚠️ **Keep `service_role` secret.** It bypasses Row Level Security. Never expose it in the frontend.

**Save both values.** You'll enter them in Render in Phase C.

---

## Phase C — Render: Provision Infrastructure via Blueprint

### C1 — Connect Your Repository

1. Go to [dashboard.render.com](https://dashboard.render.com)
2. Click **New +** in the top-right
3. Select **Blueprint**
4. If this is your first time, click **Connect a Git provider** → select **GitHub**
5. Authorize Render to access your GitHub account
6. Search for and select the `kaycare-hms` repository
7. Click **Connect**

### C2 — Review the Blueprint Resources

Render reads `render.yaml` from the repo root and shows you three resources:

| Resource | Type | Plan |
|---|---|---|
| `kaycare-hms-db` | PostgreSQL | Free |
| `kaycare-hms-api` | Web Service (Docker) | Free |
| `kaycare-hms-frontend` | Static Site | Free |

Verify all three appear before continuing.

### C3 — Fill in Secret Environment Variables

Render will prompt you for variables marked `sync: false` in `render.yaml`. Fill them in:

| Variable | Value | Notes |
|---|---|---|
| `Jwt__Key` | The base64 string from **A2** | Min 32 chars |
| `Supabase__Url` | URL from **B2** | `https://xxxx.supabase.co` |
| `Supabase__ServiceKey` | Key from **B2** | Starts with `eyJ...` |
| `Cors__AllowedOrigins__0` | *(leave blank for now)* | Set in Phase E |
| `VITE_API_URL` | *(leave blank for now)* | Set in Phase E |

### C4 — Apply the Blueprint

1. Click **Apply**
2. Render begins provisioning in this order:
   - `kaycare-hms-db` is created first (takes ~1–2 minutes)
   - `kaycare-hms-api` build starts after the DB is ready (takes ~3–5 minutes — Docker build)
   - `kaycare-hms-frontend` builds in parallel
3. Wait until the dashboard shows all three as **Active** / **Live**

> ⚠️ The API will show as **Live** but will be returning 500 errors until migrations are run in Phase D. This is expected.

---

## Phase D — Database: Migrations + Seed

### D1 — Get the External Postgres Connection String

1. In Render dashboard → click **kaycare-hms-db**
2. Click the **Info** tab
3. Under **Connections**, find **External Database URL** — copy it. It looks like:
   ```
   postgres://kaycare_user:PASSWORD@dpg-XXXXXXXXXXXXXXXX-a.oregon-postgres.render.com/kaycare_hms
   ```
4. Convert it to Npgsql key=value format (substitute your actual values):
   ```
   Host=dpg-XXXXXXXXXXXXXXXX-a.oregon-postgres.render.com;Port=5432;Database=kaycare_hms;Username=kaycare_user;Password=PASSWORD;SSL Mode=Require;Trust Server Certificate=true;
   ```

> The API uses the **internal** connection string automatically (injected by Render). This Npgsql format is **only for running migrations locally**.

### D2 — Run Migrations

Open PowerShell, navigate to the `kaycare-hms` repo root, then:

```powershell
# 1. Set the connection string in this PowerShell session
$env:ConnectionStrings__DefaultConnection = "Host=dpg-XXXXXXXXXXXXXXXX-a.oregon-postgres.render.com;Port=5432;Database=kaycare_hms;Username=kaycare_user;Password=PASSWORD;SSL Mode=Require;Trust Server Certificate=true;"

# 2. Apply migrations
dotnet ef database update `
  --project src/KayCare.Infrastructure `
  --startup-project src/KayCare.API
```

**Expected output:**
```
Build started...
Build succeeded.
Applying migration '20260619001026_InitialPostgres'.
Done.
```

If you see `Done.` — all tables are created. ✅

### D3 — Seed the Demo Tenant

In the same PowerShell session (connection string still set):

```powershell
# Navigate into the Seeder tool
cd tools/Seeder

# Run the seeder
dotnet run
```

**Expected output:** Confirmation that tenant, admin user, and sample data were created.

After seeding you can log in with:
- **Email:** `admin@demo.com`
- **Password:** `Admin@1234`
- **Tenant Code:** `demo` (HTTP header: `X-Tenant-Code: demo`)

```powershell
# Return to repo root when done
cd ../..
```

---

## Phase E — Wire Up the Services

### E1 — Find Your Render Service URLs

1. Render dashboard → click **kaycare-hms-api**
2. At the top of the page, copy the URL — e.g. `https://kaycare-hms-api.onrender.com`
3. Render dashboard → click **kaycare-hms-frontend**
4. Copy its URL — e.g. `https://kaycare-hms-frontend.onrender.com`

### E2 — Update CORS on the API

The API currently has a blank CORS origin, so the frontend can't call it. Fix this:

1. Render dashboard → **kaycare-hms-api** → **Environment** tab
2. Find `Cors__AllowedOrigins__0`
3. Set value to your frontend URL: `https://kaycare-hms-frontend.onrender.com`
4. Click **Save Changes**
5. Render automatically triggers a redeploy — wait for it to finish (~2 min)

### E3 — Update the Frontend API URL

The frontend currently has a blank API URL. Fix this:

1. Render dashboard → **kaycare-hms-frontend** → **Environment** tab
2. Find `VITE_API_URL`
3. Set value to your API URL + `/api`: `https://kaycare-hms-api.onrender.com/api`
4. Click **Save Changes**
5. Render automatically rebuilds the static site — wait for it to finish (~1 min)

### E4 — Verify the Live Application

1. Open `https://kaycare-hms-frontend.onrender.com` in your browser
2. You should see the KayCare HMS login page
3. Log in with `admin@demo.com` / `Admin@1234` / tenant code `demo`
4. Verify you can navigate around the app
5. Open `https://kaycare-hms-api.onrender.com/swagger` — Swagger UI should load

---

## Phase F — GitHub Actions CI/CD

### F1 — Get the API Deploy Hook URL

1. Render dashboard → **kaycare-hms-api** → **Settings** tab
2. Scroll down to **Deploy Hook**
3. Click **Generate Deploy Hook** (if not already generated)
4. Copy the full URL — it looks like:
   ```
   https://api.render.com/deploy/srv-XXXXXXXXXXXX?key=XXXXXXXXXXXX
   ```

### F2 — Get the Frontend Deploy Hook URL

1. Render dashboard → **kaycare-hms-frontend** → **Settings** tab
2. Scroll down to **Deploy Hook**
3. Click **Generate Deploy Hook**
4. Copy the full URL

### F3 — Add Secrets to GitHub

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** for each:

| Secret Name | Value |
|---|---|
| `RENDER_DEPLOY_HOOK_URL` | Deploy hook URL from **F1** |
| `RENDER_FRONTEND_DEPLOY_HOOK_URL` | Deploy hook URL from **F2** |

### F4 — Test the CI/CD Pipeline

1. Make a small change (e.g. add a comment to any file)
2. Commit and push to `main`
3. Go to GitHub → **Actions** tab
4. You should see two workflows triggered:
   - **Backend — Build & Deploy** (runs tests, then triggers Render)
   - **Frontend — Build & Deploy** (triggers Render directly)
5. Verify both complete with green ✅
6. In Render dashboard, verify new deploys appear in the **Deploys** tab of each service

---

## Architecture Diagram

```
Browser
  │
  └─► Render Static Site (React SPA)
          │  HTTPS API calls (/api/...)
          └─► Render Web Service (ASP.NET Core 8, Docker)
                  │  EF Core / Npgsql (internal network)
                  ├─► Render PostgreSQL (database)
                  │  Supabase C# SDK
                  └─► Supabase Storage (patient docs, logos, reports)
```

---

## Known Gotchas

### Render free tier cold starts
The free Web Service sleeps after 15 minutes of inactivity. First request after sleep takes ~30 seconds. Upgrade to the **Starter plan ($7/month)** to disable sleep.

### Render Postgres free tier expiry
Free Postgres databases are deleted after **90 days**. Upgrade to Starter ($7/month) before going live in production.

### EF Core migrations — use external connection string
The internal connection string injected into the API container is only reachable from within Render's network. When running `dotnet ef database update` locally, always use the **external** connection string with `SSL Mode=Require;Trust Server Certificate=true;`.

### Supabase Storage bucket naming
Bucket names must be lowercase, letters/digits/hyphens, 3–63 chars. The existing `BuildContainerName` helper in `DocumentService` and `FacilitySettingsService` is fully compatible.

### MLLP Listener (HL7 port 2575)
The MLLP background service runs on TCP port 2575. Render Web Services only expose one public HTTP port. The MLLP port is internal only — lab equipment must reach the service via a VPN or private network tunnel. This is the expected hospital network topology.
