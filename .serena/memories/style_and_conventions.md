# Style and conventions

## General C# conventions observed
- `Nullable` and `ImplicitUsings` are enabled in both projects.
- Dependency injection is used throughout; services are registered in `Program.cs` and injected via constructors.
- Controller code uses async EF Core queries with `AsNoTracking()`, LINQ, and clear local variable naming.
- Web project commonly uses block-scoped namespaces and explicit access modifiers.
- Test project uses file-scoped namespaces and xUnit `[Fact]` tests with descriptive `Method_State_Expectation` naming.
- Private readonly fields use underscore prefixes (for example `_db`, `_pluginRegistry`).
- Prefer strongly typed models/view models over dynamic patterns.

## Web/UI conventions
- This workspace contains a Razor Pages project area for Identity; prefer Razor Pages-aware solutions when auth UI is involved.
- Main UI work is MVC + Razor views, not Blazor.
- Copilot instruction: for `MainSpringTwo.Web` UI work, favor a modern, polished layout and presentation.

## Architecture notes
- `Program.cs` is the main composition root.
- Plugins are registered explicitly as `IPlugin` singletons, then consumed through `PluginRegistry`.
- Scheduling logic is centralized in services/helpers, with Hangfire recurring jobs configured in startup.
- Keep changes minimal and aligned with existing patterns rather than introducing new abstractions unless needed.

## Practical editing guidance
- Prefer existing libraries/framework features already in the project.
- Keep auth-related UI under `Areas/Identity` consistent with Razor Pages patterns.
- Keep controller/view-model separation intact.
- Preserve Tailwind/Alpine usage patterns for frontend changes.
