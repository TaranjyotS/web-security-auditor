# Architecture

```text
Browser Dashboard
      |
      v
ASP.NET Core Minimal API
      |
      +--> AuditOrchestrator
              |
              +--> DnsInspector
              +--> HttpInspector
              +--> PortScanner
              +--> RecommendationEngine
      |
      v
SQLite audit history
```

## Flow

1. User enters a target and confirms authorization.
2. API validates target, ports, timeout, and authorization flag.
3. Audit orchestrator runs DNS, HTTP, and controlled TCP checks.
4. Recommendation engine converts raw findings into practical hardening advice.
5. Full report is stored in SQLite.
6. Dashboard shows current results and recent audit history.
