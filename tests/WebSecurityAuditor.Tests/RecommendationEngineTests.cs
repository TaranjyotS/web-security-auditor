using Xunit;
using WebSecurityAuditor.Core;

namespace WebSecurityAuditor.Tests;

public sealed class RecommendationEngineTests
{
    [Fact]
    public void Build_RecommendsHttpsHardeningWhen443IsOpen()
    {
        var report = new AuditReport(
            Guid.NewGuid(),
            "example.com",
            DateTimeOffset.UtcNow,
            AuditStatus.Completed,
            null,
            new HttpResult("https://example.com", 200, true, null, [], null),
            [new PortResult(443, true, "HTTPS")],
            [],
            null);

        var recommendations = new RecommendationEngine().Build(report);
        Assert.Contains(recommendations, item => item.Contains("HTTPS", StringComparison.OrdinalIgnoreCase));
    }
}
