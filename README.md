# Web Security Auditor

A production-style defensive web security auditing tool built with **.NET 8**, **ASP.NET Core**, **SQLite**, and a clean browser dashboard.

> Use this only on systems you own or are explicitly authorized to assess.

## Aim

This project demonstrates how to build a safe, portfolio-ready security utility that performs lightweight external exposure checks and turns them into actionable hardening recommendations.

## Features

- Web dashboard for running audits
- REST API for audit creation and retrieval
- DNS resolution checks
- HTTP/HTTPS status and header inspection
- Controlled TCP port exposure scan with a 1000-port safety limit
- Security recommendation engine
- SQLite audit history
- JSON report download
- Docker support
- GitHub Actions CI pipeline
- Unit tests for validation and recommendations

## Tech stack

- .NET 8
- ASP.NET Core Minimal APIs
- Entity Framework Core
- SQLite
- HTML/CSS/JavaScript dashboard
- xUnit
- Docker
- GitHub Actions

## Folder structure

```text
web-security-auditor/
├── src/
│   ├── WebSecurityAuditor.Api/       # API + dashboard + SQLite persistence
│   └── WebSecurityAuditor.Core/      # audit domain logic
├── tests/
│   └── WebSecurityAuditor.Tests/     # unit tests
├── docs/
│   └── ARCHITECTURE.md
├── Dockerfile
├── docker-compose.yml
├── WebSecurityAuditor.sln
└── README.md
```

## Run locally

```bash
dotnet restore WebSecurityAuditor.sln
dotnet build WebSecurityAuditor.sln
dotnet run --project src/WebSecurityAuditor.Api/WebSecurityAuditor.Api.csproj
```

Open:

```text
http://localhost:5000
```

If your machine uses a different port, check the terminal output from `dotnet run`.

## Run with Docker

```bash
docker compose up --build
```

Open:

```text
http://localhost:8080
```

## API examples

Create an audit:

```bash
curl -X POST http://localhost:5000/api/audits \
  -H "Content-Type: application/json" \
  -d '{"target":"example.com","startPort":80,"endPort":443,"timeoutMs":800,"authorized":true}'
```

List audits:

```bash
curl http://localhost:5000/api/audits
```

Read audit by ID:

```bash
curl http://localhost:5000/api/audits/{id}
```

Download JSON report:

```bash
curl -OJ http://localhost:5000/api/reports/{id}/download
```

## Troubleshooting

### `IHttpClientFactory could not be found`

Make sure `src/WebSecurityAuditor.Core/WebSecurityAuditor.Core.csproj` includes:

```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.1" />
```

Then run:

```bash
dotnet clean
dotnet restore WebSecurityAuditor.sln
dotnet build WebSecurityAuditor.sln
```

### `CS0168: The variable 'ex' is declared but never used`

This project treats warnings as errors using:

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

So unused exception variables break the build. Use this pattern when the exception object is not needed:

```csharp
catch (HttpRequestException) when (scheme == "https")
{
    continue;
}
```

Use this pattern only when the exception message is actually returned or logged:

```csharp
catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
{
    return new HttpResult(url, null, scheme == "https", null, [], ex.Message);
}
```

### `FactAttribute could not be found` or `Fact could not be found`

This means the test files are missing the xUnit namespace import. Each test file should start with:

```csharp
using Xunit;
```

The tests project should also include these packages:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
```

After fixing it, run:

```bash
dotnet clean
dotnet restore WebSecurityAuditor.sln
dotnet build WebSecurityAuditor.sln
dotnet test WebSecurityAuditor.sln
```

### `dotnet run` looks stuck after `Now listening on http://localhost:5000`

That is expected. The API server is running and waiting for browser/API requests. Do not expect the terminal to return immediately. Open this in your browser:

```text
http://localhost:5000
```

To stop the server, press:

```text
Ctrl + C
```

### Port range safety error

The app intentionally limits scans to 1000 ports. Use a smaller range, for example:

```bash
# Dashboard: set Start Port = 80 and End Port = 443
# API: send startPort=80 and endPort=443
```

### Recommended clean rebuild

```bash
dotnet clean
dotnet restore WebSecurityAuditor.sln
dotnet build WebSecurityAuditor.sln
dotnet test WebSecurityAuditor.sln
dotnet run --project src/WebSecurityAuditor.Api/WebSecurityAuditor.Api.csproj
```
