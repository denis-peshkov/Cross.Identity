namespace Cross.Identity.Services.Crypto;

/// <summary>
/// E.164 phone helpers. Cross.Identity accepts only already-normalized numbers
/// such as <c>+79161234567</c>; national formats, spaces, and punctuation are rejected
/// by <see cref="IsValid"/> / <see cref="Require"/>. Hosts may use <see cref="Normalize"/> /
/// <see cref="Ensure"/> before calling Identity.
/// </summary>
public static class PhoneE164
{
    /// <summary>E.164: '+' + country code (no leading 0) + subscriber, 7–15 digits total after '+'.</summary>
    private static readonly Regex Pattern = new(
        @"^\+[1-9]\d{6,14}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

    /// <summary>
    /// Returns <c>true</c> when <paramref name="phone"/> is already a valid E.164 string
    /// (no trimming — leading/trailing whitespace fails).
    /// </summary>
    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrEmpty(phone) || !Pattern.IsMatch(phone))
            return false;

        try
        {
            var number = Util.Parse(phone, defaultRegion: null);
            if (!Util.IsValidNumber(number))
                return false;

            return string.Equals(
                Util.Format(number, PhoneNumberFormat.E164),
                phone,
                StringComparison.Ordinal);
        }
        catch (NumberParseException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that <paramref name="phone"/> is already E.164; otherwise throws
    /// <see cref="ArgumentException"/>. Does not reformat national or free-form numbers.
    /// </summary>
    public static string Require(string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        if (!IsValid(phone))
            throw new ArgumentException(
                "Phone must already be E.164 (e.g. +79161234567). Other formats are not accepted.",
                nameof(phone));
        return phone;
    }

    /// <summary>
    /// Host helper: normalizes a free-form phone string to E.164 (e.g. <c>+40722123456</c>).
    /// </summary>
    /// <param name="raw">Raw input (may include spaces, parentheses, etc.).</param>
    /// <param name="defaultRegion">Two-letter ISO region code (e.g. <c>RO</c>, <c>UA</c>, <c>RU</c>).</param>
    /// <returns>E.164 or <c>null</c> if the number is invalid.</returns>
    public static string? Normalize(string raw, string defaultRegion)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            var number = Util.Parse(raw.Trim(), defaultRegion?.ToUpperInvariant());
            if (!Util.IsValidNumber(number))
                return null;

            return Util.Format(number, PhoneNumberFormat.E164);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Host helper: same as <see cref="Normalize"/>, but throws if invalid.</summary>
    public static string NormalizeOrThrow(string raw, string defaultRegion)
    {
        var normalized = Normalize(raw, defaultRegion);
        if (normalized is null)
            throw new ArgumentException(
                "Phone number is invalid or cannot be normalized to E.164.",
                nameof(raw));
        return normalized;
    }

    /// <summary>
    /// Host helper: returns <paramref name="raw"/> if it is already E.164; otherwise
    /// normalizes with <paramref name="defaultRegion"/> and requires a valid E.164 result.
    /// </summary>
    public static string Ensure(string raw, string defaultRegion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);
        if (IsValid(raw))
            return raw;

        return NormalizeOrThrow(raw, defaultRegion);
    }
}
