using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LiveSession.Infrastructure.Chromium;

internal static class ChromiumCookieReader
{
    /// <summary>
    /// Island Browser'ın Salesforce cookie'lerini okur ve Cookie header string döner.
    /// Hata durumunda null döner — çağıran kodu bozmaz.
    /// </summary>
    internal static string? GetSalesforceCookieHeader(string userDataDir)
        => GetCookieHeader(userDataDir,
            "host_key LIKE '%.salesforce.com%' OR host_key LIKE '%.force.com%'");

    internal static string? GetMarriottSsoCookieHeader(string userDataDir)
        => GetCookieHeader(userDataDir,
            "host_key LIKE '%.marriott.com%' OR host_key LIKE '%.marriotts.com%'");

    private static string? GetCookieHeader(string userDataDir, string whereClause)
    {
        try
        {
            var key = ReadDecryptionKey(userDataDir);
            if (key is null) return null;

            var cookiesDb = Path.Combine(userDataDir, "Default", "Network", "Cookies");
            if (!File.Exists(cookiesDb)) return null;

            // Browser dosyayı kilitleyebilir — geçici kopyayla çalış
            var tmp = Path.Combine(Path.GetTempPath(), $"ls_cookies_{Guid.NewGuid():N}.db");
            try
            {
                File.Copy(cookiesDb, tmp, overwrite: true);
                return ReadCookies(tmp, key, whereClause);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? ReadDecryptionKey(string userDataDir)
    {
        var localState = Path.Combine(userDataDir, "Local State");
        if (!File.Exists(localState)) return null;

        using var doc = JsonDocument.Parse(File.ReadAllText(localState));
        var b64 = doc.RootElement
            .GetProperty("os_crypt")
            .GetProperty("encrypted_key")
            .GetString();

        if (b64 is null) return null;

        // İlk 5 byte "DPAPI" prefix — atla
        var dpapi = Convert.FromBase64String(b64)[5..];
        return ProtectedData.Unprotect(dpapi, null, DataProtectionScope.CurrentUser);
    }

    private static string? ReadCookies(string dbPath, byte[] key, string whereClause)
    {
        var parts = new List<string>();

        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode       = SqliteOpenMode.ReadOnly
        }.ToString();

        using var conn = new SqliteConnection(cs);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT name, value, encrypted_value
            FROM cookies
            WHERE {whereClause}
            ORDER BY host_key, name";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var name  = reader.GetString(0);
            var value = reader.IsDBNull(1) ? "" : reader.GetString(1);

            if (string.IsNullOrEmpty(value))
            {
                var enc = (byte[])reader.GetValue(2);
                value = Decrypt(enc, key) ?? "";
            }

            if (!string.IsNullOrEmpty(value))
                parts.Add($"{name}={value}");
        }

        return parts.Count > 0 ? string.Join("; ", parts) : null;
    }

    private static string? Decrypt(byte[] data, byte[] key)
    {
        try
        {
            if (data.Length < 3) return null;

            var prefix = Encoding.ASCII.GetString(data, 0, 3);

            if (prefix is "v10" or "v11")
            {
                // AES-256-GCM: [3 prefix][12 IV][cipher][16 tag]
                var iv        = data[3..15];
                var cipherLen = data.Length - 3 - 12 - 16;
                if (cipherLen <= 0) return null;
                var cipher    = data[15..(15 + cipherLen)];
                var tag       = data[^16..];

                var plain = new byte[cipherLen];
                using var aes = new AesGcm(key, 16);
                aes.Decrypt(iv, cipher, tag, plain);
                return Encoding.UTF8.GetString(plain);
            }

            // Eski Chromium: doğrudan DPAPI ile şifrelenmiş
            return Encoding.UTF8.GetString(
                ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser));
        }
        catch
        {
            return null;
        }
    }
}
