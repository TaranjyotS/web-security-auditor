namespace WebSecurityAuditor.Core;

public sealed class HttpInspector
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpInspector(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HttpResult> InspectAsync(string target, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("auditor");
        foreach (var scheme in new[] { "https", "http" })
        {
            var url = $"{scheme}://{target}";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("WebSecurityAuditor/1.0");
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                var headers = response.Headers.Concat(response.Content.Headers)
                    .Select(header => new HttpHeader(header.Key, string.Join(", ", header.Value)))
                    .OrderBy(header => header.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                response.Headers.TryGetValues("Server", out var serverHeaders);
                return new HttpResult(url, (int)response.StatusCode, scheme == "https", serverHeaders?.FirstOrDefault(), headers, null);
            }
            catch (HttpRequestException) when (scheme == "https")
            {
                continue;
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                return new HttpResult(url, null, scheme == "https", null, [], $"HTTP request timed out: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                return new HttpResult(url, null, scheme == "https", null, [], ex.Message);
            }
        }

        return new HttpResult($"http://{target}", null, false, null, [], "No HTTP or HTTPS endpoint responded.");
    }
}
