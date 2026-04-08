# MainSpring Port — Implementation Plan

> **Companion to:** `mainspring-port-idea.md`  
> **Target Project:** `MainSpringTwo.Web` (.NET 10, ASP.NET Core MVC)

This document breaks the four roadmap phases into concrete, ordered implementation steps. Each phase is detailed in its own file for easier focus during implementation.

---

## Phases

| Phase | File | Summary |
|---|---|---|
| **Phase 1 — Foundation** | [`phase-1-foundation.md`](phase-1-foundation.md) | npm + Tailwind CSS 4 build pipeline, Alpine.js setup, layout shell with dark mode, EF Core + SQLite data model, ASP.NET Core Identity |
| **Phase 2 — Hangfire + Plugin Infrastructure** | [`phase-2-hangfire-plugins.md`](phase-2-hangfire-plugins.md) | Hangfire setup, IPlugin interface & types, plugin registry, PluginExecutorJob, LogPruningJob, "Run Now" endpoint |
| **Phase 3 — Controllers & Views** | [`phase-3-controllers-views.md`](phase-3-controllers-views.md) | Toast system, Dashboard, ScheduledJob CRUD, Plugin read-only list, ApplicationConfiguration CRUD, JobHistory paginated view, dropped controllers |
| **Phase 4 — Polish & Testing** | [`phase-4-polish-testing.md`](phase-4-polish-testing.md) | Responsive layouts, toast polish, ScheduleHelper timing tests, Hangfire dashboard auth, dark mode audit |

---

## Appendix A

| Legacy Entity | New Entity | Changes |
|---|---|---|
| `Host` | *(dropped)* | No multi-host model |
| `Plugin` | `Plugin` | Dropped `AssemblyName`, `ClassName`, `Version`, `Package`; added `IsActive` |
| `ScheduledJob` | `ScheduledJob` | Dropped `HostId` FK; `ScheduleType` is now an enum instead of FK |
| `ScheduleType` (lookup table) | `ScheduleType` (enum) | No longer a DB table |
| `PluginConfiguration` | *(dropped)* | Parameter metadata comes from `IPlugin.ConfigurationParameters` at runtime |
| `DataType` (lookup table) | `ParameterDataType` (enum) | No longer a DB table |
| `ConfigurationValue` | `ConfigurationValue` | Replaced `PluginConfigurationId` FK with `ParameterName` string |
| `ScheduledJobStatus` (lookup table) | *(dropped)* | Replaced with `JobHistory.IsError` boolean |
| `ApplicationConfiguration` | `ApplicationConfiguration` | Unchanged |
| `JobHistory` | `JobHistory` | Replaced `ScheduledJobStatusId` FK with `IsError` boolean |

## Appendix B — Legacy-to-New Controller Mapping

| Legacy Controller | New Controller | Changes |
|---|---|---|
| `HomeController` | `HomeController` | Now returns dashboard stats |
| `HostController` | *(dropped)* | No multi-host model |
| `ApiHostController` | *(dropped)* | No agent API needed |
| `ScheduledJobController` | `ScheduledJobController` | Added Run Now; dropped Host dropdown; inline config editing |
| `PluginController` | `PluginController` | Read-only (no upload); backed by `PluginRegistry` |
| `PluginConfigurationController` | *(folded into ScheduledJob)* | Parameter metadata from `IPlugin` |
| `ConfigurationValueController` | *(folded into ScheduledJob)* | Inline editing on ScheduledJob Edit |
| `ApplicationConfigurationController` | `ApplicationConfigurationController` | Largely unchanged |
| `JobHistoryController` | `JobHistoryController` | Added pagination, filter, 7-day window |

## Appendix C — File Inventory Checklist

A full list of files expected at project completion:

```
MainSpringTwo.Web/
├── Controllers/
│   ├── HomeController.cs
│   ├── ScheduledJobController.cs
│   ├── PluginController.cs
│   ├── ApplicationConfigurationController.cs
│   └── JobHistoryController.cs
├── Data/
│   ├── AppDbContext.cs
│   ├── SeedData.cs
│   └── Migrations/
├── Jobs/
│   ├── PluginExecutorJob.cs
│   ├── LogPruningJob.cs
│   └── HangfireAuthorizationFilter.cs
├── Models/
│   ├── Entities/
│   │   ├── Plugin.cs
│   │   ├── ScheduledJob.cs
│   │   ├── ScheduleType.cs          (enum)
│   │   ├── ConfigurationValue.cs
│   │   ├── ApplicationConfiguration.cs
│   │   └── JobHistory.cs
│   ├── Plugins/
│   │   ├── IPlugin.cs
│   │   ├── PluginParameter.cs
│   │   ├── ParameterDataType.cs      (enum)
│   │   ├── PluginResult.cs
│   │   └── PluginLogEntry.cs
│   └── ViewModels/
│       ├── ScheduledJobViewModel.cs
│       └── JobHistoryIndexViewModel.cs
├── Plugins/
│   └── SamplePlugin.cs
├── Services/
│   ├── PluginRegistry.cs
│   └── ScheduleHelper.cs
├── Views/
│   ├── _ViewStart.cshtml
│   ├── _ViewImports.cshtml
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   └── _Toast.cshtml
│   ├── Home/
│   │   └── Index.cshtml
│   ├── ScheduledJob/
│   │   ├── Index.cshtml
│   │   ├── Details.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   └── Delete.cshtml
│   ├── Plugin/
│   │   ├── Index.cshtml
│   │   └── Details.cshtml
│   ├── ApplicationConfiguration/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   └── Delete.cshtml
│   └── JobHistory/
│       ├── Index.cshtml
│       └── Details.cshtml
├── wwwroot/
│   ├── css/
│   │   ├── app.css
│   │   └── app.min.css              (generated)
│   └── js/
├── Specifications/
│   ├── mainspring-port-idea.md
│   ├── mainspring-port-plan.md
│   ├── phase-1-foundation.md
│   ├── phase-2-hangfire-plugins.md
│   ├── phase-3-controllers-views.md
│   └── phase-4-polish-testing.md
├── package.json
├── appsettings.json
├── Program.cs
└── MainSpringTwo.Web.csproj

MainSpringTwo.Tests/
└── ScheduleHelperTests.cs
```
