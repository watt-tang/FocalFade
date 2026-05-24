# Release Process

## Versioning

FocalFade uses [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes to settings format or API
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, backward compatible

Pre-release versions use `-beta.N` suffix (e.g., `1.0.0-beta.1`).

## Build

```bash
dotnet build -c Release
```

## Test

```bash
dotnet test -c Release
```

## Publish

### Portable Self-Contained (win-x64)
```bash
dotnet publish src/FocalFade.App/FocalFade.App.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:PublishReadyToRun=true
```

### Using the Publish Script
```powershell
.\eng\publish.ps1
```

## Checksums

Generate SHA256 checksum for the release zip:
```powershell
Get-FileHash .\artifacts\FocalFade-win-x64-portable.zip -Algorithm SHA256
```

## GitHub Actions

### CI (ci.yml)
Runs on every push and PR:
- Restores dependencies
- Builds in Debug and Release
- Runs all tests

### Release (release.yml)
Manual workflow dispatch:
- Builds Release
- Publishes self-contained win-x64
- Creates release artifact with checksum
- Uploads to GitHub Releases

## Release Checklist

1. Update version in `Directory.Build.props`
2. Update `CHANGELOG.md`
3. Run `dotnet test -c Release`
4. Run `eng\publish.ps1`
5. Verify the portable zip works on a clean machine
6. Create GitHub Release with release notes
7. Upload artifacts and checksums
