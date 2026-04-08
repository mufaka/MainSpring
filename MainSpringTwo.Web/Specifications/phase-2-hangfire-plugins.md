# Phase 2 — Hangfire + Plugin Infrastructure

> **Parent document:** [`mainspring-port-plan.md`](mainspring-port-plan.md)

---

## 2.1 Hangfire Setup

**Goal:** Register Hangfire server and dashboard inside the web app.

**NuGet packages to add:**

- `Hangfire.Core`
- `Hangfire.AspNetCore`
- `Hangfire.InMemory` (start with in-memory; switch to a persistent provider later if needed)

**Files to modify:**

- `Program.cs`:
  ```csharp
  builder.Services.AddHangfire(config => config.UseInMemoryStorage());
  builder.Services.AddHangfireServer();

  // After app.UseAuthorization():
  app.MapHangfireDashboard("/hangfire", new DashboardOptions
  {
      Authorization = [new HangfireAuthorizationFilter()]
  });
  ```

**Files to create:**

- `Jobs/HangfireAuthorizationFilter.cs` — implements `IDashboardAuthorizationFilter`, checks `httpContext.User.Identity?.IsAuthenticated`

**Acceptance criteria:**

- [ ] Hangfire server starts with the web app and processes jobs
- [ ] `/hangfire` dashboard is accessible only to authenticated users

---

## 2.2 IPlugin Interface & Supporting Types

**Goal:** Define the plugin contract and supporting POCOs.

**Files to create:**

- `Models/Plugins/IPlugin.cs`:
  ```csharp
  public interface IPlugin
  {
      string Name { get; }
      string Description { get; }
      List<PluginParameter> ConfigurationParameters { get; }
      Task<PluginResult> RunAsync(
          Dictionary<string, string> configuration,
          CancellationToken cancellationToken);
  }
  ```

- `Models/Plugins/PluginParameter.cs`:
  ```csharp
  public class PluginParameter
  {
      public required string Name { get; init; }
      public string Description { get; init; } = "";
      public ParameterDataType DataType { get; init; } = ParameterDataType.String;
  }
  ```

- `Models/Plugins/ParameterDataType.cs`:
  ```csharp
  public enum ParameterDataType
  {
      String = 1,
      Integer = 2,
      Decimal = 3,
      DateTime = 4,
      Password = 5,
      Text = 6,
      Boolean = 7
  }
  ```

- `Models/Plugins/PluginResult.cs`:
  ```csharp
  public class PluginResult
  {
      public bool Success { get; init; }
      public List<PluginLogEntry> Logs { get; init; } = [];
  }
  ```

- `Models/Plugins/PluginLogEntry.cs`:
  ```csharp
  public class PluginLogEntry
  {
      public bool IsError { get; init; }
      public required string Message { get; init; }
      public string? Detail { get; init; }
  }
  ```

**Acceptance criteria:**

- [ ] Types compile and are referenceable from Jobs and Plugins folders

---

## 2.3 Plugin Registry

**Goal:** Provide a way to look up compiled `IPlugin` implementations by name.

**Files to create:**

- `Services/PluginRegistry.cs`:
  ```csharp
  public class PluginRegistry
  {
      private readonly Dictionary<string, IPlugin> _plugins = new(StringComparer.OrdinalIgnoreCase);

      public void Register(IPlugin plugin) => _plugins[plugin.Name] = plugin;
      public IPlugin? GetByName(string name) => _plugins.GetValueOrDefault(name);
      public IReadOnlyCollection<IPlugin> GetAll() => _plugins.Values;
  }
  ```

**Files to modify:**

- `Program.cs` — register `PluginRegistry` as a singleton and register each compiled plugin:
  ```csharp
  var registry = new PluginRegistry();
  // registry.Register(new SamplePlugin());  // add plugins here
  builder.Services.AddSingleton(registry);
  ```

**Files to create (sample plugin for testing):**

- `Plugins/SamplePlugin.cs` — a no-op plugin that logs "Hello from SamplePlugin" so the pipeline can be tested end to end.

**Acceptance criteria:**

- [ ] `PluginRegistry` resolves from DI and returns registered plugins by name
- [ ] `SamplePlugin` is returned from `GetAll()` and `GetByName("Sample")`

---

## 2.4 PluginExecutorJob (Recurring)

**Goal:** A Hangfire recurring job that checks the schedule and runs due plugins.

This is the direct replacement for the legacy `TaskManager.ManageTasks()` loop.

**Files to create:**

- `Jobs/PluginExecutorJob.cs`:
  ```csharp
  public class PluginExecutorJob
  {
      public async Task ExecuteAsync(
          AppDbContext db,
          PluginRegistry registry,
          CancellationToken cancellationToken)
      {
          var now = DateTime.UtcNow;
          var activeJobs = await db.ScheduledJobs
              .Where(j => j.IsActive)
              .Include(j => j.ConfigurationValues)
              .ToListAsync(cancellationToken);

          foreach (var job in activeJobs)
          {
              if (ScheduleHelper.ShouldRun(job, now))
              {
                  await RunJobAsync(job, registry, db, now, cancellationToken);
              }
          }
      }

      // Also used by RunNow:
      public async Task RunSingleJobAsync(
          int scheduledJobId,
          AppDbContext db,
          PluginRegistry registry,
          CancellationToken cancellationToken) { ... }
  }
  ```

- `Services/ScheduleHelper.cs` — pure static method porting the legacy `ShouldRun` logic:

  The legacy algorithm (from `TaskManager.ShouldRun`):
  1. Map `ScheduleType` to a `minutesInPeriod` multiplier (Minute=1, Hour=60, Day=1440, Week=10080).
  2. For `Month`: check if `now.Day/Hour/Minute` matches `StartTime`, with end-of-month fallback.
  3. For all others: `interval = job.Interval * minutesInPeriod`; compute `minutesSinceStart = floor((now - StartTime).TotalMinutes)`; if `minutesSinceStart % interval == 0`, run.

  This logic is ported as-is but cleaned up (use `DateTimeOffset`/UTC, handle edge cases).

**Files to modify:**

- `Program.cs` — register the recurring job:
  ```csharp
  RecurringJob.AddOrUpdate<PluginExecutorJob>(
      "plugin-executor",
      job => job.ExecuteAsync(/* injected */),
      Cron.Minutely);
  ```

**Acceptance criteria:**

- [ ] `ScheduleHelper.ShouldRun` returns `true` for a job configured as "every 5 minutes" when exactly 5 minutes have elapsed since `StartTime`
- [ ] `PluginExecutorJob` writes a `JobHistory` record after running a plugin
- [ ] Errors in a plugin do not crash the job — they are caught and logged as `IsError = true` in `JobHistory`

---

## 2.5 LogPruningJob (Recurring)

**Goal:** Delete `JobHistory` records older than the configurable retention threshold.

**Files to create:**

- `Jobs/LogPruningJob.cs`:
  ```csharp
  public class LogPruningJob
  {
      public async Task ExecuteAsync(AppDbContext db, CancellationToken cancellationToken)
      {
          var retentionDays = await GetRetentionDaysAsync(db, cancellationToken);
          var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
          await db.JobHistories
              .Where(h => h.RunTime < cutoff)
              .ExecuteDeleteAsync(cancellationToken);
      }

      private async Task<int> GetRetentionDaysAsync(AppDbContext db, CancellationToken ct)
      {
          var config = await db.ApplicationConfigurations
              .FirstOrDefaultAsync(c =>
                  c.CategoryName == "Logging" &&
                  c.ConfigurationName == "RetentionDays", ct);
          return int.TryParse(config?.ConfigurationValue, out var days) ? days : 7;
      }
  }
  ```

**Files to modify:**

- `Program.cs` — register the recurring job:
  ```csharp
  RecurringJob.AddOrUpdate<LogPruningJob>(
      "log-pruning",
      job => job.ExecuteAsync(/* injected */),
      Cron.Daily);
  ```

- `Data/SeedData.cs` — seed the default retention configuration:
  ```csharp
  // Seed ApplicationConfiguration: Logging / RetentionDays = 7
  ```

**Acceptance criteria:**

- [ ] After running, `JobHistory` records older than the retention threshold are deleted
- [ ] Default threshold is 7 days when no configuration exists

---

## 2.6 "Run Now" Endpoint

**Goal:** Let users trigger a job immediately from the UI.

**Files to modify:**

- `Controllers/ScheduledJobController.cs` — add a `POST RunNow(int id)` action:
  ```csharp
  [HttpPost]
  public IActionResult RunNow(int id)
  {
      BackgroundJob.Enqueue<PluginExecutorJob>(
          job => job.RunSingleJobAsync(id, /* injected */));
      TempData["Toast"] = "Job queued for immediate execution.";
      return RedirectToAction("Index");
  }
  ```

**Acceptance criteria:**

- [ ] POST to `/ScheduledJob/RunNow/{id}` enqueues a Hangfire background job
- [ ] The enqueued job runs through the same plugin execution + logging path as the recurring job
- [ ] UI shows confirmation feedback (TempData toast or redirect)
