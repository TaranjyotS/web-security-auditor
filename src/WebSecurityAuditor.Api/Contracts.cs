namespace WebSecurityAuditor.Api;

public sealed record CreateAuditRequest(string Target, int StartPort = 1, int EndPort = 1000, int TimeoutMs = 800, bool Authorized = false);
