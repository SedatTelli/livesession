using Microsoft.Extensions.Logging;

namespace LiveSession.Infrastructure.Chromium;

internal sealed class SalesforceHeartbeat
{
    private static readonly HttpClient _http = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 3,
        UseCookies        = false,
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    // Island Browser'ın farklı kurulumlarda kullanabileceği olası yollar
    private static readonly string[] IslandUserDataPaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Island", "Island", "User Data"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Island", "User Data"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Island", "Island", "User Data"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Island", "User Data"),
    ];

    private const string SalesforceBaseUrl =
        "https://marriottintl.lightning.force.com/lightning/o/Case/home";

    private readonly ILogger _logger;

    internal SalesforceHeartbeat(ILogger logger) => _logger = logger;

    internal async Task PingAsync()
    {
        var userDataDir = FindIslandUserDataDir();
        if (userDataDir is null)
        {
            _logger.LogWarning("Salesforce heartbeat: Island Browser user data directory not found. Tried: {Paths}",
                string.Join(", ", IslandUserDataPaths));
            return;
        }

        var cookieHeader = ChromiumCookieReader.GetSalesforceCookieHeader(userDataDir);
        if (cookieHeader is null)
        {
            _logger.LogWarning("Salesforce heartbeat: no Salesforce cookies found in Island Browser (dir: {Dir})",
                userDataDir);
            return;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, SalesforceBaseUrl);
            req.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            req.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            req.Headers.TryAddWithoutValidation("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            req.Headers.TryAddWithoutValidation("Referer",
                "https://marriottintl.lightning.force.com/");

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            _logger.LogInformation("Salesforce heartbeat → {Url} [{Status}]",
                SalesforceBaseUrl, (int)resp.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Salesforce heartbeat request failed: {Msg}", ex.Message);
        }
    }

    private static string? FindIslandUserDataDir()
    {
        foreach (var path in IslandUserDataPaths)
            if (Directory.Exists(path))
                return path;
        return null;
    }
}
