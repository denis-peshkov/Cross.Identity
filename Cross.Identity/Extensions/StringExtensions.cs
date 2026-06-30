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

    public static string ToPascalCaseWithoutSpaces(this string input)
        => Regex.Replace(input, @"\s+(.)", m => m.Groups[1].Value.ToUpperInvariant()).ToPascalCase();
}
