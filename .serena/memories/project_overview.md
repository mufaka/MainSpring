# MainSpringPort project overview

- Repository root: `C:\Development\MainSpringPort`
- Solution entry: `MainSpringPort.slnx`
- Primary app: `MainSpringTwo.Web` (`net10.0` ASP.NET Core web app)
- Test project: `MainSpringTwo.Tests` (`net10.0`, xUnit)

## Purpose
MainSpringTwo is a port of an older .NET Framework 4.6.1 web application into modern ASP.NET Core/.NET 10. It provides an operational dashboard for scheduled plugin execution, using ASP.NET Core Identity for auth, Hangfire for scheduling/background jobs, SQLite for storage, Tailwind CSS 4 for styling, and Alpine.js on the frontend.

## Tech stack
- .NET 10 / ASP.NET Core
- Razor Pages for Identity UI (`Areas/Identity/...`) and MVC controllers/views elsewhere
- EF Core 10 with SQLite
- ASP.NET Core Identity
- Hangfire + Hangfire.InMemory
- Tailwind CSS 4 CLI
- Alpine.js
- xUnit + Microsoft.NET.Test.Sdk + coverlet.collector

## Rough structure
- `MainSpringTwo.Web/Program.cs` - composition root and middleware pipeline
- `MainSpringTwo.Web/Controllers/` - MVC controllers for dashboard, jobs, plugins, config, history
- `MainSpringTwo.Web/Areas/Identity/` - Razor Pages auth UI
- `MainSpringTwo.Web/Data/` - `AppDbContext`, seeding, migrations referenced from README
- `MainSpringTwo.Web/Jobs/` - Hangfire jobs and dashboard auth filter
- `MainSpringTwo.Web/Models/Entities` - persistence models
- `MainSpringTwo.Web/Models/ViewModels` - MVC view models
- `MainSpringTwo.Web/Models/Plugins` - plugin contracts and DTOs
- `MainSpringTwo.Web/Plugins/` - concrete plugins such as `SamplePlugin` and `GenericHttpRequestPlugin`
- `MainSpringTwo.Web/Services/` - scheduling helpers and plugin registry
- `MainSpringTwo.Web/Views/` - UI views; instruction file says UI work should stay modern/polished
- `MainSpringTwo.Web/wwwroot/` - static assets including Tailwind CSS output
- `MainSpringTwo.Tests/` - unit tests focused on scheduling helpers and plugin registry behavior

## Runtime notes
- Default local URL from launch settings: `http://localhost:5235`
- Default DB connection uses SQLite: `Data Source=mainspring.db`
- EF Core migrations are applied automatically at startup
- Default seeded admin credentials from README:
  - username: `admin`
  - email: `admin@localhost`
  - password: `ChangeMe123!`
- Hangfire dashboard is mapped at `/hangfire` and requires auth
- App integrates with `systemd` (`builder.Host.UseSystemd()`), and `DEPLOY.md` documents Linux deployment
