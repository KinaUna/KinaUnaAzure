# KinaUnaAzure

KinaUna is a family-oriented child-tracking web application that allows families to record and share timeline events, photos, videos, notes, contacts, locations, measurements, sleep data, vaccinations, skills, vocabulary, and more for their children ("progenies"). It is hosted on Azure.

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
| **Cloud** | Azure App Service, Azure Blob Storage, Azure Key Vault, Application Insights, Azure Notification Hubs |
| **Client-side** | TypeScript (ES2020, strict mode), vanilla DOM manipulation, jQuery (legacy) |
| **CSS** | Custom `site.css` with light/dark theme support via `prefers-color-scheme` media queries |
| **Testing** | xUnit, Moq |

## Architecture Overview

### Deployment

The application is designed for deployment to **Azure App Service** as three separate web apps:

1. **KinaUnaWeb** – the front-end web application
2. **KinaUnaProgenyApi** – the back-end REST API
3. **KinaUna.OpenIddict** – the authentication/authorization server

Data is stored in **SQL Server** (three databases: one for identity/auth, one for progeny data, one for media data). **Azure Blob Storage** is used for images and file uploads.

### Host Configuration

All projects use the `Startup` class pattern (`Program.cs` → `CreateHostBuilder` → `webBuilder.UseStartup<Startup>()`). Azure Key Vault provides configuration values in production.

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

Configuration values are stored in `appsettings.json`, `appsettings.Development.json`, User Secrets, and Azure Key Vault.

### Azure Resources
- **Azure App Service** – Three web app instances (Web, API, Auth)
- **Azure Storage Account** – Blob storage for images and files
- **Azure SQL Server** – Three databases (Identity, Progeny, Media)
- **Azure Key Vault** – Shared secrets across web apps
- **Azure Notification Hubs** – Push messaging
- **Application Insights** – Logging and analytics

### External Services
- **Email** – Required for account confirmation and password reset emails
- **Here Maps** – Map display for locations
- **VAPID keys** – Web push notifications
- **Login providers** (optional) – Apple, Google, Microsoft, etc. require credentials from each provider
