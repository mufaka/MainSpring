# Suggested commands for MainSpringPort (Windows / PowerShell)

## Repository / navigation
- `Set-Location C:\Development\MainSpringPort`
- `Get-ChildItem`
- `Get-ChildItem -Recurse`
- `git status`
- `git diff`
- `git log --oneline -n 20`
- `Select-String -Path .\* -Pattern "text" -Recurse`

## Build and restore
- `dotnet build .\MainSpringPort.slnx`
- `dotnet build .\MainSpringTwo.Web\MainSpringTwo.Web.csproj`
- `dotnet build .\MainSpringTwo.Tests\MainSpringTwo.Tests.csproj`

Notes:
- Building the web project also runs `npm install` and `npm run css:build` via the csproj targets.
- Node.js and npm are required for web project build/publish.

## Run the app
From repo root:
- `dotnet run --project .\MainSpringTwo.Web\MainSpringTwo.Web.csproj`

From web project dir:
- `Set-Location .\MainSpringTwo.Web`
- `dotnet run`

Default local launch URL from `launchSettings.json`:
- `http://localhost:5235`

## Frontend asset workflow
From `MainSpringTwo.Web`:
- `npm install`
- `npm run css:build`
- `npm run css:watch`

## Tests
- `dotnet test .\MainSpringTwo.Tests\MainSpringTwo.Tests.csproj`
- `dotnet test .\MainSpringPort.slnx`

## Publish / deployment
- `dotnet publish .\MainSpringTwo.Web\MainSpringTwo.Web.csproj -c Release -o .\publish\MainSpringTwo.Web`

## Formatting / linting
No dedicated lint config or `.editorconfig` was found during onboarding.
Practical default command if formatting is needed:
- `dotnet format .\MainSpringPort.slnx`
