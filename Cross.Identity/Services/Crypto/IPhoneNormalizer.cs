namespace Cross.Identity.Services.Crypto;

public interface IPhoneNormalizer
{
    string NormalizePhone(string phoneRaw);

    /// <summary>Normalizes a phone string to E.164 (e.g. +40722123456).</summary>
    /// <param name="raw">Raw input (may include spaces, parentheses, etc.).</param>
    /// <param name="defaultRegion">Two-letter ISO region code (e.g. "RO", "UA", "RU").</param>
    /// <returns>E.164 or null if the number is invalid.</returns>
    string? NormalizeToE164(string raw, string defaultRegion);

    /// <summary>Throws if the number is invalid.</summary>
    string NormalizeToE164OrThrow(string raw, string defaultRegion);
}
