using System.Net.Sockets;

namespace WebSecurityAuditor.Core;

public sealed class PortScanner
{
    private static readonly IReadOnlyDictionary<int, string> ServiceHints = new Dictionary<int, string>
    {
        [21] = "FTP", [22] = "SSH", [25] = "SMTP", [53] = "DNS", [80] = "HTTP",
        [110] = "POP3", [143] = "IMAP", [443] = "HTTPS", [465] = "SMTPS", [587] = "SMTP Submission",
        [993] = "IMAPS", [995] = "POP3S", [1433] = "SQL Server", [1521] = "Oracle DB",
        [3306] = "MySQL", [3389] = "RDP", [5432] = "PostgreSQL", [6379] = "Redis", [8080] = "HTTP Alternate"
    };

    public async Task<IReadOnlyList<PortResult>> ScanAsync(string target, int startPort, int endPort, int timeoutMs, CancellationToken cancellationToken)
    {
        TargetValidator.ValidatePorts(startPort, endPort);
        var throttler = new SemaphoreSlim(64);
        var tasks = Enumerable.Range(startPort, endPort - startPort + 1)
            .Select(port => ScanOneAsync(target, port, timeoutMs, throttler, cancellationToken));

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.Where(result => result.IsOpen).OrderBy(result => result.Port).ToArray();
    }

    private static async Task<PortResult> ScanOneAsync(string target, int port, int timeoutMs, SemaphoreSlim throttler, CancellationToken cancellationToken)
    {
        await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(target, port, cancellationToken).AsTask();
            var completed = await Task.WhenAny(connectTask, Task.Delay(timeoutMs, cancellationToken)).ConfigureAwait(false);
            var isOpen = completed == connectTask && client.Connected;
            return new PortResult(port, isOpen, ServiceHints.GetValueOrDefault(port));
        }
        catch (Exception exception) when (exception is SocketException or TimeoutException or OperationCanceledException or ArgumentException)
        {
            return new PortResult(port, false, ServiceHints.GetValueOrDefault(port));
        }
        finally
        {
            throttler.Release();
        }
    }
}
