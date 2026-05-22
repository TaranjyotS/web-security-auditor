namespace WebSecurityAuditor.Core;

public sealed class RecommendationEngine
{
    public IReadOnlyList<string> Build(AuditReport report)
    {
        var recommendations = new List<string>();
        var openPorts = report.Ports.Select(port => port.Port).ToHashSet();
        var headers = report.Http?.Headers.ToDictionary(h => h.Name, h => h.Value, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (openPorts.Contains(80))
        {
            recommendations.Add("HTTP is open. Redirect HTTP to HTTPS and confirm no sensitive traffic uses plaintext.");
        }

        if (openPorts.Contains(443))
        {
            recommendations.Add("HTTPS is open. Review TLS certificate validity, protocol versions, and cipher suites.");
        }

        foreach (var riskyPort in new[] { 21, 22, 1433, 3306, 3389, 5432, 6379 })
        {
            if (openPorts.Contains(riskyPort))
            {
                recommendations.Add($"Port {riskyPort} is exposed. Restrict it with firewall rules, VPN, allowlists, or private networking if not public-facing.");
            }
        }

        if (report.Http?.StatusCode is not null)
        {
            AddHeaderRecommendation(headers, "Strict-Transport-Security", "Add Strict-Transport-Security after confirming HTTPS is stable.", recommendations);
            AddHeaderRecommendation(headers, "X-Content-Type-Options", "Add X-Content-Type-Options: nosniff to reduce browser content-type confusion risk.", recommendations);
            AddHeaderRecommendation(headers, "Content-Security-Policy", "Add a Content-Security-Policy tuned to the app to reduce client-side injection impact.", recommendations);
            AddHeaderRecommendation(headers, "X-Frame-Options", "Add X-Frame-Options or CSP frame-ancestors to reduce clickjacking exposure.", recommendations);
            AddHeaderRecommendation(headers, "Referrer-Policy", "Add Referrer-Policy to control how much URL information browsers share.", recommendations);
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("No obvious exposure was found in this lightweight audit. Continue with authenticated scanning, dependency checks, and cloud configuration review.");
        }

        return recommendations;
    }

    private static void AddHeaderRecommendation(IReadOnlyDictionary<string, string> headers, string header, string recommendation, ICollection<string> recommendations)
    {
        if (!headers.ContainsKey(header))
        {
            recommendations.Add(recommendation);
        }
    }
}
