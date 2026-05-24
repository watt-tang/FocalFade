# Contributing to FocalFade

Thank you for your interest in contributing to FocalFade!

## Getting Started

1. Fork the repository
2. Clone your fork
3. Create a feature branch: `git checkout -b feature/my-feature`
4. Make your changes
5. Run tests: `dotnet test`
6. Commit and push
7. Create a Pull Request

## Development Setup

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download) or later.

```bash
dotnet build
dotnet test
dotnet run --project src/FocalFade.App
```

## Code Style

- Use nullable reference types
- Use records for simple data models
- Use CommunityToolkit.Mvvm for MVVM in the settings UI
- Keep native interop in the `Native/` namespace
- Use interfaces for services to enable testing
- Prefer XML comments only where they clarify complex behavior

## Testing

Run all tests:
```bash
dotnet test
```

Tests use xUnit and FluentAssertions. Add tests for:
- Geometry calculations
- Settings persistence
- App rule matching
- Fullscreen detection logic

## Pull Requests

- Keep PRs focused on a single feature or fix
- Include a clear description of what changed and why
- Ensure `dotnet build` and `dotnet test` pass
- Update documentation if adding user-facing features

## Reporting Issues

Please include:
- Windows version (10/11, build number)
- Monitor setup (number, resolution, DPI)
- Steps to reproduce
- Expected vs actual behavior

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
