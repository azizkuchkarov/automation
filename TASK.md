# ATG Unified Platform — TASK.md
## Phase 1: Foundation + Admin Panel + User Management

---

## Project Overview

**Client:** Asia Trans Gas JV LLC (ATG)  
**Platform:** ATG Unified Workspace  
**Phase:** 1 of N — Foundation, Auth, Admin Panel, User Management  
**Stack:**
- Frontend: Next.js 15 (App Router, TypeScript, Tailwind CSS)
- Backend: ASP.NET Core 10 (C#, Minimal API + Controllers)
- Database: PostgreSQL (`host: localhost`, `user: postgres`, `password: postgres`, `db: automation`)
- ORM: Entity Framework Core 10
- Auth: JWT (Access Token 15min + Refresh Token 7d, HttpOnly cookie)
- Languages: Russian / English (i18n via `next-intl`)
- NO Redis, NO SignalR in this phase

---

## Organization Structure (ATG-specific)

```
Tashkent Head Office (HO)
└── BMGMC
    ├── WKC1
    ├── WKC2
    ├── UCS1
    ├── GCS
    ├── WKC3
    ├── UCS3
    ├── MS
    └── UKMS
```

---

## Roles

| Role | Access |
|---|---|
| `SuperAdmin` | Full platform control |
| `HOTopManager` | All organizations, all modules |
| `HONachalnik` | HO department, all modules |
| `HOEngineer` | Own tasks only |
| `BMGMCManager` | BMGMC + all stations |
| `BMGMCNachalnikiOtdeli` | Own department in BMGMC |
| `BMGMCEngineer` | Own tasks only |
| `StationEngineer` | Own station, own tasks only |

---

## Modules (icons shown on home screen after login)

| Module | Route | Icon | Description |
|---|---|---|---|
| Business Automation | `/automation` | `briefcase` | Document Control System (DCS) |
| HelpDesk | `/helpdesk` | `headset` | IT & support tickets |
| HR System | `/hr` | `users` | Staff, leave, attendance |
| Task Navigation | `/tasks` | `layout-kanban` | Tasks by role/org visibility |

---

## Database Schema

### Table: `organizations`
```sql
id          UUID PRIMARY KEY DEFAULT gen_random_uuid()
name        VARCHAR(100) NOT NULL          -- "Tashkent Head Office", "BMGMC", "WKC1" etc.
code        VARCHAR(20) NOT NULL UNIQUE    -- "HO", "BMGMC", "WKC1", "WKC2", "UCS1", "GCS", "WKC3", "UCS3", "MS", "UKMS"
parent_id   UUID REFERENCES organizations(id) NULL
org_type    VARCHAR(20) NOT NULL           -- "HeadOffice", "BMGMC", "Station"
is_active   BOOLEAN DEFAULT true
created_at  TIMESTAMPTZ DEFAULT now()
```

### Table: `departments`
```sql
id              UUID PRIMARY KEY DEFAULT gen_random_uuid()
organization_id UUID NOT NULL REFERENCES organizations(id)
name            VARCHAR(100) NOT NULL
code            VARCHAR(30) NOT NULL
is_active       BOOLEAN DEFAULT true
created_at      TIMESTAMPTZ DEFAULT now()
```

### Table: `positions`
```sql
id          UUID PRIMARY KEY DEFAULT gen_random_uuid()
name        VARCHAR(100) NOT NULL    -- "Engineer", "Manager", "Nachalnik otdeli" etc.
code        VARCHAR(30) NOT NULL
is_active   BOOLEAN DEFAULT true
```

### Table: `users`
```sql
id                  UUID PRIMARY KEY DEFAULT gen_random_uuid()
employee_id         VARCHAR(20) UNIQUE              -- "ATG-001" etc.
first_name          VARCHAR(50) NOT NULL
last_name           VARCHAR(50) NOT NULL
middle_name         VARCHAR(50)                     -- otchestvo
email               VARCHAR(100) UNIQUE NOT NULL
phone               VARCHAR(20)
password_hash       TEXT NOT NULL
organization_id     UUID NOT NULL REFERENCES organizations(id)
department_id       UUID REFERENCES departments(id)
position_id         UUID REFERENCES positions(id)
role                VARCHAR(30) NOT NULL            -- enum: SuperAdmin, HOTopManager, HONachalnik, HOEngineer, BMGMCManager, BMGMCNachalnikiOtdeli, BMGMCEngineer, StationEngineer
is_active           BOOLEAN DEFAULT true
avatar_url          TEXT
language            VARCHAR(5) DEFAULT 'ru'         -- 'ru' | 'en'
created_at          TIMESTAMPTZ DEFAULT now()
updated_at          TIMESTAMPTZ DEFAULT now()
last_login_at       TIMESTAMPTZ
```

### Table: `refresh_tokens`
```sql
id          UUID PRIMARY KEY DEFAULT gen_random_uuid()
user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE
token       TEXT NOT NULL UNIQUE
expires_at  TIMESTAMPTZ NOT NULL
created_at  TIMESTAMPTZ DEFAULT now()
is_revoked  BOOLEAN DEFAULT false
```

### Table: `audit_logs`
```sql
id           UUID PRIMARY KEY DEFAULT gen_random_uuid()
user_id      UUID REFERENCES users(id)
action       VARCHAR(100) NOT NULL    -- "USER_CREATED", "USER_DEACTIVATED", "LOGIN" etc.
entity_type  VARCHAR(50)              -- "User", "Organization", "Department"
entity_id    UUID
details      JSONB
ip_address   VARCHAR(45)
created_at   TIMESTAMPTZ DEFAULT now()
```

---

## Backend — ASP.NET Core 10

### Project Structure
```
ATG.Platform.sln
├── ATG.Platform.API/              ← Main API project
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── UsersController.cs
│   │   ├── OrganizationsController.cs
│   │   ├── DepartmentsController.cs
│   │   └── PositionsController.cs
│   ├── Middleware/
│   │   ├── ExceptionMiddleware.cs
│   │   └── AuditMiddleware.cs
│   ├── Program.cs
│   └── appsettings.json
├── ATG.Platform.Application/      ← Business logic
│   ├── Auth/
│   │   ├── LoginCommand.cs
│   │   ├── RefreshTokenCommand.cs
│   │   └── LogoutCommand.cs
│   ├── Users/
│   │   ├── CreateUserCommand.cs
│   │   ├── UpdateUserCommand.cs
│   │   ├── DeactivateUserCommand.cs
│   │   ├── GetUsersQuery.cs
│   │   └── GetUserByIdQuery.cs
│   ├── Organizations/
│   └── Common/
│       ├── Result.cs              ← Result<T> pattern
│       └── PagedResult.cs
├── ATG.Platform.Domain/           ← Entities, enums
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Organization.cs
│   │   ├── Department.cs
│   │   ├── Position.cs
│   │   └── AuditLog.cs
│   └── Enums/
│       └── UserRole.cs
└── ATG.Platform.Infrastructure/   ← EF Core, repositories
    ├── Data/
    │   ├── AppDbContext.cs
    │   └── Migrations/
    ├── Repositories/
    └── Seeds/
        └── DatabaseSeeder.cs      ← Initial data (orgs, superadmin)
```

### API Endpoints

#### Auth
```
POST   /api/auth/login             Body: { email, password } → { accessToken, user }
POST   /api/auth/refresh           Cookie: refreshToken → { accessToken }
POST   /api/auth/logout            Revokes refresh token
GET    /api/auth/me                Returns current user profile
```

#### Users (Admin only)
```
GET    /api/users                  Query: page, pageSize, search, orgId, role, isActive
GET    /api/users/{id}
POST   /api/users                  Create user
PUT    /api/users/{id}             Update user
PATCH  /api/users/{id}/deactivate  Soft deactivate
PATCH  /api/users/{id}/activate
PATCH  /api/users/{id}/reset-password
GET    /api/users/export           CSV export
```

#### Organizations
```
GET    /api/organizations          Returns tree structure
GET    /api/organizations/{id}
POST   /api/organizations          SuperAdmin only
PUT    /api/organizations/{id}
```

#### Departments
```
GET    /api/departments            Query: orgId
POST   /api/departments
PUT    /api/departments/{id}
DELETE /api/departments/{id}       Soft delete
```

#### Positions
```
GET    /api/positions
POST   /api/positions
PUT    /api/positions/{id}
```

#### Audit
```
GET    /api/audit-logs             Query: userId, action, from, to, page
```

### Key Implementation Rules

1. **Result<T> pattern** — never throw exceptions for business logic:
```csharp
public record Result<T>(bool IsSuccess, T? Data, string? Error)
{
    public static Result<T> Ok(T data) => new(true, data, null);
    public static Result<T> Fail(string error) => new(false, default, error);
}
```

2. **Password hashing** — use `BCrypt.Net-Next` (cost factor 12)

3. **JWT config** in `appsettings.json`:
```json
{
  "Jwt": {
    "SecretKey": "atg-platform-secret-key-min-32-chars-long",
    "Issuer": "atg-platform",
    "Audience": "atg-users",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=automation;Username=postgres;Password=postgres"
  }
}
```

4. **CORS** — allow `http://localhost:3000` in development

5. **Database Seed** — on first run, create:
   - All 10 organizations (HO, BMGMC, 8 stations) with correct parent relationships
   - Default SuperAdmin: `email: admin@atg.uz`, `password: Admin@2024!`
   - 5 default departments per organization
   - 8 default positions

6. **Soft delete** — never hard delete users. Use `is_active = false`.

7. **Audit everything** — user create/update/deactivate/login must write to `audit_logs`.

---

## Frontend — Next.js 15

### Project Structure
```
atg-platform/
├── app/
│   ├── [locale]/                  ← next-intl locale routing (ru | en)
│   │   ├── (auth)/
│   │   │   └── login/
│   │   │       └── page.tsx       ← Login page
│   │   ├── (platform)/
│   │   │   ├── layout.tsx         ← Shell: sidebar + topbar
│   │   │   ├── home/
│   │   │   │   └── page.tsx       ← Module selector (4 cards)
│   │   │   ├── automation/
│   │   │   │   └── page.tsx       ← Business Automation (placeholder)
│   │   │   ├── helpdesk/
│   │   │   │   └── page.tsx       ← HelpDesk (placeholder)
│   │   │   ├── hr/
│   │   │   │   └── page.tsx       ← HR System (placeholder)
│   │   │   └── tasks/
│   │   │       └── page.tsx       ← Task Navigation (placeholder)
│   │   └── admin/
│   │       ├── layout.tsx         ← Admin shell
│   │       ├── page.tsx           ← Admin dashboard
│   │       ├── users/
│   │       │   ├── page.tsx       ← Users list
│   │       │   ├── [id]/
│   │       │   │   └── page.tsx   ← User detail / edit
│   │       │   └── new/
│   │       │       └── page.tsx   ← Create user
│   │       ├── organizations/
│   │       │   └── page.tsx
│   │       ├── departments/
│   │       │   └── page.tsx
│   │       └── audit/
│   │           └── page.tsx
├── components/
│   ├── layout/
│   │   ├── Sidebar.tsx
│   │   ├── TopBar.tsx
│   │   └── ModuleCard.tsx
│   ├── ui/                        ← Base components (shadcn/ui based)
│   │   ├── Button.tsx
│   │   ├── Input.tsx
│   │   ├── Badge.tsx
│   │   ├── DataTable.tsx          ← Reusable table with sort/filter/pagination
│   │   ├── Modal.tsx
│   │   ├── Avatar.tsx
│   │   └── LanguageToggle.tsx
│   └── admin/
│       ├── UserForm.tsx           ← Create / edit user form
│       ├── UserStatusBadge.tsx
│       └── OrgTree.tsx            ← Organization tree view
├── lib/
│   ├── api.ts                     ← Axios instance with interceptors
│   ├── auth.ts                    ← Token management
│   └── utils.ts
├── hooks/
│   ├── useAuth.ts
│   └── useUsers.ts                ← React Query hooks
├── store/
│   └── authStore.ts               ← Zustand: current user, role
├── messages/
│   ├── ru.json                    ← Russian translations
│   └── en.json                    ← English translations
└── middleware.ts                  ← Auth guard + locale redirect
```

### Pages Specification

#### 1. Login Page (`/[locale]/login`)

**Design:**
- Full screen, centered card (max-width: 420px)
- ATG logo + "ATG Unified Platform" title
- Subtitle: "Asia Trans Gas JV LLC"
- Fields: Email, Password (with show/hide toggle)
- "Sign in" button (full width, primary)
- Language switcher bottom right: RU | EN
- Error message inline (wrong credentials)
- No "forgot password" in this phase

**Behavior:**
- On submit → POST `/api/auth/login`
- Store `accessToken` in memory (Zustand)
- Store `refreshToken` in HttpOnly cookie (set by backend)
- Redirect to `/home` after success
- If user is SuperAdmin or HOTopManager → show admin badge on avatar

---

#### 2. Home — Module Selector (`/[locale]/home`)

**Design:**
- Top bar: ATG logo, user name + role badge, org name, language toggle, logout
- Page title: "ATG Unified Platform" / "Платформа АТГ"
- 4 large module cards in 2×2 grid (or row on wide screens):

| Card | Icon | Title RU | Title EN | Color accent |
|---|---|---|---|---|
| Business Automation | `ti-briefcase` | Бизнес Автоматизация | Business Automation | Blue |
| HelpDesk | `ti-headset` | Служба поддержки | HelpDesk | Teal |
| HR System | `ti-users` | Кадровая система | HR System | Purple |
| Task Navigation | `ti-layout-kanban` | Навигация задач | Task Navigation | Amber |

Each card:
- Large icon (48px)
- Module title (18px, weight 500)
- Short description (13px, muted)
- Subtle right-arrow on hover
- Click → navigate to module route

---

#### 3. Admin Panel (`/[locale]/admin`)

Access: `SuperAdmin`, `HOTopManager` only. Redirect others to `/home`.

**Admin Sidebar items:**
- Dashboard (overview stats)
- Users (`/admin/users`)
- Organizations (`/admin/organizations`)
- Departments (`/admin/departments`)
- Positions (`/admin/positions`)
- Audit Log (`/admin/audit`)
- ← Back to Platform

**Admin Dashboard:**
- Stat cards: Total users, Active users, Organizations, Departments
- Recent activity table (last 10 audit log entries)
- Quick action buttons: "Add user", "View pending"

---

#### 4. Users List (`/admin/users`)

**Toolbar:**
- Search input (by name, email, employee ID)
- Filter: Organization (dropdown), Role (dropdown), Status (Active/Inactive)
- Button: "Add user" (primary)
- Button: "Export CSV"

**Table columns:**
| Column | Notes |
|---|---|
| Employee | Avatar + Full name + employee_id |
| Organization | Badge with org code color |
| Department | Text |
| Position | Text |
| Role | Badge (color by role level) |
| Status | Active (green) / Inactive (red) |
| Last login | Relative time |
| Actions | Edit icon, Deactivate/Activate toggle |

- Pagination: 20 per page
- Click row → opens user detail page
- Bulk select → bulk deactivate

---

#### 5. Create / Edit User Form (`/admin/users/new`, `/admin/users/[id]`)

**Fields:**
```
Section: Personal info
- First name *        (text)
- Last name *         (text)  
- Middle name         (text, otchestvo)
- Employee ID *       (text, e.g. ATG-001, auto-suggest next)
- Email *             (email)
- Phone               (text)

Section: Organization
- Organization *      (select, shows tree: HO / BMGMC / Station)
- Department *        (select, filtered by org)
- Position *          (select)
- Role *              (select, limited by current admin's own role)

Section: Access
- Language            (RU / EN toggle)
- Password *          (only on create; on edit: separate "Reset password" button)
- Confirm password *  (only on create)
```

**Validation:**
- All `*` fields required
- Email: unique check (debounced API call)
- Employee ID: unique check
- Password: min 8 chars, 1 uppercase, 1 number

**On save:**
- POST `/api/users` (create) or PUT `/api/users/{id}` (edit)
- Show success toast
- Redirect to users list

---

#### 6. Organization Tree (`/admin/organizations`)

- Visual tree: HO → BMGMC → Stations
- Each node: org name, code badge, user count, active status
- Inline edit: name, code
- Cannot delete orgs with active users

---

### i18n Keys (ru.json / en.json structure)

```json
{
  "common": {
    "save": "Сохранить / Save",
    "cancel": "Отмена / Cancel",
    "delete": "Удалить / Delete",
    "edit": "Редактировать / Edit",
    "search": "Поиск / Search",
    "loading": "Загрузка / Loading",
    "error": "Ошибка / Error",
    "success": "Успешно / Success"
  },
  "auth": {
    "title": "АТГ Платформа / ATG Platform",
    "email": "Электронная почта / Email",
    "password": "Пароль / Password",
    "signIn": "Войти / Sign in",
    "invalidCredentials": "Неверный email или пароль / Invalid email or password"
  },
  "nav": {
    "automation": "Бизнес Автоматизация / Business Automation",
    "helpdesk": "Служба поддержки / HelpDesk",
    "hr": "Кадровая система / HR System",
    "tasks": "Навигация задач / Task Navigation",
    "admin": "Администрирование / Administration"
  },
  "users": {
    "title": "Сотрудники / Employees",
    "addUser": "Добавить сотрудника / Add employee",
    "firstName": "Имя / First name",
    "lastName": "Фамилия / Last name",
    "middleName": "Отчество / Middle name",
    "employeeId": "Табельный номер / Employee ID",
    "organization": "Организация / Organization",
    "department": "Отдел / Department",
    "position": "Должность / Position",
    "role": "Роль / Role",
    "status": "Статус / Status",
    "active": "Активен / Active",
    "inactive": "Неактивен / Inactive",
    "lastLogin": "Последний вход / Last login"
  }
}
```

---

## Implementation Order (strict sequence for Cursor)

```
Step 1: Backend — Database + EF Core
  - Create all 5 entities with correct relationships
  - AppDbContext with configurations
  - Initial migration
  - DatabaseSeeder (10 orgs + superadmin + positions)

Step 2: Backend — Auth
  - JWT service (generate + validate access token)
  - Refresh token logic (HttpOnly cookie)
  - AuthController: login, refresh, logout, me
  - Auth middleware

Step 3: Backend — Users CRUD
  - User entity methods (Create, Update, Deactivate)
  - UsersController with all endpoints
  - Pagination + filtering logic
  - Audit log middleware

Step 4: Backend — Orgs, Departments, Positions
  - Simple CRUD controllers
  - Tree structure for organizations

Step 5: Frontend — Setup
  - Next.js 15 project with TypeScript + Tailwind
  - next-intl setup (ru/en)
  - Axios instance with JWT interceptor + auto-refresh
  - Zustand auth store
  - middleware.ts (auth guard)

Step 6: Frontend — Login page
  - Form with validation
  - API integration
  - Redirect logic

Step 7: Frontend — Shell layout (Sidebar + TopBar)
  - Role-based nav items
  - Language toggle
  - User avatar + dropdown

Step 8: Frontend — Home (module selector)
  - 4 module cards
  - Navigation

Step 9: Frontend — Admin: Users list
  - DataTable with server-side pagination
  - Search + filter
  - Status badges

Step 10: Frontend — Admin: User form
  - Create + Edit
  - Cascading dropdowns (org → dept)
  - Validation

Step 11: Frontend — Admin: Orgs + Audit log
  - Organization tree
  - Audit log table

Step 12: Placeholder pages
  - /automation, /helpdesk, /hr, /tasks
  - Each shows "Coming soon" with module icon
```

---

## Design System

**Colors (Tailwind custom):**
```js
// tailwind.config.ts
colors: {
  atg: {
    blue:   '#2563eb',   // Business Automation
    teal:   '#0d9488',   // HelpDesk  
    purple: '#7c3aed',   // HR System
    amber:  '#d97706',   // Task Navigation
    dark:   '#0d1117',   // Dark bg
    surface:'#161b22',   // Dark surface
    border: '#30363d',   // Dark border
  }
}
```

**Typography:** Inter font (Google Fonts)

**Component library:** shadcn/ui (pre-configured with Tailwind)

**Dark/Light mode:** Default dark, toggle available, persisted in localStorage

**Density:** Compact (Jira-like) — table rows 40px, sidebar items 34px

---

## Acceptance Criteria

- [ ] SuperAdmin can log in with `admin@atg.uz` / `Admin@2024!`
- [ ] SuperAdmin sees all 4 module cards on home
- [ ] SuperAdmin can navigate to `/admin`
- [ ] Admin can create a new user with all fields
- [ ] Admin can search/filter users by org, role, status
- [ ] Admin can deactivate/activate a user
- [ ] All 10 organizations visible in org dropdown
- [ ] Language switches between RU and EN instantly
- [ ] Dark/Light theme toggle works
- [ ] JWT refresh works transparently (no logout on token expiry)
- [ ] All actions logged in audit_logs table
- [ ] Mobile responsive (min 375px)

---

## Out of Scope (Phase 1)

- Redis caching
- SignalR real-time
- File uploads / avatars
- Email notifications
- Password reset via email
- Business Automation module content
- HelpDesk module content
- HR System module content
- Task Navigation content
