# Contributing to MetricsReporter

Thanks for your interest in contributing! Here's how to get started.

## Quick start

```powershell
git clone https://github.com/baidakovil/metricsreporter.git
cd metricsreporter
dotnet restore
dotnet build
dotnet test
```

## Workflow

1. Fork the repository and create a feature branch from `main`.
2. Make your changes — keep commits focused and well-described.
3. Ensure `dotnet build --no-incremental` passes with no warnings.
4. Ensure `dotnet test` passes.
5. Open a pull request against `main`.

## Code style

- Follow existing conventions in the codebase.
- Enable nullable reference types (`#nullable enable`).
- Add XML documentation for public members.
- Use NUnit + FluentAssertions + NSubstitute for tests.

## Reporting issues

Open a [GitHub Issue](https://github.com/baidakovil/metricsreporter/issues) with:
- Steps to reproduce
- Expected vs actual behavior
- .NET SDK version (`dotnet --info`)

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
