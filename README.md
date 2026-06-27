# ATG Unified Platform

Phase 1: Foundation + Admin Panel + User Management

## Stack

- **Backend:** ASP.NET Core 10 + EF Core + PostgreSQL
- **Frontend:** Next.js 15 + TypeScript + Tailwind CSS + next-intl

## Database

```
Host=localhost;Database=automation;Username=postgres;Password=postgres
```

## Quick Start

### 1. Backend API

```bash
cd automation
dotnet run --project ATG.Platform.API
```

API: http://localhost:5161  
Swagger: http://localhost:5161/swagger

### 2. Frontend

```bash
cd atg-platform
npm install
npm run dev
```

App: http://localhost:3000

## Docker (one command)

Requires [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
# From repository root
docker compose up --build
```

Detached mode:

```bash
docker compose up --build -d
```

Windows PowerShell helper:

```powershell
.\docker-up.ps1          # foreground
.\docker-up.ps1 -Detached
.\docker-up.ps1 -Down    # stop and remove containers
.\docker-up.ps1 -Logs    # follow logs
```

Copy `.env.example` to `.env` to override ports and secrets.

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| API / Swagger | http://localhost:5161/swagger |
| Hangfire | http://localhost:5161/hangfire |
| MinIO console | http://localhost:9001 (`minioadmin` / `minioadmin`) |
| PostgreSQL | `localhost:5432` (db: `automation`) |

First startup runs EF migrations and seeds ~450 users — allow 1–2 minutes before login.

Docker stack: **PostgreSQL**, **MinIO**, **API**, **Next.js** (API proxied via `/api` rewrite).

## Default Login

| Account | Password | Auth type |
|---------|----------|-----------|
| admin@atg.uz | 12345 | Local (platform SuperAdmin) |

LDAP users must be registered in the platform database first. AD validates the password; the platform assigns roles and permissions.

## LDAP Configuration

Set in `appsettings.json` or via environment variables:

| Variable | Default |
|----------|---------|
| `LDAP_SERVER` | DC03.atg.uz |
| `LDAP_PORT` | 389 |
| `LDAP_USE_SSL` | false |
| `LDAP_BASE_DN` | DC=atg,DC=uz |
| `LDAP_BIND_DN` | (empty — direct user bind) |
| `LDAP_BIND_PASSWORD` | (empty) |
| `LDAP_DOMAIN` | atg.uz |
| `LDAP_NETBIOS_NAME` | ATG |

Supported login formats: `username`, `user@atg.uz`, `ATG\user`.

## Tashkent Head Office (HO) — Departments & Users

On API startup, HO departments are seeded automatically (16 departments). Legacy placeholder `HO-SAF` is deactivated.

### Add users manually
1. Admin → **Employees** → **Add employee**
2. Organization: **HO — Tashkent Head Office**
3. Auth type: **LDAP** (default) — email must match AD
4. Select department, position, and HO role

### Bulk import (CSV)
Template: `data/ho-users.template.csv`

```csv
EmployeeId,FirstName,LastName,MiddleName,Email,Phone,DepartmentCode,PositionCode,Role,Language
ATG-002,Ivan,Petrov,,ivan.petrov@atg.uz,,HO-IT,ENGINEER,HOEngineer,ru
```

Admin → Employees → filter **HO** → **Import CSV**

### HO department codes
`HO-EXEC`, `HO-SEC`, `HO-AC`, `HO-FINPLAN`, `HO-ACCT`, `HO-ENGCON`, `HO-NEWPRJ`, `HO-ITDIG`, `HO-MKT`, `HO-CPROC`, `HO-DCPR`, `HO-QHSE`, `HO-GASM`, `HO-LEGAL`, `HO-HR`, `HO-ADM`

Total HO staff seeded: **156 users** (ATG-002 — ATG-157) + SuperAdmin (ATG-001).

## Bukhara MGMC (BMGMC) — Departments & Users

On API startup, BMGMC departments are seeded automatically (16 departments). Legacy placeholder departments (`BMGMC-OPS`, `BMGMC-ENG`, etc.) are deactivated.

### BMGMC department codes
`BMGMC-EXEC`, `BMGMC-SEC`, `BMGMC-HR`, `BMGMC-DCPR`, `BMGMC-ADM`, `BMGMC-ACCT`, `BMGMC-TRANS`, `BMGMC-HSE`, `BMGMC-GCC`, `BMGMC-TECH`, `BMGMC-ITDIG`, `BMGMC-CALIB`, `BMGMC-RMC`, `BMGMC-PIPE`, `BMGMC-SUP`, `BMGMC-DORM`

Total BMGMC staff seeded: **173 users** (ATG-158 — ATG-454, excluding station staff).

## BMGMC Stations

### Regional station WKC1 / UCS1 (`WKC1-UCS1`)

On API startup, station org `WKC1-UCS1` is created under BMGMC. Legacy separate orgs `WKC1` and `UCS1` are deactivated.

**Department codes:** `WKC1-UCS1-EXEC`, `WKC1-UCS1-ENG`, `WKC1-UCS1-OPS`

**Staff seeded:** 37 users (ATG-315 — ATG-351). Skipped: vacant deputy (row 228); 7 support staff without email (drivers, gardeners); facility/service rows (269–276).

### Regional station WKC2 / GCS (`WKC2-GCS`)

Legacy separate orgs `WKC2` and `GCS` are deactivated.

**Department codes:** `WKC2-GCS-EXEC`, `WKC2-GCS-ENG`, `WKC2-GCS-OPS`

**Staff seeded:** 32 users (ATG-352 — ATG-383). Skipped: Kuldoshev, Arazmedov (no email); support staff and facility rows (311–324).

### Regional station WKC3 (`WKC3`)

**Department codes:** `WKC3-EXEC`, `WKC3-ENG`, `WKC3-OPS`

**Staff seeded:** 19 users (ATG-384 — ATG-402). Skipped: gardener, cleaner, drivers, water intake operator (no email); facility rows (351–358).

### Regional station UCS3 (`UCS3`)

**Department codes:** `UCS3-EXEC`, `UCS3-ENG`, `UCS3-OPS`

**Staff seeded:** 13 users (ATG-403 — ATG-415). Skipped: vacant deputy; shift engineers/drivers without email; facility rows.

### Regional station MS / UKMS (`MS-UKMS`)

Legacy orgs `MS` and `UKMS` deactivated.

**Department codes:** `MS-UKMS-EXEC`, `MS-UKMS-ENG`, `MS-UKMS-OPS`

**Staff seeded:** 23 users (ATG-416 — ATG-438). Skipped: gardeners, drivers, facility rows.

### BMGMC Supply & Dormitory

**Department codes:** `BMGMC-SUP`, `BMGMC-DORM`

**Staff seeded:** 16 users (ATG-439 — ATG-454). Skipped: forklift driver, warehouse workers without email; dormitory staff without email.

**Grand total seeded:** HO 156 + BMGMC 173 + Stations 124 = **453 users** (+ SuperAdmin ATG-001).

## Seeded Data

- 10 organizations (HO → BMGMC → 8 stations)
- 8 positions
- 5 departments per organization
- SuperAdmin user

## API Endpoints

- `POST /api/auth/login` — Login
- `POST /api/auth/refresh` — Refresh token
- `GET /api/auth/me` — Current user
- `GET /api/users` — Users list (admin)
- `GET /api/organizations` — Org tree
- `GET /api/audit-logs` — Audit log
