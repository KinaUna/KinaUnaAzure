# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build, Test, and Run Commands

```bash
# Build the entire solution
dotnet build KinaUnaAzure.sln

# Run all tests
dotnet test KinaUnaAzure.sln

# Run tests for a single project
dotnet test KinaUnaProgenyApi.Tests/KinaUnaProgenyApi.Tests.csproj
dotnet test KinaUnaWeb.Tests/KinaUnaWeb.Tests.csproj
dotnet test KinaUna.OpenIddict.Tests/KinaUna.OpenIddict.Tests.csproj

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run individual services (requires configured environment variables or appsettings)
dotnet run --project KinaUnaProgenyApi
dotnet run --project KinaUnaWeb
dotnet run --project KinaUna.OpenIddict

# Local development with Docker Compose (copy .env.example to .env first)
docker compose up --build

# Install frontend dependencies (from KinaUnaWeb/)
npm install --prefix KinaUnaWeb
```

## Architecture

KinaUna is a family-oriented child-tracking web application. It runs as **three services**:

1. **KinaUnaWeb** - ASP.NET Core MVC/Razor front-end (port 5002 locally)
2. **KinaUnaProgenyApi** - REST API back-end (port 5001 locally)
3. **KinaUna.OpenIddict** - OAuth 2.0 / OpenID Connect auth server (port 5000 locally)

**KinaUna.Data** is a shared class library containing models, DbContext classes, DTOs, constants, and extension methods.

### Request flow

```
Browser → KinaUnaWeb (controllers/views) → typed HTTP clients → KinaUnaProgenyApi (API controllers → services → EF Core) → PostgreSQL
                                                                        ↕
                                                              KinaUna.OpenIddict (auth tokens)
```

The web app never accesses the database directly. It communicates with the API through **typed HTTP clients** (e.g., `INotesHttpClient` / `NotesHttpClient`) that acquire Bearer tokens via `ITokenService`.

### Database

PostgreSQL with **three databases** via three DbContexts in `KinaUna.Data/Contexts/`:
- **ProgenyDbContext** - Core data (40+ DbSets: progenies, timeline, contacts, photos, notes, etc.)
- **MediaDbContext** - Photo/video metadata
- **ApplicationDbContext** - Identity/auth and data protection keys

EF Core migrations are in the `KinaUna.OpenIddict` assembly and are applied at startup.

### Service layer patterns

- **API services** (e.g., `INoteService` / `NoteService`): Registered as **Scoped**. Handle data access, distributed caching (`IDistributedCache`), and permission checks via `IAccessManagementService.HasItemPermission()`.
- **Web HTTP clients** (e.g., `INotesHttpClient` / `NotesHttpClient`): Registered with `AddHttpClient<>`. Handle API communication with token management and caching.

### Host configuration

- Web and API projects use the `Startup` class pattern (`Program.cs` → `CreateHostBuilder` → `Startup.cs` with `ConfigureServices`/`Configure`). **Do not change to minimal hosting.**
- `KinaUna.OpenIddict` uses modern minimal hosting (`WebApplication.CreateBuilder`).

### Environment-specific configuration

Configuration keys use suffixes for different environments:
- `AuthServerUrl` (production), `AuthServerUrlLocal` (development), `AuthServerUrlAzure` (staging)
- Resolved in `Startup.cs` based on `IHostEnvironment`

Production config is via environment variables (Coolify); local dev uses `appsettings.Development.json` and User Secrets. See `.env.example` for Docker Compose variables.

## Code Conventions

### C#

- **Primary constructors** for classes with dependency injection (controllers, services)
- Prefer **explicit types** over `var` (e.g., `List<Note> notesList = ...`)
- Use **`[]` collection expressions** for empty lists
- All I/O-bound methods are `async` with `Async` suffix in service interfaces
- Entity copy logic uses extension methods: `CopyPropertiesForUpdate`, `CopyPropertiesForAdd` in `KinaUna.Data.Extensions`
- User helpers: `User.GetUserId()`, `User.GetEmail()`, `Request.GetLanguageIdFromCookie()`
- Use `System.Text.Json` for new code (Newtonsoft.Json is legacy)
- API controllers: `[ApiController]`, `[Authorize]`, `[Route("api/[controller]")]`, inherit `ControllerBase`
- Web controllers: inherit `Controller`, return `View()` with strongly-typed ViewModels

### TypeScript

- Source: `KinaUnaWeb/Scripts/` (organized by feature subdirectory), compiles to `KinaUnaWeb/wwwroot/js/`
- Files use `-v12` version suffix (e.g., `todo-details-v12.ts`) - **maintain this naming**
- Target: ES2020, Module: ES2020, strict mode
- Imports **must use `.js` extensions**: `import { ... } from '../page-models-v12.js'`
- Vanilla DOM manipulation with `document.querySelector` and TypeScript generics
- Named event handlers (not inline anonymous), added/removed with `addEventListener`/`removeEventListener`
- `async/await` with `fetch` for API calls (not XMLHttpRequest)
- Shared models in `page-models-v12.ts` as `export class`
- CSS changes must include `@media (prefers-color-scheme: dark)` variants

### Testing

- xUnit with Moq; in-memory EF Core databases for unit tests
- Test classes: `{ClassName}Tests`; mirror source project structure
- Constructor-based setup (xUnit convention); `ClaimsPrincipal` with OpenIddict claims for controller tests

## Key Domain Concepts

- **Progeny** - A child being tracked; central entity that most data relates to
- **Family** - A group of users sharing access to progenies
- **UserInfo** - Extended user profile with preferences and family memberships
- **TimeLineItem** - Links to content types (notes, photos, events, etc.) via `KinaUnaTypes.TimeLineType`
- **Permissions** - Granular access control at progeny, family, and item levels; all data access must verify permissions via `IAccessManagementService`

## Important Guidelines

1. This project uses **Razor views with controllers** - do not suggest Blazor components or minimal API patterns
2. All data access must go through the **access management service** for permission checks - never bypass permission logic
3. Services must include **distributed cache handling** when adding new data access
4. Follow existing **DI lifetime patterns**: Web services Transient, API services Scoped, HTTP clients via `AddHttpClient<>`
5. Models in `KinaUna.Data.Models`, DTOs in `KinaUna.Data.Models.DTOs`, ViewModels in `KinaUnaWeb.Models`
6. Constants in `KinaUna.Data.Constants`, `KinaUna.Data.AuthConstants`, `KinaUna.Data.PageNames`
