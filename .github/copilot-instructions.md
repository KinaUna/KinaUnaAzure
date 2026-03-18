# KinaUna – Copilot Instructions

## Project Overview

KinaUna is a family-oriented child-tracking web application deployed on a VPS Linux server using Coolify. It allows families to record and share timeline events, photos, videos, notes, contacts, locations, measurements, sleep data, vaccinations, skills, vocabulary, and more for their children ("progenies").

## Solution Structure

| Project | Type | Description |
|---|---|---|
| `KinaUnaWeb` | ASP.NET Core Razor Pages / MVC web app | Front-end web application with controllers, Razor views, and TypeScript client-side code |
| `KinaUnaProgenyApi` | ASP.NET Core Web API | Back-end REST API for progeny data, accessed by the web app via HTTP clients |
| `KinaUna.Data` | Class library | Shared models, DbContext classes, extension methods, constants, and DTOs |
| `KinaUna.OpenIddict` | ASP.NET Core web app | Authentication/authorization server using OpenIddict |
| `KinaUnaWeb.Tests` | xUnit test project | Unit tests for the web application (uses Moq) |
| `KinaUnaProgenyApi.Tests` | xUnit test project | Unit tests for the API |
| `KinaUna.OpenIddict.Tests` | xUnit test project | Unit tests for the auth server |

## Technology Stack

- **Target Framework:** .NET 10 (`net10.0`) for all projects
- **Web Framework:** ASP.NET Core with Razor views (not Blazor, not minimal APIs)
- **ORM:** Entity Framework Core with SQL Server
- **Authentication:** OpenIddict (OAuth 2.0 / OpenID Connect), with `Duende.IdentityModel`
- **Serialization:** `System.Text.Json` for new code; `Newtonsoft.Json` exists in some areas via `Microsoft.AspNetCore.Mvc.NewtonsoftJson`
- **Caching:** `IDistributedCache` (distributed memory cache) and `IMemoryCache`
- **Real-time:** ASP.NET Core SignalR (via `WebNotificationHub`)
- **Hosting/Deployment:** VPS Linux server with Coolify, Docker containers (one per service), reverse proxy with HTTPS
- **Storage:** Azure Blob Storage (or compatible S3/Azurite alternative)
- **Monitoring:** Application Insights (optional)
- **Push Notifications:** Azure Notification Hubs (optional), VAPID web push
- **Client-side:** TypeScript (ES2020, strict mode), jQuery (legacy usage), no front-end framework (vanilla DOM manipulation)
- **CSS:** Custom `site.css` with `prefers-color-scheme: dark` media queries (light/dark theme support)
- **Image Processing:** Magick.NET (`Magick.NET-Q8-AnyCPU`)
- **Testing:** xUnit, Moq

## Architecture Patterns

### Host Configuration
- Projects use the `Startup` class pattern (`Program.cs` → `CreateHostBuilder` → `webBuilder.UseStartup<Startup>()`), not minimal hosting / top-level statements.
- Exception: `KinaUna.OpenIddict` uses the minimal hosting pattern (`WebApplication.CreateBuilder`).
- In production, configuration is provided via environment variables set in Coolify (or the `.env` file for local Docker Compose). Azure Key Vault support exists in the codebase but is no longer the primary configuration source.

### Deployment
- The application runs as three Docker containers (auth, api, web) orchestrated by Coolify on a VPS Linux server.
- Each service has its own `Dockerfile` (`Dockerfile.auth`, `Dockerfile.api`, `Dockerfile.web`).
- A `docker-compose.yml` is provided for local development and can be used as a reference for Coolify configuration.
- Configuration values (connection strings, client secrets, URLs) are injected as environment variables.
- HTTPS is terminated by the Coolify reverse proxy (Traefik/Caddy) in production; Kestrel with PFX certificates is used for local Docker Compose development.

### Dependency Injection
- **Web app:** Services registered as `Transient` or `Singleton`; HTTP clients registered with `services.AddHttpClient<IInterface, Implementation>()`.
- **API:** Services registered as `Scoped`; DbContexts are scoped.
- Always use constructor injection with primary constructors (C# 12+ syntax).

### Controller Style
- Controllers use **primary constructors** for dependency injection:
  ```csharp
  public class HomeController(IMediaHttpClient mediaHttpClient, ...) : Controller
  ```
- API controllers inherit `ControllerBase` and use `[ApiController]`, `[Authorize]`, `[Route("api/[controller]")]`.
- Web controllers inherit `Controller` and return `View()` with strongly-typed ViewModels.

### Data Access
- Three `DbContext` classes in `KinaUna.Data`: `ProgenyDbContext`, `MediaDbContext`, `ApplicationDbContext`.
- DbSet properties use the `init` accessor: `public DbSet<Progeny> ProgenyDb { get; init; }`.
- Migrations are in the `KinaUna.OpenIddict` assembly.

### Service Layer (API)
- Each domain area has a service interface and implementation (e.g., `INoteService` / `NoteService`).
- Services handle caching with `IDistributedCache` using `DistributedCacheEntryOptions`.
- Permission checks use `IAccessManagementService.HasItemPermission(...)`.

### HTTP Client Layer (Web)
- The web app communicates with the API through typed HTTP clients (e.g., `INotesHttpClient` / `NotesHttpClient`).
- HTTP clients use `ITokenService` for authentication tokens and `IDistributedCache` for caching.
- Client URIs are resolved from configuration, with separate keys for local/development (`{key}Local`).

### Extension Methods
- Entity copy logic lives in `KinaUna.Data.Extensions` as extension methods (e.g., `CopyPropertiesForUpdate`, `CopyPropertiesForAdd`).
- User-related helpers: `User.GetUserId()`, `User.GetEmail()`, `Request.GetLanguageIdFromCookie()`.

### Models & DTOs
- Domain entities are in `KinaUna.Data.Models`.
- DTOs for API communication are in `KinaUna.Data.Models.DTOs`.
- ViewModels for the web app are in `KinaUnaWeb.Models` with sub-namespaces (e.g., `ItemViewModels`, `HomeViewModels`, `TypeScriptModels`).
- TypeScript model equivalents are in `KinaUnaWeb.Models.TypeScriptModels` (C# classes that correspond to TypeScript interfaces/classes in `page-models-v12.ts`).

### Constants
- Solution-wide constants are in `KinaUna.Data.Constants` (URLs, defaults, app name).
- Authentication constants are in `KinaUna.Data.AuthConstants`.
- Page name constants for translations are in `KinaUna.Data.PageNames`.

## TypeScript Conventions

### File Organization
- Source files are in `KinaUnaWeb/Scripts/` organized by feature subdirectories (e.g., `todos/`, `friends/`, `calendar/`, `contacts/`).
- TypeScript compiles to `KinaUnaWeb/wwwroot/js/` (configured in `tsconfig.json`).
- Files use a `-v12` version suffix (e.g., `todo-details-v12.ts`, `app-v12.ts`).

### TypeScript Configuration
- Target: `ES2020`, Module: `ES2020`, Strict: `true`, `noImplicitAny: true`.
- Imports use `.js` extensions (for ES module compatibility): `import { ... } from '../page-models-v12.js'`.

### Coding Patterns
- Use `document.querySelector` / `document.querySelectorAll` with TypeScript generics for DOM access.
- Named event handler functions (not inline anonymous) that are added/removed: `element.removeEventListener('click', handler); element.addEventListener('click', handler);`.
- `async/await` with `fetch` for API calls; do not use `XMLHttpRequest`.
- Export functions that need to be used by other modules; keep internal helpers as module-private functions.
- Shared models are defined as `export class` in `page-models-v12.ts`, interfaces are used for base types.
- JSDoc-style comments (`/** ... */`) on exported functions.

### Popup/Modal Pattern
- Item details are displayed in popup overlays using `#item-details-div`.
- Show: create overlay → `hideBodyScrollbars()` → `history.pushState(...)` → remove `d-none`.
- Close: clear innerHTML → add `d-none` → `showBodyScrollbars()` → `history.back()`.

## C# Code Style

- **Primary constructors** for classes with dependency injection (controllers, services).
- **XML documentation comments** (`/// <summary>`) on public classes and methods.
- **`var`**: Not widely used; prefer explicit types for clarity (e.g., `List<Note> notesList = ...`).
- **Nullable reference types:** Enabled in `KinaUna.OpenIddict` and test projects. Not explicitly enabled in other projects, but null checks are prevalent.
- **Collection expressions:** Use `[]` syntax for empty lists (e.g., `List<int> allowedProgenies = [];`).
- **Pattern matching:** Used for null checks and type checks.
- **Async/await:** All I/O-bound methods are async. Follow the `Async` suffix convention for method names in service interfaces.

## Testing Conventions

- **Framework:** xUnit with Moq for mocking.
- **Test class naming:** `{ClassName}Tests` (e.g., `TodosControllerTests`).
- **Test organization:** Test files mirror the source project structure (e.g., `Controllers/TodosController/TodosControllerTests.cs`).
- **Setup:** Constructor-based test setup (xUnit convention), no `[SetUp]` attributes.
- **Mocking:** Mock all HTTP clients and services; use `Mock<IInterface>` pattern.
- **Claims setup:** Use `ClaimsPrincipal` with OpenIddict claims (`OpenIddictConstants.Claims.Email`, `OpenIddictConstants.Claims.Subject`) for controller tests.
- **Constants:** Define test constants at the top of test classes (e.g., `TestUserEmail`, `TestUserId`, `TestProgenyId`).

## Key Domain Concepts

- **Progeny:** A child being tracked. Central entity that most data relates to.
- **Family:** A group of users who share access to one or more progenies.
- **UserInfo:** Extended user profile with preferences, linked progenies, and family memberships.
- **TimeLineItem:** A timeline entry that links to various content types (notes, photos, events, etc.) via `KinaUnaTypes.TimeLineType`.
- **Permissions:** Granular access control at progeny, family, and item levels (`ProgenyPermission`, `FamilyPermission`, `TimelineItemPermission`).
- **KinaUnaText / TextTranslation:** Localization system for UI strings, managed via admin pages.
- **LanguageId:** Integer-based language identifier, stored in a cookie named `KinaUnaLanguage`.

## Important Guidelines

1. **Razor Pages over Blazor or MVC:** This project uses Razor views with controllers. Do not suggest Blazor components or minimal API patterns.
2. **Do not change the Startup pattern:** The project uses `Startup.cs` with `ConfigureServices` and `Configure` methods, not minimal hosting.
3. **Keep TypeScript file versioning:** Maintain the `-v12` suffix on TypeScript file names.
4. **Match import style:** TypeScript imports must use `.js` extensions (e.g., `from './page-models-v12.js'`).
5. **Follow existing DI patterns:** Web app services are Transient, API services are Scoped, HTTP clients use `AddHttpClient<>`.
6. **Respect the cache layer:** Services use distributed caching. When adding new data access, include appropriate cache handling.
7. **Maintain permission checks:** All data access must go through the access management service or check permissions. Never bypass permission logic.
8. **Use extension methods for entity operations:** Follow the `CopyPropertiesForUpdate` / `CopyPropertiesForAdd` pattern in `KinaUna.Data.Extensions`.
9. **Use `System.Text.Json`** for new serialization code. Newtonsoft.Json exists but is legacy.
10. **Dark mode support:** CSS changes should include `@media (prefers-color-scheme: dark)` variants where appropriate.
