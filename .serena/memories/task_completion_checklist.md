# Task completion checklist

When finishing work in MainSpringPort:

1. Build affected projects, usually:
   - `dotnet build .\MainSpringPort.slnx`
2. Run relevant tests, at minimum:
   - `dotnet test .\MainSpringTwo.Tests\MainSpringTwo.Tests.csproj`
3. If frontend assets were changed, ensure Tailwind output is rebuilt:
   - build already triggers CSS build for the web project
   - optionally run `npm run css:build` from `MainSpringTwo.Web`
4. For UI changes, verify the result stays modern/polished per project instruction.
5. Keep changes aligned with existing ASP.NET Core MVC + Razor Pages patterns.
6. Check for unintended diffs before finalizing:
   - `git status`
   - `git diff`
