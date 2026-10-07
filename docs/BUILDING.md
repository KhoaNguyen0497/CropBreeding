# Build and release

Use .NET SDK 8 and a Stardew Valley 1.6 installation with SMAPI 4:

```sh
dotnet build src/CropBreeding.csproj -c Release -p:GamePath="/path/to/Stardew Valley"
dotnet run --project tests/TraitRules.Tests.csproj -c Release
dotnet run --project safety-tests/Safety.Tests.csproj -c Release
python scripts/package.py
```

The installable ZIP is written to `artifacts/`. It includes only the mod DLL, manifest and assets; game/SMAPI assemblies and user settings are excluded.

The GitHub Actions release workflow uses pinned build-only reference assemblies, runs both test suites, builds and validates the ZIP, and publishes a release when `src/manifest.json` changes on main. Existing releases are never overwritten. Keep the project and manifest versions equal and add version-specific notes to `CHANGELOG.md` before publishing. The workflow can also be started manually.

Automated checks do not replace [in-game validation](TESTING.md).
