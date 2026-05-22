using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace WebSecurityAuditor.Core;

public static partial class TargetValidator
{
    public const int MaxPortsPerAudit = 1000;

    public static AuditRequest Validate(string? target, string? startPort, string? endPort, string? timeoutMs, bool authorized)
    {
        if (!authorized)
        {
            throw new ArgumentException("You must confirm authorization before running an audit.");
        }

        var cleanTarget = NormalizeTarget(target);
        var start = ParsePort(startPort, 1, nameof(startPort));
        var end = ParsePort(endPort, 1000, nameof(endPort));
        var timeout = ParseTimeout(timeoutMs, 800);

        ValidatePorts(start, end);
        return new AuditRequest(cleanTarget, start, end, timeout, true);
    }

    public static string NormalizeTarget(string? rawTarget)
    {
        if (string.IsNullOrWhiteSpace(rawTarget))
        {
            throw new ArgumentException("Target is required.");
        }

        var target = rawTarget.Trim();
        if (Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            target = uri.Host;
        }

        if (IPAddress.TryParse(target, out _))
        {
            return target;
        }

        if (!HostnameRegex().IsMatch(target))
        {
            throw new ArgumentException("Target must be a valid hostname, URL, or IP address.");
        }

        return target.ToLowerInvariant();
    }

    public static void ValidatePorts(int startPort, int endPort)
    {
        if (startPort is < 1 or > 65535 || endPort is < 1 or > 65535)
        {
            throw new ArgumentException("Ports must be between 1 and 65535.");
        }

        if (startPort > endPort)
        {
            throw new ArgumentException("Start port cannot be greater than end port.");
        }

        var totalPorts = endPort - startPort + 1;
        if (totalPorts > MaxPortsPerAudit)
        {
            throw new ArgumentException($"For safety, one audit is limited to {MaxPortsPerAudit.ToString(CultureInfo.InvariantCulture)} ports.");
        }
    }

    private static int ParsePort(string? value, int defaultValue, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
        {
            throw new ArgumentException($"{name} must be a valid integer.");
        }

        return port;
    }

    private static int ParseTimeout(string? value, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout))
        {
            throw new ArgumentException("timeoutMs must be a valid integer.");
        }

        if (timeout is < 100 or > 5000)
        {
            throw new ArgumentException("timeoutMs must be between 100 and 5000.");
        }

        return timeout;
    }

    [GeneratedRegex(@"^(?=.{1,253}$)([a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,63}$", RegexOptions.Compiled)]
    private static partial Regex HostnameRegex();
}
