# KinaUnaAzure

KinaUna is a family-oriented child-tracking web application that allows families to record and share timeline events, photos, videos, notes, contacts, locations, measurements, sleep data, vaccinations, skills, vocabulary, and more for their children ("progenies"). It is deployed on a VPS Linux server using [Coolify](https://coolify.io/) with Docker containers.

## Solution Structure

The solution contains four application projects and three test projects:

| Project | Type | Description |
|---|---|---|
| **KinaUnaWeb** | ASP.NET Core Razor Pages / MVC web app | Front-end web application with controllers, Razor views, and TypeScript client-side code |
| **KinaUnaProgenyApi** | ASP.NET Core Web API | Back-end REST API for progeny data, accessed by the web app via typed HTTP clients |
| **KinaUna.Data** | Class library | Shared models, DbContext classes, extension methods, constants, and DTOs |
| **KinaUna.OpenIddict** | ASP.NET Core web app | Authentication/authorization server using OpenIddict (OAuth 2.0 / OpenID Connect) |
| **KinaUnaWeb.Tests** | xUnit test project | Unit tests for the web application (uses Moq) |
| **KinaUnaProgenyApi.Tests** | xUnit test project | Unit tests for the API |
| **KinaUna.OpenIddict.Tests** | xUnit test project | Unit tests for the auth server |

## Technology Stack

| Area | Technology |
|---|---|
| **Target Framework** | .NET 10 (`net10.0`) for all projects |
| **Web Framework** | ASP.NET Core with Razor views and controllers |
| **ORM** | Entity Framework Core with SQL Server |
| **Authentication** | OpenIddict (OAuth 2.0 / OpenID Connect), Duende IdentityModel |
| **Serialization** | `System.Text.Json` (preferred); `Newtonsoft.Json` in legacy areas |
| **Caching** | `IDistributedCache` (distributed memory cache) and `IMemoryCache` |
| **Real-time** | ASP.NET Core SignalR |
| **Image Processing** | Magick.NET (`Magick.NET-Q8-AnyCPU`) |
| **Hosting** | VPS Linux server with Coolify, Docker containers, reverse proxy (Traefik/Caddy) |
| **Storage** | Azure Blob Storage (or compatible S3/Azurite alternative) |
| **Monitoring** | Application Insights (optional) |
| **Push Notifications** | Azure Notification Hubs (optional), VAPID web push |
| **Client-side** | TypeScript (ES2020, strict mode), vanilla DOM manipulation, jQuery (legacy) |
| **CSS** | Custom `site.css` with light/dark theme support via `prefers-color-scheme` media queries |
| **Testing** | xUnit, Moq |

## Architecture Overview

### Deployment

The application runs as **three Docker containers** deployed via [Coolify](https://coolify.io/) on a VPS Linux server:

1. **KinaUnaWeb** – the front-end web application
2. **KinaUnaProgenyApi** – the back-end REST API
3. **KinaUna.OpenIddict** – the authentication/authorization server

Each service has its own Dockerfile (`Dockerfile.auth`, `Dockerfile.api`, `Dockerfile.web`). A `docker-compose.yml` is provided for local development. In production, Coolify manages container orchestration and HTTPS termination via its built-in reverse proxy.

Data is stored in **SQL Server** (three databases: one for identity/auth, one for progeny data, one for media data). **Azure Blob Storage** (or a compatible alternative) is used for images and file uploads.

Configuration values (connection strings, client secrets, service URLs) are injected as **environment variables** — set in Coolify for production or in a `.env` file for local Docker Compose development. See `.env.example` for all required variables.

### Host Configuration

The web and API projects use the `Startup` class pattern (`Program.cs` → `CreateHostBuilder` → `webBuilder.UseStartup<Startup>()`). The OpenIddict project uses `WebApplication.CreateBuilder`. Azure Key Vault support exists in the codebase but is no longer the primary configuration source; environment variables are used instead.

### Data Access

Three `DbContext` classes in `KinaUna.Data` handle data access: `ProgenyDbContext`, `MediaDbContext`, and `ApplicationDbContext`. EF Core migrations are in the `KinaUna.OpenIddict` assembly.

### Service Layer

- **API:** Each domain area has a service interface and implementation (e.g., `INoteService` / `NoteService`), registered as **Scoped**. Services handle distributed caching and permission checks via `IAccessManagementService`.
- **Web:** The web app communicates with the API through typed HTTP clients (e.g., `INotesHttpClient` / `NotesHttpClient`), registered with `AddHttpClient<>` as **Transient**. HTTP clients use `ITokenService` for authentication tokens.

### Client-side

TypeScript source files are in `KinaUnaWeb/Scripts/`, organized by feature subdirectory. Files use a `-v12` version suffix (e.g., `todo-details-v12.ts`). TypeScript compiles to `KinaUnaWeb/wwwroot/js/`.

## Features

### Core Features
- **Family Management** – Add/remove family members (children or any person) and manage access permissions
- **Timeline** – View all content in chronological order
- **Photos & Videos** – Galleries with tags and comments
- **Notes** – Free-form content
- **Calendar** – Event scheduling
- **Sleep** – Sleep data tracking
- **Skills** – Record when skills are acquired
- **Vocabulary** – Track vocabulary development
- **Measurements** – Height and weight tracking
- **Contacts & Friends** – Contact and relationship management
- **Locations** – Places lived, visited, or of interest
- **Vaccinations** – Vaccination records
- **Todos** – Task management
- **Profile Management** – User profile and preferences

### Administration
- Manage translations and supported languages
- Manage page texts (about page, terms and conditions, privacy, etc.)

### Privacy & Permissions
- Users control all access to their data
- All data access verifies the current user's permissions
- Granular access control at progeny, family, and item levels
- Personal data is never visible to unauthorized users

### Multilingual Support
- Built-in localization system (`KinaUnaText` / `TextTranslation`) for UI strings
- Integer-based language identifier stored in a cookie

## Services and Configuration

Configuration values are provided via environment variables in production (set in Coolify) or via `appsettings.json`, `appsettings.Development.json`, and User Secrets for local development. Azure Key Vault support remains in the codebase for legacy/alternative deployments.

### Infrastructure

- **VPS Linux Server** – Hosts all three Docker containers via Coolify
- **Coolify** – Container orchestration, reverse proxy (HTTPS termination), environment variable management
- **SQL Server** – Three databases (Identity, Progeny, Media)
- **Azure Blob Storage** – Blob storage for images and files (or compatible S3/Azurite alternative)
- **Azure Notification Hubs** – Push messaging (optional)
- **Application Insights** – Logging and analytics (optional)

### External Services
- **Email** – Required for account confirmation and password reset emails
- **Here Maps** – Map display for locations
- **VAPID keys** – Web push notifications
- **Login providers** (optional) – Apple, Google, Microsoft, etc. require credentials from each provider

### Local Development with Docker Compose

1. Copy `.env.example` to `.env` and fill in the required values
2. Generate dev certificates (see `.env.example` for instructions)
3. Place PFX files in the `./certs/` directory
4. Run: `docker compose up --build`
5. Set `RESET_OPENIDDICT_DATABASE=true` on first run to seed the OpenIddict database
