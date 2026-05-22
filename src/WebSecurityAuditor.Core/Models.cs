namespace WebSecurityAuditor.Core;

public enum AuditStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public sealed record AuditRequest(string Target, int StartPort, int EndPort, int TimeoutMs, bool Authorized);

public sealed record DnsResult(string Target, IReadOnlyList<string> IpAddresses);

public sealed record HttpHeader(string Name, string Value);

public sealed record HttpResult(
    string Url,
    int? StatusCode,
    bool IsHttps,
    string? Server,
    IReadOnlyList<HttpHeader> Headers,
    string? Error);

public sealed record PortResult(int Port, bool IsOpen, string? ServiceHint);

public sealed record AuditReport(
    Guid Id,
    string Target,
    DateTimeOffset CreatedAtUtc,
    AuditStatus Status,
    DnsResult? Dns,
    HttpResult? Http,
    IReadOnlyList<PortResult> Ports,
    IReadOnlyList<string> Recommendations,
    string? ErrorMessage);
