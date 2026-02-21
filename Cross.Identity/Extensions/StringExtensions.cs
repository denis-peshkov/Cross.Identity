namespace Cross.Identity.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Конвертирует PascalCase/TitleCase в camelCase.
    /// Умно обрабатывает начальные акронимы:
    /// - "SendCodeStep"  -> "sendCodeStep"
    /// - "IPAddress"     -> "ipAddress"
    /// - "URL"           -> "url"
    /// Если строка уже camelCase или не буква вначале — возвращает исходную.
    /// </summary>
    public static string ToCamelCase(this string s)
    {
        if (string.IsNullOrEmpty(s))
            return s ?? string.Empty;

        if (!char.IsLetter(s[0]) || char.IsLower(s[0]))
            return s;

        int len = s.Length;

        // длина начальной серии заглавных букв (акроним)
        int run = 0;
        while (run < len && char.IsUpper(s[run]))
            run++;

        if (run == 1)
            return char.ToLowerInvariant(s[0]) + s.Substring(1);

        if (run >= len)
            return s.ToLowerInvariant(); // вся строка — акроним: "URL" -> "url"

        // "IPAddress" -> lower первых (run-1) символов, остальное без изменений
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
