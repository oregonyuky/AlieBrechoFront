using System.Text;
using System.Text.Json;

namespace AlieBrecho.Domain.Auth;

public static class JwtTokenReader
{
    public static DateTimeOffset? GetExpirationUtc(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var document = JsonDocument.Parse(payload);

            if (!document.RootElement.TryGetProperty("exp", out var expiration) ||
                !expiration.TryGetInt64(out var seconds))
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool IsExpired(string? token, TimeProvider? timeProvider = null)
    {
        var expirationUtc = GetExpirationUtc(token);
        if (expirationUtc is null)
        {
            return false;
        }

        timeProvider ??= TimeProvider.System;
        return expirationUtc <= timeProvider.GetUtcNow();
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
