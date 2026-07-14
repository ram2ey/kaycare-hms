# Deep Audit Report: Page Errors Investigation

During the audit, we observed that while both the frontend and backend applications build and start successfully, interacting with the application (e.g. attempting to log in via the frontend) results in a **500 Internal Server Error**. 

## Root Cause
The core issue originates from the backend `KayCare.API`. It is failing because the **Database Connection String is missing**. 

When the frontend calls the login endpoint (`/api/Auth/login`), the server throws the following exception:
> `The ConnectionString property has not been initialized.`

This happens because the EF Core `AppDbContext` is expecting a `DefaultConnection` connection string to connect to the database, but it is not defined in any of your local configuration files.

## Detailed Findings

1. **Missing Local Configuration:**
   Your `appsettings.json` file inside `src/KayCare.API` does not include a `ConnectionStrings` section. Typically, for local development, you should have an `appsettings.Development.json` or rely on User Secrets/Environment Variables to supply the database credentials. Currently, none of these are present.

2. **Missing JWT and Supabase Keys:**
   Alongside the missing database connection string, `appsettings.json` has several placeholder values that must be replaced for the app to function properly:
   - `Jwt:Key` is set to `"PLACEHOLDER-set-via-Render-env-var"`
   - `Supabase:Url` and `Supabase:ServiceKey` are set to `"PLACEHOLDER-set-via-Render-env-var"`

3. **Database Provider Mismatch (Tooling vs. App):**
   - **Application Code:** In `src/KayCare.Infrastructure/DependencyInjection.cs`, the application is configured to use **PostgreSQL** (`options.UseNpgsql(...)`).
   - **Seeding Script:** The `seed.csx` file located in the root directory is configured to use **SQL Server** (`Microsoft.Data.SqlClient`) and points to a local SQL Express instance (`Server=.\SQLEXPRESS;Database=MediCloudDb;...`). This script is incompatible with the PostgreSQL database the app actually uses.

## Recommended Fixes

To resolve the errors and run the application locally, you should:

1. **Create an `appsettings.Development.json`** inside `src/KayCare.API` with a local PostgreSQL connection string and test values for JWT/Supabase:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=kaycare_hms;Username=postgres;Password=yourpassword;"
     },
     "Jwt": {
       "Key": "a-very-long-and-secure-local-test-key-for-jwt-12345!"
     }
   }
   ```
2. **Update Database Seeding Tools:** Refactor `seed.csx` (or `tools/Seeder/Program.cs`) to use `Npgsql` instead of `Microsoft.Data.SqlClient` so that it can correctly seed your PostgreSQL database.
3. **Run Migrations:** Ensure your PostgreSQL database is running and apply EF Core migrations using `dotnet ef database update --project src\KayCare.Infrastructure --startup-project src\KayCare.API`.
