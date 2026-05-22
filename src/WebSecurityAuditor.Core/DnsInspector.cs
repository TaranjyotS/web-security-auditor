using System.Net;

namespace WebSecurityAuditor.Core;

public sealed class DnsInspector
{
    public async Task<DnsResult> InspectAsync(string target, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(target, out var parsedIp))
        {
            return new DnsResult(target, [parsedIp.ToString()]);
        }

        var addresses = await Dns.GetHostAddressesAsync(target, cancellationToken).ConfigureAwait(false);
        return new DnsResult(target, addresses.Select(address => address.ToString()).Distinct().Order().ToArray());
    }
}
