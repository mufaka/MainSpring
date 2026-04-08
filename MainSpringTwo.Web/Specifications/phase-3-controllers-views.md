# Phase 3 — Controllers & Views

> **Parent document:** [`mainspring-port-plan.md`](mainspring-port-plan.md)

All controllers use constructor-injected `AppDbContext`. All actions are protected by the global `[Authorize]` filter from Phase 1.

---

## 3.1 Notification Toast System (shared infrastructure)

Before building views, set up a lightweight toast notification system to replace the legacy `BaseViewModel` / `NotificationMessage` pattern.

**Files to create:**

- `Views/Shared/_Toast.cshtml` — an Alpine.js-driven toast partial:
  ```html
  <div x-data="{ show: @(TempData["Toast"] != null ? "true" : "false"),
                  message: '@TempData["Toast"]' }"
       x-show="show" x-transition
       x-init="setTimeout(() => show = false, 4000)"
       class="fixed bottom-4 right-4 ...">
    <span x-text="message"></span>
  </div>
  ```

Include `_Toast` in `_Layout.cshtml`. Controllers set `TempData["Toast"]` for success messages. Errors are shown inline on forms.

---

## 3.2 HomeController — Dashboard

**Legacy:** Bare `Index()` returning an empty view with `ViewBag.Title = "Dashboard"`.

**New implementation:**

- `Controllers/HomeController.cs` — `Index()` queries summary stats:
  - Total active scheduled jobs
  - Total plugins registered
  - Recent job history (last 24 hours): success count, error count
  - Next upcoming job (nearest `StartTime` alignment)

- `Views/Home/Index.cshtml` — Tailwind card grid:
  - Stat cards (jobs, plugins, recent successes, recent errors)
  - Small table of the last 10 `JobHistory` entries
  - "Run Now" quick-action buttons for active jobs

**Acceptance criteria:**

- [ ] Dashboard loads with live stats from the database
- [ ] Responsive card layout adapts to mobile/desktop

---

## 3.3 ScheduledJobController — CRUD + Run Now

**Legacy:** CRUD with `Host`, `Plugin`, `ScheduleType` select lists; `Bind` attribute on create/edit.

**New implementation:**

- `Controllers/ScheduledJobController.cs` — standard CRUD:
  - `Index()` — list all jobs with status badge (active/inactive), plugin name, schedule summary
  - `Details(int id)` — read-only view
  - `Create()` / `Create(ScheduledJobViewModel)` — form with plugin dropdown (populated from `PluginRegistry`), schedule type enum dropdown, interval, start time, active toggle
  - `Edit(int id)` / `Edit(ScheduledJobViewModel)` — same form + inline configuration value editing (replaces legacy `ConfigurationValueController`)
  - `Delete(int id)` / `DeleteConfirmed(int id)` — with Alpine.js confirmation modal
  - `RunNow(int id)` — from Phase 2.6

- `Models/ViewModels/ScheduledJobViewModel.cs`:
  ```csharp
  public class ScheduledJobViewModel
  {
      public ScheduledJob ScheduledJob { get; set; }
      public List<PluginParameter> PluginParameters { get; set; } // from IPlugin
      public List<ConfigurationValue> ConfigurationValues { get; set; } // from DB
      public SelectList PluginOptions { get; set; }
  }
  ```

**Configuration value editing (folded into ScheduledJob Edit):**

The legacy `ConfigurationValueController` and `PluginConfigurationController` are **not ported as separate controllers**. Instead, the ScheduledJob Edit view:
1. Looks up the selected plugin's `ConfigurationParameters` from the registry
2. Displays a form field for each parameter (type-aware: text, number, checkbox, password, textarea based on `ParameterDataType`)
3. Saves `ConfigurationValue` rows on form submit alongside the `ScheduledJob` update

**Acceptance criteria:**

- [ ] Full CRUD for scheduled jobs works
- [ ] Plugin dropdown is populated from `PluginRegistry.GetAll()`
- [ ] Configuration values are editable inline on the Edit page
- [ ] "Run Now" button enqueues the job and shows a toast

---

## 3.4 PluginController — Read-Only List

**Legacy:** Full CRUD with file upload (plugin ZIP packages).

**New implementation (read-only — plugins are compiled):**

- `Controllers/PluginController.cs`:
  - `Index()` — list all registered plugins (from `PluginRegistry`) joined with `Plugin` entity for metadata
  - `Details(string name)` — show plugin name, description, configuration parameters list

- `Views/Plugin/Index.cshtml` — Tailwind table with columns: Name, Description, # Parameters, Status
- `Views/Plugin/Details.cshtml` — parameter table (Name, Description, Data Type)

No Create/Edit/Delete — plugins are managed by recompiling and deploying.

**Acceptance criteria:**

- [ ] Plugin list shows all registered plugins
- [ ] Details page shows configuration parameter metadata

---

## 3.5 ApplicationConfigurationController — CRUD

**Legacy:** Standard CRUD with `CategoryName`, `ConfigurationName`, `ConfigurationValue`.

**New implementation:**

- `Controllers/ApplicationConfigurationController.cs` — standard CRUD, largely unchanged in shape
  - `Index()` — grouped by `CategoryName`
  - `Create()` / `Edit(int id)` / `Delete(int id)`

- `Views/ApplicationConfiguration/Index.cshtml` — grouped table or accordion by category
- Seed the `Logging / RetentionDays = 7` row (from Phase 2.5)

**Acceptance criteria:**

- [ ] CRUD works for application configuration entries
- [ ] The `Logging / RetentionDays` setting is editable and respected by `LogPruningJob`

---

## 3.6 JobHistoryController — Paginated Log View

**Legacy:** `Index()` loaded last 200 records with GridMvc.

**New implementation:**

- `Controllers/JobHistoryController.cs`:
  - `Index(int page = 1, int? scheduledJobId = null)` — paginated, filterable by job, last 7 days only
  - `Details(int id)` — full log entry with detail text

- `Views/JobHistory/Index.cshtml`:
  - Filter dropdown (by scheduled job)
  - Tailwind table with columns: Run Time, Job Name, Status (success/error badge), Message
  - Pagination controls
  - Error rows highlighted with `bg-red-50 dark:bg-red-900/20`

- `Models/ViewModels/JobHistoryIndexViewModel.cs`:
  ```csharp
  public class JobHistoryIndexViewModel
  {
      public List<JobHistory> Entries { get; set; }
      public int Page { get; set; }
      public int TotalPages { get; set; }
      public int? FilterScheduledJobId { get; set; }
      public SelectList ScheduledJobOptions { get; set; }
  }
  ```

**Acceptance criteria:**

- [ ] Pagination works (e.g., 25 per page)
- [ ] Filtering by scheduled job works
- [ ] Only records from the last 7 days are shown

---

## 3.7 Controllers Dropped from Legacy

The following legacy controllers are **not ported**:

| Controller | Reason |
|---|---|
| `HostController` | No multi-host model — Hangfire runs in-process |
| `ApiHostController` | Same — the agent no longer registers hosts via API |
| `PluginConfigurationController` | Folded into ScheduledJob Edit view (configuration value editing) |
| `ConfigurationValueController` | Folded into ScheduledJob Edit view |
