namespace Cross.Identity.Services.Crypto;

internal sealed class PhoneNormalizer : IPhoneNormalizer
{
    private readonly PhoneNumberUtil _util = PhoneNumberUtil.GetInstance();

    public string NormalizePhone(string phoneRaw)
    {
        var s = phoneRaw.Trim();
        var plus = s.StartsWith("+") ? "+" : "";
        var digits = new string(s.Where(char.IsDigit).ToArray());
        if (digits.Length is < 7 or > 18)
            throw new ArgumentException("Invalid phone length.", nameof(phoneRaw));
        return plus + digits;
    }

    public string? NormalizeToE164(string raw, string defaultRegion)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            var number = _util.Parse(raw.Trim(), defaultRegion?.ToUpperInvariant());
            if (!_util.IsValidNumber(number)) return null;

            // Полезно ограничить типы (мобильный/фиксированный) по бизнес-правилам:
            // var type = _util.GetNumberType(number); // Mobile, FixedLine, Voip, etc.

            return _util.Format(number, PhoneNumberFormat.E164);
        }
        catch
        {
            return null;
        }
    }

    public string NormalizeToE164OrThrow(string raw, string defaultRegion)
    {
        var normalized = NormalizeToE164(raw, defaultRegion);
        if (normalized is null)
            throw new ArgumentException("Phone number is invalid or cannot be normalized to E.164.", nameof(raw));
        return normalized;
    }
}
