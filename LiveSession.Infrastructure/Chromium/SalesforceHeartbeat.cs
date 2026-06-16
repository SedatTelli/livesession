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

    private const string SalesforceUrl  = "https://marriottintl.lightning.force.com/lightning/o/Case/home";
    private const string MarriottSsoUrl = "https://extranetcloud.marriott.com/";

    private readonly ILogger _logger;

    internal SalesforceHeartbeat(ILogger logger) => _logger = logger;

    internal async Task PingAsync()
    {
        var userDataDir = FindIslandUserDataDir();
        if (userDataDir is null)
        {
            _logger.LogWarning("GXP heartbeat: Island Browser user data directory not found. Tried: {Paths}",
                string.Join(", ", IslandUserDataPaths));
            return;
        }

        // İki session paralel olarak canlı tutulur
        await Task.WhenAll(
            PingSalesforceAsync(userDataDir),
            PingMarriottSsoAsync(userDataDir));
    }

    private async Task PingSalesforceAsync(string userDataDir)
    {
        var cookies = ChromiumCookieReader.GetSalesforceCookieHeader(userDataDir);
        if (cookies is null)
        {
            _logger.LogWarning("GXP heartbeat: no Salesforce cookies found in Island Browser");
            return;
        }

        await SendGetAsync(SalesforceUrl, cookies, "https://marriottintl.lightning.force.com/", "Salesforce");
    }

    private async Task PingMarriottSsoAsync(string userDataDir)
    {
        var cookies = ChromiumCookieReader.GetMarriottSsoCookieHeader(userDataDir);
        if (cookies is null)
        {
            _logger.LogWarning("GXP heartbeat: no Marriott SSO cookies found in Island Browser");
            return;
        }

        await SendGetAsync(MarriottSsoUrl, cookies, MarriottSsoUrl, "Marriott SSO");
    }

    private async Task SendGetAsync(string url, string cookieHeader, string referer, string label)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            req.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            req.Headers.TryAddWithoutValidation("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            req.Headers.TryAddWithoutValidation("Referer", referer);

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            _logger.LogInformation("GXP heartbeat [{Label}] → {Url} [{Status}]",
                label, url, (int)resp.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("GXP heartbeat [{Label}] failed: {Msg}", label, ex.Message);
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
