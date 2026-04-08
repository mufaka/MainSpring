# Phase 1 — Foundation

> **Parent document:** [`mainspring-port-plan.md`](mainspring-port-plan.md)

---

## 1.1 npm + Tailwind CSS 4 Build Pipeline

**Goal:** Compile Tailwind CSS from a source file on every build.

**Files to create:**

- `MainSpringTwo.Web/package.json`
  ```json
  {
    "private": true,
    "scripts": {
      "css:build": "npx @tailwindcss/cli -i wwwroot/css/app.css -o wwwroot/css/app.min.css --minify",
      "css:watch": "npx @tailwindcss/cli -i wwwroot/css/app.css -o wwwroot/css/app.min.css --watch"
    },
    "devDependencies": {
      "@tailwindcss/cli": "^4",
      "alpinejs": "^3"
    }
  }
  ```
- `MainSpringTwo.Web/wwwroot/css/app.css` — Tailwind entry point:
  ```css
  @import "tailwindcss";

  @custom-variant dark (&:where(.dark, .dark *));

  @theme {
    /* extend with project tokens later */
  }
  ```

**Files to modify:**

- `MainSpringTwo.Web/MainSpringTwo.Web.csproj` — add MSBuild targets:
  ```xml
  <Target Name="NpmInstall" BeforeTargets="BuildCSS" Inputs="package.json" Outputs="node_modules/.package-lock.json">
    <Exec Command="npm install" WorkingDirectory="$(ProjectDir)" />
  </Target>

  <Target Name="BuildCSS" BeforeTargets="Build">
    <Exec Command="npm run css:build" WorkingDirectory="$(ProjectDir)" />
  </Target>
  ```
  Add `node_modules` to a `.gitignore` (or the existing one).

**Acceptance criteria:**

- [ ] `dotnet build` produces `wwwroot/css/app.min.css` containing compiled Tailwind output
- [ ] `npm run css:watch` rebuilds on file changes during development

---

## 1.2 Alpine.js Setup

**Goal:** Make Alpine.js available to all views.

Alpine is installed as an npm dependency in step 1.1. During build, copy it to `wwwroot/js/`:

- Option A: Add an npm copy script (`"js:build": "cp node_modules/alpinejs/dist/cdn.min.js wwwroot/js/alpine.min.js"`) and wire it into the MSBuild target.
- Option B: Reference the Alpine CDN in `_Layout.cshtml` (`https://cdn.jsdelivr.net/npm/alpinejs@3/dist/cdn.min.js`) with `defer`. This avoids a build step and is fine since Alpine is a small, stable dependency.

**Decision:** Use Option B (CDN) for simplicity. The `<script>` tag goes in `_Layout.cshtml` just before `</body>`.

**Acceptance criteria:**

- [ ] `Alpine` is available globally; an `x-data` directive on a test element renders correctly

---

## 1.3 Layout Shell (`_Layout.cshtml`)

**Goal:** Create the shared responsive layout with sidebar nav and dark mode toggle.

**Files to create:**

- `Views/Shared/_Layout.cshtml`
- `Views/_ViewStart.cshtml` (sets `Layout = "_Layout"`)
- `Views/_ViewImports.cshtml` (sets `@using MainSpringTwo.Web` and tag helpers)

**Layout structure (Tailwind + Alpine):**

```
<html class="..." x-data="{ darkMode: localStorage.getItem('dark') === 'true' }"
      x-init="$watch('darkMode', v => { localStorage.setItem('dark', v); ... })"
      :class="{ 'dark': darkMode }">
<head>
  <link href="~/css/app.min.css" rel="stylesheet" />
</head>
<body class="bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
  <!-- Mobile top bar with hamburger -->
  <!-- Sidebar nav (Alpine-toggled on mobile, always visible on lg+) -->
  <!-- Main content area: @RenderBody() -->
  <!-- Alpine.js CDN script -->
</body>
</html>
```

**Navigation items (based on legacy controllers):**

| Label | Route | Icon idea |
|---|---|---|
| Dashboard | `/` | home |
| Scheduled Jobs | `/ScheduledJob` | clock |
| Plugins | `/Plugin` | puzzle |
| Configuration | `/ApplicationConfiguration` | cog |
| Job History | `/JobHistory` | list |

The `HostController` and `ApiHostController` are **dropped** (no multi-host model). `PluginConfigurationController` and `ConfigurationValueController` functionality will be folded into the `ScheduledJob` edit view (inline configuration editing).

**Dark mode toggle:** A button in the sidebar footer or top bar with sun/moon SVG icons, wired to `darkMode` Alpine state.

**Acceptance criteria:**

- [ ] Layout renders a responsive sidebar that collapses to a hamburger on small screens
- [ ] Dark mode toggle switches classes on `<html>` and persists across page loads
- [ ] All navigation links route to placeholder views

---

## 1.4 EF Core + SQLite Data Model

**Goal:** Define the domain entities and create the initial migration.

**NuGet packages to add:**

- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.Tools` (for migrations)

**Files to create:**

- `Data/AppDbContext.cs` — inherits `IdentityDbContext` (so Identity tables live in the same database)
- `Models/Entities/Plugin.cs`
- `Models/Entities/ScheduledJob.cs`
- `Models/Entities/ConfigurationValue.cs`
- `Models/Entities/ApplicationConfiguration.cs`
- `Models/Entities/JobHistory.cs`
- `Models/Entities/ScheduleType.cs` (enum or lookup — see below)

**Entity design (mapped from legacy):**

### `Plugin`
| Column | Type | Notes |
|---|---|---|
| PluginId | int (PK, auto) | |
| Name | string | Unique. Matches `IPlugin.Name` for registry lookup. |
| Description | string | |
| IsActive | bool | |
| InsertDate | DateTime | |
| UpdateDate | DateTime | |

Legacy `Plugin` stored `AssemblyName`, `ClassName`, `Version`, and a `Package` (byte[] ZIP). All of that is **dropped** — plugins are compiled and registered by name.

### `ScheduledJob`
| Column | Type | Notes |
|---|---|---|
| ScheduledJobId | int (PK, auto) | |
| Name | string | |
| PluginId | int (FK → Plugin) | |
| ScheduleType | enum (Minute, Hour, Day, Week, Month) | Stored as int. Replaces legacy `ScheduleType` FK to lookup table. |
| Interval | int | e.g., "every 2 hours" = ScheduleType.Hour, Interval 2 |
| StartTime | DateTime | Anchor time for interval calculation |
| IsActive | bool | |
| InsertDate | DateTime | |
| UpdateDate | DateTime | |

Legacy `HostId` FK is **dropped**.

### `ConfigurationValue`
| Column | Type | Notes |
|---|---|---|
| ConfigurationValueId | int (PK, auto) | |
| ScheduledJobId | int (FK → ScheduledJob) | |
| ParameterName | string | Matches the plugin's `PluginParameter.Name` |
| Value | string | |
| InsertDate | DateTime | |
| UpdateDate | DateTime | |

Legacy had a FK to `PluginConfiguration` (a separate table). This is simplified: the parameter metadata comes from the compiled `IPlugin.ConfigurationParameters` and the stored value is matched by `ParameterName`.

### `ApplicationConfiguration`
| Column | Type | Notes |
|---|---|---|
| ApplicationConfigurationId | int (PK, auto) | |
| CategoryName | string | |
| ConfigurationName | string | |
| ConfigurationValue | string | |
| InsertDate | DateTime | |
| UpdateDate | DateTime | |

Unchanged from legacy except audit columns are simplified.

### `JobHistory`
| Column | Type | Notes |
|---|---|---|
| JobHistoryId | int (PK, auto) | |
| ScheduledJobId | int (FK → ScheduledJob) | |
| RunTime | DateTime | Indexed for pruning queries |
| IsError | bool | Replaces legacy FK to `ScheduledJobStatus` lookup |
| Message | string | |
| Detail | string (nullable) | |

Legacy used a `ScheduledJobStatusId` FK to a status lookup table. Simplified to a boolean `IsError`.

**`ScheduleType` as an enum:**

```csharp
public enum ScheduleType
{
    Minute = 1,
    Hour = 2,
    Day = 3,
    Week = 4,
    Month = 5
}
```

This replaces the legacy `ScheduleType` database lookup table. EF Core stores it as an `int`.

**Connection string:** Configure in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=mainspring.db"
  }
}
```

**Files to modify:**

- `Program.cs` — register `AppDbContext` with SQLite provider

**Commands to run:**

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Acceptance criteria:**

- [ ] `dotnet ef database update` creates `mainspring.db` with all tables + Identity tables
- [ ] `AppDbContext` resolves from DI and can read/write all entities

---

## 1.5 ASP.NET Core Identity

**Goal:** Protect the app with login/registration and seed an admin account.

**NuGet packages to add:**

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.AspNetCore.Identity.UI`

**Files to create / modify:**

- `Data/AppDbContext.cs` — already inherits `IdentityDbContext` (from step 1.4)
- `Data/SeedData.cs` — static method to seed a default admin user on first run
- `Program.cs` — register Identity services, call seed, add `app.UseAuthentication()` before `app.UseAuthorization()`

**Seed logic (`SeedData.cs`):**

```csharp
public static async Task SeedAsync(IServiceProvider services)
{
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    if (!userManager.Users.Any())
    {
        var admin = new IdentityUser { UserName = "admin", Email = "admin@localhost" };
        await userManager.CreateAsync(admin, "ChangeMe123!");
    }
}
```

Called from `Program.cs` after `app` is built, using a scope.

**Controller protection:**

- Add `[Authorize]` globally via `builder.Services.AddControllersWithViews(options => options.Filters.Add(new AuthorizeFilter()))` or a base controller attribute. The Identity UI login/register pages are excluded automatically.

**Acceptance criteria:**

- [ ] Unauthenticated requests redirect to `/Identity/Account/Login`
- [ ] Seeded admin account (`admin` / `ChangeMe123!`) can log in on first run
- [ ] After login, all controllers are accessible
