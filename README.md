# KayCare HMS — Hospital Management System

KayCare Hospital Management System (HMS) is an enterprise-grade, multi-tenant digital health platform engineered to unify outpatient consultations, inpatient care, pharmacy operations, diagnostic laboratory workflows, radiology imaging, and financial revenue cycle management into a centralized, high-performance system.

Built on a Modular Monolith architecture, KayCare HMS offers strict transactional integrity, domain isolation, and horizontal scalability while eliminating operational friction between clinical and administrative departments.

---

## Technical Stack

### Backend & Core API
- **Framework**: ASP.NET Core 8.0 Web API
- **Language**: C# 12
- **Persistence & ORM**: Entity Framework Core 8.0
- **Database Engine**: PostgreSQL 15+ with EF Core Global Query Filters for tenant isolation
- **Messaging & Analyzer Connectivity**: HL7 v2.x MLLP protocol engine
- **Authentication & Authorization**: JWT Bearer tokens with Role-Based Access Control (RBAC)

### Frontend Workspace
- **Framework**: React 18
- **Build Tool**: Vite
- **Language**: TypeScript
- **Styling**: Tailwind CSS
- **State Management & Routing**: React Router v6, Context API

---

## Core System Modules

1. **Patient EMR & Outpatient Consultations**: Centralized patient registry with unique Medical Record Numbers (MRN), longitudinal vitals tracking, allergy alerts, SOAP note documentation, and ICD-10 codification.
2. **Inpatient Department (IPD) & Nursing Workspace**: Dynamic bed occupancy management, ward transfer workflows, Medication Administration Records (MAR), and nursing flowsheets.
3. **Laboratory Information System (LIS)**: Specimen accessioning lifecycle (Ordered -> Sample Received -> Resulted -> Signed Off), panic-value critical alerts, and automated HL7 analyzer interface ingestion.
4. **Pharmacy & Inventory Lifecycle**: Electronic prescribing, real-time stock deduction on dispensing, controlled substances register, purchase order processing, and low-stock threshold alerts.
5. **Radiology & PACS Imaging**: Exam scheduling, worklist management, and integrated WebGL zero-footprint DICOM image viewing.
6. **Billing & Revenue Cycle Management (RCM)**: Centralized charge capture automatically aggregating clinical items, lab tests, and medications into unified patient ledgers and insurance claims.

---

## Repository Structure

```
kaycare-hms/
├── docs/                      # Architectural overview, client specifications, and database schema
├── frontend/                  # React + TypeScript single-page application
├── infrastructure/            # Terraform, Render, and cloud deployment configs
├── src/
│   ├── KayCare.API/           # ASP.NET Core Web API host, endpoints, middleware
│   ├── KayCare.Core/          # Domain models, business rules, interface abstractions
│   ├── KayCare.Infrastructure/  # DbContext, EF Core migrations, repository implementations
│   └── KayCare.Tests/         # Automated unit and integration test project
├── tools/                     # Utility scripts and database seed helpers
├── Dockerfile                 # Multi-stage production container build
├── KayCare.sln                # .NET Solution File
└── render.yaml                # Render platform deployment manifest
```

---

## Prerequisites

Before setting up the project locally, ensure you have the following installed:

- **.NET 8.0 SDK** or higher
- **Node.js** (v18.0.0 or higher) and **npm** (v9.0.0 or higher)
- **PostgreSQL Server** (v15.0 or higher)

---

## Getting Started

### 1. Database Configuration
Ensure your PostgreSQL instance is running. Create a target database (e.g., `kaycare_hms`).

Update the connection string in `src/KayCare.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=kaycare_hms;Username=postgres;Password=yourpassword"
  }
}
```

### 2. Backend Setup & Run

1. Navigate to the root directory and restore .NET dependencies:
   ```bash
   dotnet restore
   ```

2. Apply database migrations:
   ```bash
   dotnet ef database update --project src/KayCare.Infrastructure --startup-project src/KayCare.API
   ```

3. Start the Web API server:
   ```bash
   dotnet run --project src/KayCare.API
   ```
   By default, the REST API listens on `https://localhost:7198` and `http://localhost:5242`. OpenAPI/Swagger UI is accessible at `https://localhost:7198/swagger`.

### 3. Frontend Setup & Run

1. Open a new terminal session and navigate to the `frontend` directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the Vite development server:
   ```bash
   npm run dev
   ```
   The application will be accessible at `http://localhost:5173`.

---

## Testing

To execute automated backend test suites:

```bash
dotnet test
```

To run frontend linting and type checks:

```bash
cd frontend
npm run lint
```

---

## Related Documentation

- [KAYCARE_HMS_OVERVIEW.md](file:///c:/Users/asnah/Desktop/KayCare%20Suite/kaycare-hms/docs/KAYCARE_HMS_OVERVIEW.md) — Detailed Enterprise Architecture & Functional Specification
- [DEPLOY.md](file:///c:/Users/asnah/Desktop/KayCare%20Suite/kaycare-hms/infrastructure/DEPLOY.md) — Infrastructure Deployment & Production Operational Guide
- [frontend/README.md](file:///c:/Users/asnah/Desktop/KayCare%20Suite/kaycare-hms/frontend/README.md) — Frontend Architecture Details
