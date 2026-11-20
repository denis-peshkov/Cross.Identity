namespace Cross.Identity.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Converts PascalCase/TitleCase to camelCase.
    /// Handles leading acronyms:
    /// - "SendCodeStep"  -> "sendCodeStep"
    /// - "IPAddress"     -> "ipAddress"
    /// - "URL"           -> "url"
    /// Returns the original string if already camelCase or does not start with a letter.
    /// </summary>
    public static string ToCamelCase(this string s)
    {
        if (string.IsNullOrEmpty(s))
            return s ?? string.Empty;

        if (!char.IsLetter(s[0]) || char.IsLower(s[0]))
            return s;

        int len = s.Length;

        // length of the initial run of uppercase letters (acronym)
        int run = 0;
        while (run < len && char.IsUpper(s[run]))
            run++;

        if (run == 1)
            return char.ToLowerInvariant(s[0]) + s.Substring(1);

        if (run >= len)
            return s.ToLowerInvariant(); // entire string is an acronym: "URL" -> "url"

        // "IPAddress" -> lowercase first (run-1) chars, rest unchanged
        return s.Substring(0, run - 1).ToLowerInvariant() + s.Substring(run - 1);
    }

    public static string ToCamelCase1(this string input)
        => Regex.Replace(input, "^[A-Z]", m => m.Value.ToLowerInvariant());

    public static string ToPascalCase(this string input)
        => Regex.Replace(input, "^[a-z]", m => m.Value.ToUpperInvariant());

    public static string? MaskSSN(this string? ssn)
        => string.IsNullOrEmpty(ssn)
            ? ssn
            : ssn.Length <= 5
                ? "*****"
                : string.Concat("*****", ssn.AsSpan(5));

    /// <summary>
    /// Masks an email for logs (e.g. <c>s***@e***.com</c>). Keeps first local/domain chars and TLD.
    /// </summary>
    public static string MaskEmailForLog(this string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return email ?? string.Empty;
        }

        var at = email.IndexOf('@');
        if (at <= 0 || at >= email.Length - 1)
        {
            return "*****";
        }

        var local = email.AsSpan(0, at);
        var domain = email.AsSpan(at + 1);
        var localMasked = local.Length == 1 ? "*" : string.Concat(local[0].ToString(), "***");

        var lastDot = domain.LastIndexOf('.');
        if (lastDot <= 0 || lastDot >= domain.Length - 1)
        {
            return $"{localMasked}@***";
        }

        var tld = domain[(lastDot + 1)..].ToString();
        return $"{localMasked}@{domain[0]}***.{tld}";
    }

    /// <summary>
    /// Masks a phone for logs (e.g. <c>+791****67</c>). Keeps a short prefix and last two digits.
    /// </summary>
    public static string MaskPhoneForLog(this string? phone)
    {
        if (string.IsNullOrEmpty(phone))
        {
            return phone ?? string.Empty;
        }

        if (phone.Length <= 4)
        {
            return "****";
        }

        var keepStart = Math.Min(4, phone.Length - 2);
        return string.Concat(phone.AsSpan(0, keepStart), "****", phone.AsSpan(phone.Length - 2));
    }

    /// <summary>
    /// Masks a selector identity for logs: Email / Phone / PhoneNumber → address mask; other fields unchanged.
    /// </summary>
    public static string MaskIdentityForLog(string? field, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        if (string.IsNullOrEmpty(field))
        {
            return value;
        }

        if (field.Equals("Email", StringComparison.OrdinalIgnoreCase))
        {
            return value.MaskEmailForLog();
        }

        if (field.Equals("PhoneNumber", StringComparison.OrdinalIgnoreCase)
            || field.Equals("Phone", StringComparison.OrdinalIgnoreCase))
        {
            return value.MaskPhoneForLog();
        }

        return value;
    }

    public static string ToPascalCaseWithoutSpaces(this string input)
        => Regex.Replace(input, @"\s+(.)", m => m.Groups[1].Value.ToUpperInvariant()).ToPascalCase();
}
