# uis-bachelor-sustainability-webapp

Sustainability dashboard for apparel brands with:

- public brand overview and scoring
- admin login and brand management
- ASP.NET Core backend + React/Vite frontend

## Quick Start (Recommended)

From the repository root, run:

```powershell
pwsh ./scripts/start-dev.ps1
```

The script will:

1. Prompt for admin credentials
2. Save them to `dotnet user-secrets`
3. Start backend (`dotnet watch run`)
4. Start frontend (`npm run dev` in `Transparent`)

Then open the frontend URL shown in the npm terminal (typically `http://localhost:5173`) and navigate to `/admin/login`.

### Non-interactive script usage

```powershell
pwsh ./scripts/start-dev.ps1 -AdminUser "admin" -AdminPassword "your-password"
```

If frontend dependencies are already installed:

```powershell
pwsh ./scripts/start-dev.ps1 -SkipFrontendInstall
```

## Manual Setup

### 1. Prerequisites

- .NET SDK 9.x
- Node.js 20+
- npm 10+
- PostgreSQL (local or container)

### 2. Database

Create a development database:

```sql
CREATE DATABASE sustainability_db_dev;
```

Configure connection string via one of these options:

- Update `appsettings.Development.json` locally
- Set environment variable `ConnectionStrings__DefaultConnection`
- Use `.env.example` as a reference template (values must still be applied to environment/appsettings)

Example value:

```text
Host=localhost;Port=5432;Database=sustainability_db_dev;Username=postgres;Password=your-postgres-password
```

### 3. Backend dependencies and admin bootstrap

```powershell
dotnet restore
dotnet user-secrets init
dotnet user-secrets set "ADMIN_BOOTSTRAP_USER" "admin"
dotnet user-secrets set "ADMIN_BOOTSTRAP_PASSWORD" "your-password"
```

Notes:

- minimum admin password length is 6
- legacy keys (`ADMIN_USER`, `ADMIN_PASSWORD`) are also supported

### 4. Frontend dependencies

```powershell
cd Transparent
npm ci
```

### 5. Run the app

Backend (terminal 1, repo root):

```powershell
dotnet watch run
```

Frontend (terminal 2, `Transparent`):

```powershell
npm run dev
```

Admin login:

- URL: `/admin/login`
- Username/password: same values you stored in user-secrets

## Brand Seeding

On first startup, if no brands exist in the database, the real curated brand dataset is seeded automatically. No configuration is needed.

`Seeding:Mode` can also be set explicitly in `appsettings.Development.json` or via environment variable `Seeding__Mode`:

- `None`: skip seeding (auto-seed still runs if no brands exist)
- `Demo`: synthetic demo brands
- `Real`: real-brand seed data

Example (PowerShell):

```powershell
$env:Seeding__Mode="Demo"
dotnet run
```

## Submission Verification Commands

From repository root:

```powershell
dotnet build .\uis-bachelor-sustainability-webapp.csproj -c Release
dotnet test .\tests\uis-bachelor-sustainability-webapp.Tests\uis-bachelor-sustainability-webapp.Tests.csproj -c Release
cd Transparent; npm run lint; npm run build
```

## Troubleshooting

- If you see nested `bin/obj` path warnings in tests, delete generated output folders and run again.
- If admin login fails, re-run user-secrets commands and restart backend.
