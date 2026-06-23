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

## Default Login

| Email | Password |
|-------|----------|
| admin@atg.uz | Admin@2024! |

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
