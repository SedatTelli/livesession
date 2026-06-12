using Microsoft.Extensions.Logging;

namespace LiveSession.Infrastructure.Chromium;

internal sealed class SalesforceHeartbeat
{
    private static readonly HttpClient _http = new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies        = false,
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly string IslandUserDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Island", "Island", "User Data");

    private readonly ILogger _logger;

    internal SalesforceHeartbeat(ILogger logger) => _logger = logger;

    /// <summary>
    /// Island Browser'dan Salesforce cookie'lerini okur ve
    /// salesforce.com'a HEAD isteği atarak server session'ı sıfırlar.
    /// </summary>
    internal async Task PingAsync()
    {
        if (!Directory.Exists(IslandUserDataDir))
        {
            _logger.LogDebug("Island user data not found: {Dir}", IslandUserDataDir);
            return;
        }

        var cookieHeader = ChromiumCookieReader.GetSalesforceCookieHeader(IslandUserDataDir);
        if (cookieHeader is null)
        {
            _logger.LogDebug("Salesforce heartbeat: no cookies found in Island Browser");
            return;
        }

        // Salesforce org URL'ini cookie'den çıkar (sid cookie'nin host'u)
        var orgUrl = ExtractOrgUrl(cookieHeader);
        if (orgUrl is null)
        {
            _logger.LogDebug("Salesforce heartbeat: could not determine org URL");
            return;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Head, orgUrl);
            req.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            req.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var resp = await _http.SendAsync(req);
            _logger.LogInformation("Salesforce HTTP heartbeat → {Url} [{Status}]",
                orgUrl, (int)resp.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Salesforce heartbeat request failed: {Msg}", ex.Message);
        }
    }

    private static string? ExtractOrgUrl(string cookieHeader)
    {
        // Bilinen Marriott Salesforce URL'i — cookie'de "sid" varsa bu domain için ping at
        if (cookieHeader.Contains("sid=", StringComparison.OrdinalIgnoreCase))
            return "https://marriottintl.lightning.force.com/";

        return null;
    }
}
