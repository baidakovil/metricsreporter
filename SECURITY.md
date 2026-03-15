# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 0.4.x   | :white_check_mark: |
| < 0.4   | :x:                |

## Reporting a Vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Instead, report them via [GitHub Security Advisories](https://github.com/baidakovil/metricsreporter/security/advisories/new). You will receive a response within 5 business days.

### What to include

- A description of the vulnerability and its potential impact.
- Steps to reproduce or a proof-of-concept.
- MetricsReporter version (`metricsreporter --version`).
- .NET SDK version (`dotnet --version`).

## Scope

MetricsReporter is a local CLI tool that reads files from disk and writes HTML/JSON reports. It does not make network requests, start servers, or handle user-supplied input over a network. The primary security surface is **file path handling** — passing untrusted file paths to the tool on a shared machine could allow directory traversal.
