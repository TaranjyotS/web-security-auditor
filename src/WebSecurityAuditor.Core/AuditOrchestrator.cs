namespace WebSecurityAuditor.Core;

public sealed class AuditOrchestrator
{
    private readonly DnsInspector _dnsInspector;
    private readonly HttpInspector _httpInspector;
    private readonly PortScanner _portScanner;
    private readonly RecommendationEngine _recommendationEngine;

    public AuditOrchestrator(DnsInspector dnsInspector, HttpInspector httpInspector, PortScanner portScanner, RecommendationEngine recommendationEngine)
    {
        _dnsInspector = dnsInspector;
        _httpInspector = httpInspector;
        _portScanner = portScanner;
        _recommendationEngine = recommendationEngine;
    }

    public async Task<AuditReport> RunAsync(Guid id, AuditRequest request, CancellationToken cancellationToken)
    {
        var createdAt = DateTimeOffset.UtcNow;
        try
        {
            var dns = await _dnsInspector.InspectAsync(request.Target, cancellationToken).ConfigureAwait(false);
            var http = await _httpInspector.InspectAsync(request.Target, cancellationToken).ConfigureAwait(false);
            var ports = await _portScanner.ScanAsync(request.Target, request.StartPort, request.EndPort, request.TimeoutMs, cancellationToken).ConfigureAwait(false);

            var baseReport = new AuditReport(id, request.Target, createdAt, AuditStatus.Completed, dns, http, ports, [], null);
            var recommendations = _recommendationEngine.Build(baseReport);
            return baseReport with { Recommendations = recommendations };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AuditReport(id, request.Target, createdAt, AuditStatus.Failed, null, null, [], [], ex.Message);
        }
    }
}
