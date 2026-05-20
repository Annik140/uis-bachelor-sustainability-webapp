# uis-bachelor-sustainability-webapp

## Setup

### Database Setup

This project uses PostgreSQL with Entity Framework Core. Before running, you need a PostgreSQL instance.

1. Install [PostgreSQL](https://www.postgresql.org/download/) (or use Docker).
2. Create a development database:
```sql
CREATE DATABASE sustainability_db_dev;
```
3. Set the connection string. Choose one:
   - Create a `.env` file in the project root (see `.env.example`):
     ```
     DefaultConnection=Host=localhost;Port=5432;Database=sustainability_db_dev;Username=postgres;Password=your-postgres-password
     ```
   - Or set the `DefaultConnection` environment variable.
   - Or update `appsettings.Development.json` locally (not committed).

4. Apply migrations on first run (the backend will auto-create tables).

### Admin Credentials Setup

This project uses a lightweight cookie-based admin login. Admin credentials are read from the environment and must be configured before starting the backend.

Use `dotnet user-secrets` so credentials are not committed to the repository.

1. Configure admin credentials (project root):

```powershell
dotnet user-secrets init
dotnet user-secrets set "ADMIN_USER" "your-admin"
dotnet user-secrets set "ADMIN_PASSWORD" "your-password"
```

2. Start the backend (project root):

```powershell
dotnet watch run
```

3. Start the frontend (separate terminal, inside `Transparent`):

```bash
cd Transparent
npm install
npm run dev
```

4. Open the Vite URL shown in the `npm run dev` terminal and visit `/admin/login` to sign in.

Convenience script

You can run `scripts/start-dev.ps1` (PowerShell) to be prompted for admin credentials and start both backend and frontend for local development. The script does not store credentials; it uses `dotnet user-secrets` for the backend and launches both servers.
