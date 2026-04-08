# MainSpring Port — Idea Document

> **Status:** Draft — decisions captured, ready for detailed specs  
> **Target Project:** `MainSpringTwo.Web` (.NET 10, ASP.NET Core MVC)  
> **Source Projects:** `MainSpring.Agent`, `MainSpring.Interfaces`, `MainSpring.Web`

---

## 1. Background

The original MainSpring solution is a .NET Framework 4.6.1 application composed of three projects:

| Project | Role | Key Technologies |
|---|---|---|
| **MainSpring.Web** | ASP.NET MVC 5 web dashboard | Entity Framework 6, GridMvc, System.Web.Mvc |
| **MainSpring.Agent** | Windows Service that executes scheduled plugin tasks | ServiceBase, Serilog, EF 6 |
| **MainSpring.Interfaces** | Shared plugin contract library (`IPlugin`, `PluginParameter`, `PluginLog`) | .NET Framework class library |

### Existing Domain Entities (via `MainSpring.Data.Models`)

- **Host** — machines that run the agent
- **ScheduledJob** — jobs with scheduling metadata
- **Plugin** — loadable task plugins with configuration parameters
- **ApplicationConfiguration** / **ConfigurationValue** — app-level and per-plugin settings
- **JobHistory** — execution audit trail

### Existing Web Controllers

`HomeController`, `HostController`, `ScheduledJobController`, `PluginController`, `PluginConfigurationController`, `ApplicationConfigurationController`, `ConfigurationValueController`, `JobHistoryController`, `ApiHostController`

---

## 2. Vision

Port the MainSpring solution into a **modern, fully responsive web application** built on:

- **ASP.NET Core MVC** (.NET 10) — replacing ASP.NET MVC 5 / System.Web
- **Tailwind CSS 4** — utility-first styling with built-in dark mode support
- **Alpine.js** — lightweight client-side interactivity (dropdowns, modals, toggles, etc.)

The result should be a clean, single-project web application (`MainSpringTwo.Web`) that can manage hosts, plugins, scheduled jobs, configuration, and job history through a responsive UI that supports **light/dark mode switching**.

---

## 3. High-Level Goals

- [ ] Replace .NET Framework 4.6.1 with .NET 10 and ASP.NET Core MVC
- [ ] Replace Entity Framework 6 with EF Core + **SQLite** (fresh schema — no legacy data migration)
- [ ] Replace legacy GridMvc grids with Tailwind-styled HTML tables
- [ ] Replace the Windows Service agent with **Hangfire** embedded in the web app
- [ ] Modernize the `IPlugin` contract to a **compiled/registered plugin model** targeting .NET 10
- [ ] Implement a fully responsive layout with Tailwind CSS 4
- [ ] Add dark mode toggle (persisted via `localStorage` or user preference)
- [ ] Use Alpine.js for interactive UI elements instead of jQuery
- [ ] Add **ASP.NET Core Identity** for authentication
- [ ] Implement **"Run Now"** capability for on-demand job execution
- [ ] Implement **aggressive log pruning** based on a configurable retention threshold

---

## 4. Front-End Architecture

### 4.1 Tailwind CSS 4

- Install via **npm** using the Tailwind CLI directly (`@tailwindcss/cli`) — no PostCSS layer needed
- Tailwind CSS 4 uses a CSS-first configuration model — no `tailwind.config.js` required
- Import Tailwind via `@import "tailwindcss"` in the main CSS input file (`wwwroot/css/app.css`)
- Configure `darkMode: 'class'` strategy so dark mode is toggled by a CSS class on `<html>`
- Define custom design tokens (colors, spacing) using `@theme` directives directly in CSS
- Add an npm script and wire it into the .csproj build via an MSBuild `<Exec>` target:
  ```xml
  <Target Name="BuildCSS" BeforeTargets="Build">
    <Exec Command="npm run css:build" WorkingDirectory="$(ProjectDir)" />
  </Target>
  ```
- `package.json` script: `"css:build": "npx @tailwindcss/cli -i wwwroot/css/app.css -o wwwroot/css/app.min.css --minify"`

### 4.2 Alpine.js

- Include via CDN or npm
- Use for: dark mode toggle (`x-data`, `x-on:click`), mobile nav drawer, confirmation modals, inline editing, toast notifications
- Keep Alpine scoped to UI behavior — no client-side routing or heavy state management

### 4.3 Dark Mode

- Toggle button in the top nav (sun/moon icon)
- Persist preference in `localStorage`; respect `prefers-color-scheme` as default
- All Tailwind classes use `dark:` variant for color overrides

### 4.4 Responsive Layout

- Mobile-first grid using Tailwind breakpoints (`sm`, `md`, `lg`, `xl`)
- Collapsible sidebar navigation on smaller screens (Alpine-driven)
- Data tables scroll horizontally on mobile or collapse to card layout

---

## 5. Back-End Architecture

### 5.1 Project Structure (proposed)

```
MainSpringTwo.Web/
├── Controllers/          # MVC controllers (ported from MainSpring.Web)
├── Models/               # Domain entities + view models
├── Data/                 # DbContext, EF Core migrations, seeding
├── Services/             # Business logic, plugin registry
├── Plugins/              # Compiled plugin implementations
├── Jobs/                 # Hangfire job classes (PluginExecutorJob, LogPruningJob)
├── Views/                # Razor views (.cshtml)
│   ├── Shared/           # _Layout, _Partials (nav, dark-mode toggle)
│   └── {Controller}/     # Per-controller views
├── Areas/
│   └── Identity/         # ASP.NET Core Identity scaffolded pages (if customized)
├── wwwroot/
│   ├── css/              # Tailwind input (app.css) + compiled output (app.min.css)
│   └── js/               # Alpine.js, any small scripts
├── Specifications/       # This document and future specs
├── package.json          # npm: @tailwindcss/cli, alpinejs
├── Program.cs
└── MainSpringTwo.Web.csproj
```

### 5.2 Data Access

- **EF Core + SQLite** — fresh schema, no legacy data migration
- Code-first migrations; SQLite database file stored in the app's data directory
- Entities: `Plugin`, `ScheduledJob`, `ConfigurationValue`, `ApplicationConfiguration`, `JobHistory`
- The `Host` entity is **removed** — Hangfire runs in-process, so there is no multi-host topology
- Lightweight service classes over DbContext directly (no repository abstraction layer)

### 5.2.1 Log Pruning

- `JobHistory` records are pruned aggressively based on a configurable retention threshold
- Default retention: **7 days** (stored in `ApplicationConfiguration`)
- Pruning runs as a recurring Hangfire job (e.g., daily) that deletes `JobHistory` rows older than the threshold
- The UI only needs to display up to one week of logs

### 5.3 Hangfire Job Scheduler

- Replace the Windows Service (`MainSpring.Agent`) entirely with **Hangfire** running inside the web app
- Hangfire server is registered in `Program.cs` via `builder.Services.AddHangfire()` / `builder.Services.AddHangfireServer()`
- Use **Hangfire.InMemory** or **Hangfire.SQLite** for storage (keeping everything in one SQLite file is preferred for simplicity)
- A single recurring Hangfire job (`PluginExecutorJob`) runs on a schedule (e.g., every minute) and mirrors the legacy agent behavior:
  1. Query `ScheduledJob` table for jobs whose next run time has passed
  2. Resolve the registered `IPlugin` implementation for each job
  3. Execute the plugin, passing resolved configuration values
  4. Write a `JobHistory` record with the result
- A second recurring job (`LogPruningJob`) runs daily to delete `JobHistory` records older than the retention threshold
- Since only one agent has ever been needed, Hangfire's single-server model is a perfect fit
- Jobs are generally not long-running, so no special queue or concurrency configuration is required

### 5.3.1 Run Now

- Each `ScheduledJob` in the UI will have a **"Run Now"** button
- Clicking it enqueues a one-off Hangfire background job (`BackgroundJob.Enqueue`) that executes the selected plugin immediately
- The job runs through the same `PluginExecutorJob` logic so logging and error handling are consistent
- The UI redirects to the job history view (or shows a toast) confirming the job was queued

### 5.4 Plugin Model (Compiled / Registered)

- **No dynamic assembly loading** — all plugins are compiled into the `MainSpringTwo.Web` project (or a referenced class library)
- Define an `IPlugin` interface in the project:
  ```csharp
  public interface IPlugin
  {
      string Name { get; }
      string Description { get; }
      List<PluginParameter> ConfigurationParameters { get; }
      Task<PluginResult> RunAsync(Dictionary<string, string> configuration, CancellationToken cancellationToken);
  }
  ```
- Each plugin is registered in DI as a keyed/named service or via a `PluginRegistry` dictionary
- `PluginParameter`, `PluginLog`, `PluginResult`, and `ParameterDataType` are simple POCOs in the `Models` folder
- Configuration values are resolved from the database and passed into `RunAsync` — plugins have no direct knowledge of `DbContext` or `IConfiguration`
- Adding a new plugin = write a class implementing `IPlugin`, register it, recompile, deploy

### 5.5 Authentication

- **ASP.NET Core Identity** with the default UI (scaffolded Identity Razor pages)
- SQLite as the Identity store (same database file or a separate one — TBD)
- Protect all controllers with `[Authorize]`; the Hangfire dashboard is also gated behind authentication
- Seed a default admin account on first run via `DbContext` seeding

---

## 6. Decisions Log

| # | Question | Decision |
|---|---|---|
| 1 | **Database** | **SQLite** — fresh schema via EF Core migrations. No legacy data migration. |
| 2 | **Authentication** | **ASP.NET Core Identity** — default UI, SQLite-backed. |
| 3 | **Agent hosting** | **Hangfire** embedded in the web app. Single server, mirrors legacy agent behavior. |
| 4 | **Plugin loading** | **Compiled/registered model.** No dynamic assembly loading. Recompile to add plugins. |
| 5 | **Real-time updates** | **No SignalR.** Users configure schedules and view up to one week of logs. |
| 6 | **CSS build pipeline** | **npm + Tailwind CLI** (`@tailwindcss/cli`). MSBuild `<Exec>` target runs `npm run css:build` before build. No PostCSS. |
| 7 | **Testing strategy** | **Minimal.** Focus on verifying jobs run at the correctly scheduled time. |

### Remaining Open Questions

1. **Identity database:** Same SQLite file as the app database, or separate? (Leaning toward same file for simplicity.)
2. **Hangfire storage:** Use `Hangfire.InMemory` (simplest, no persistence across restarts) or a SQLite-backed provider? If persistence of the Hangfire queue matters, a SQLite provider is preferred.
3. **Plugin error handling:** Should a failing plugin retry automatically, or just log the failure?
4. **Deployment model:** Single-file publish, Docker container, or plain `dotnet run`?

---

## 7. Implementation Roadmap

### Phase 1 — Foundation

- [ ] Set up npm + Tailwind CLI (`@tailwindcss/cli`) build pipeline
- [ ] Add Alpine.js (via npm)
- [ ] Wire MSBuild target to compile Tailwind CSS on build
- [ ] Create `_Layout.cshtml` with responsive shell, sidebar nav, and dark mode toggle
- [ ] Add EF Core + SQLite with initial migration (entities: `Plugin`, `ScheduledJob`, `ConfigurationValue`, `ApplicationConfiguration`, `JobHistory`)
- [ ] Scaffold ASP.NET Core Identity (register, login, logout)
- [ ] Seed default admin account

### Phase 2 — Hangfire + Plugin Infrastructure

- [ ] Add Hangfire with SQLite or in-memory storage
- [ ] Define `IPlugin` interface and supporting types (`PluginParameter`, `PluginResult`, `ParameterDataType`)
- [ ] Implement `PluginRegistry` for compiled plugin registration in DI
- [ ] Implement `PluginExecutorJob` — recurring Hangfire job that checks schedules and runs plugins
- [ ] Implement `LogPruningJob` — recurring Hangfire job that deletes old `JobHistory` records
- [ ] Add **"Run Now"** endpoint that enqueues a one-off Hangfire job for a given `ScheduledJob`

### Phase 3 — Controllers & Views

- [ ] `HomeController` — dashboard with summary stats
- [ ] `ScheduledJobController` — CRUD for scheduled jobs + Run Now button
- [ ] `PluginController` — list registered plugins, view configuration parameters
- [ ] `ConfigurationValueController` — manage per-job configuration values
- [ ] `ApplicationConfigurationController` — app-level settings (including log retention threshold)
- [ ] `JobHistoryController` — paginated log view (last 7 days)

### Phase 4 — Polish & Testing

- [ ] Responsive table/card layouts for all list views
- [ ] Toast notifications for actions (Alpine.js)
- [ ] Write tests to verify schedule-based job execution timing
- [ ] Review Hangfire dashboard access (auth-gated)
- [ ] Final dark mode pass across all views

---

*This is a living document. Update it as decisions are made.*
