# PolicyEngine

A portfolio-grade, multi-tenant policy management system built to demonstrate enterprise and government-ready software engineering. PolicyEngine provides a Blazor WebAssembly frontend, a secured ASP.NET Core 10 API, Auth0-based authentication with role-based access control, and a full CI/CD pipeline deployed on Azure free-tier services.

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor WebAssembly |
| Backend | ASP.NET Core 10 Web API |
| Auth | Auth0 (JWT, RBAC) |
| Database | Azure SQL (Free Tier) |
| Hosting | Azure App Service (F1) + Azure Static Web Apps |
| CI/CD | GitHub Actions |

## Project Structure

```
PolicyEngine/
├── PolicyEngine.Api/           # ASP.NET Core 10 Web API — JWT auth, role enforcement, CORS, CRUD endpoints
├── PolicyEngine.Authorization/ # Core authorization logic and policy evaluation
├── PolicyEngine.Persistence/   # EF Core DbContext, repositories, migrations, shadow properties
├── PolicyEngine.Shared/        # Shared entities, DTOs, and interfaces
├── PolicyEngine.Tests/         # xUnit unit and integration tests
├── PolicyEngine.Web/           # Blazor WebAssembly frontend — MSAL auth, policy pages
└── .github/workflows/          # GitHub Actions CI/CD pipelines
```

## Architecture

PolicyEngine follows a clean three-tier architecture with identity delegated to Auth0:

```
[Blazor WASM]  →  (Bearer JWT)  →  [ASP.NET Core API]  →  [Azure SQL]
     ↑                                      ↑
  Azure Static                          App Service F1
  Web Apps (CDN)                    (Microsoft.Identity.Web)
                        ↑
                      Auth0
               (JWT issuance, App Roles)
```

| Tier | Component | Hosting |
|---|---|---|
| Presentation | Blazor WebAssembly SPA — login, policy list, admin CRUD | Azure Static Web Apps (free) |
| Identity | Auth0 — user directory, JWT issuance, App Roles | Auth0 managed (free tier) |
| API | PolicyEngine.Api — JWT validation, role enforcement, EF Core | Azure App Service F1 (free) |
| Data | PolicyEngine.Persistence + Azure SQL — tenant-isolated policies table | Azure SQL (free, 32 GB) |

## Authentication & Authorization

Auth0 provides authentication. Two App Roles are enforced at both the API and UI layers:

| Role | Permissions |
|---|---|
| `Policy.Admin` | Full CRUD — create, edit, deactivate, and delete policies |
| `Policy.Viewer` | Read-only — view active policies for their tenant |
| Unauthenticated | Redirected to Auth0 login — no API access |

**Multi-tenancy** is implemented via EF Core shadow properties (`TenantId`, `CreatedAt`, `CreatedBy`). The `TenantId` is resolved from the validated JWT `tid` claim — not from an unauthenticated HTTP header — providing cryptographically enforced tenant isolation.

## Prerequisites

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- An [Auth0](https://auth0.com) account (free tier)
- An [Azure](https://portal.azure.com) account (free tier)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (for deployment)

## Getting Started (Local Development)

### 1. Clone the repository

```bash
git clone https://github.com/apexit13/PolicyEngine.git
cd PolicyEngine
```

### 2. Configure Auth0

In the [Auth0 Dashboard](https://manage.auth0.com), create two applications:

- **API** (`PolicyEngine-API`): Register as an API. Set the identifier (audience) to `https://your-tenant.auth0.com/api/v2/`. Create two permissions: `Policy.Admin` and `Policy.Viewer`.
- **SPA** (`PolicyEngine-Web`): Register as a Single Page Application. Add `https://localhost:5002` to Allowed Callback URLs, Logout URLs, and Web Origins. Assign users the `Policy.Admin` or `Policy.Viewer` role via Auth0 roles.

### 3. Set up app settings

In `PolicyEngine.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-local-or-azure-sql-connection-string"
  },
  "Auth0": {
    "Domain": "your-tenant.auth0.com",
    "Audience": "https://your-tenant.auth0.com/api/v2/"
  }
}
```

In `PolicyEngine.Web/wwwroot/appsettings.json`:

```json
{
  "Auth0": {
    "Authority": "https://your-tenant.auth0.com",
    "ClientId": "your-spa-client-id"
  },
  "ApiBaseUrl": "https://localhost:5001"
}
```

### 4. Apply database migrations

```bash
dotnet ef database update --project PolicyEngine.Persistence --startup-project PolicyEngine.Api
```

### 5. Run the application

```bash
# Run the API
dotnet run --project PolicyEngine.Api

# In a separate terminal, run the Blazor frontend
dotnet run --project PolicyEngine.Web
```

The API will be available at `https://localhost:7058/scalar/v1`. The Blazor app will be at `https://localhost:7026`.

> **Local dev note:** When no `Authorization` header is present, the app falls back to mock claims in `Development` mode, so you can test without Auth0 configured.

## Running Tests

```bash
dotnet test PolicyEngine.Tests
```

## API Endpoints

| Method | Endpoint | Role Required | Description |
|---|---|---|---|
| `GET` | `/api/policies` | Viewer, Admin | List all policies for the authenticated tenant |
| `GET` | `/api/policies/{id}` | Viewer, Admin | Get a policy by ID |
| `POST` | `/api/policies` | Admin | Create a new policy |
| `PUT` | `/api/policies/{id}` | Admin | Update a policy |
| `PATCH` | `/api/policies/{id}/toggle` | Admin | Toggle active/inactive status |
| `DELETE` | `/api/policies/{id}` | Admin | Delete a policy |

## Frontend Pages

| Route | Access | Description |
|---|---|---|
| `/` | Public | Landing page with Auth0 login |
| `/policies` | Viewer, Admin | Searchable, filterable policy list with status badges |
| `/policies/new` | Admin only | Create a new policy |
| `/policies/{id}/edit` | Admin only | Edit an existing policy |
| `/unauthorized` | Any | Shown when a Viewer attempts an admin route |

## Deployment (Azure Free Tier)

All services run within Azure's free tier:

| Service | Tier | Purpose |
|---|---|---|
| Azure App Service | F1 (Free) | Hosts `PolicyEngine.Api` |
| Azure Static Web Apps | Free | Hosts `PolicyEngine.Web` Blazor WASM |
| Azure SQL Database | Free (32 GB) | Tenant-isolated policies table |
| GitHub Actions | Free | CI/CD — build, test, deploy on push |

### Deploy steps

```bash
# 1. Create a resource group
az group create --name rg-policyengine-demo --location eastus

# 2. Create Azure SQL
az sql server create --name policyengine-sql --resource-group rg-policyengine-demo ...
az sql db create --name PolicyEngineDb --server policyengine-sql ...

# 3. Create App Service (F1) and deploy API
az appservice plan create --name policyengine-plan --sku F1 ...
az webapp create --name policyengine-api --plan policyengine-plan ...

# 4. Link repo to Azure Static Web Apps (auto-configures GitHub Actions for frontend)
az staticwebapp create --name policyengine-web --source https://github.com/apexit13/PolicyEngine ...
```

After linking, push to `main` and GitHub Actions will build and deploy both projects automatically.

## CI/CD

Two GitHub Actions workflows handle automated builds:

- **`api.yml`** — Triggers on push to `main`. Runs `restore → build → test → publish → deploy` to Azure App Service.
- **`web.yml`** — Auto-generated by Azure Static Web Apps. Deploys the Blazor frontend on every push to `main`.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
