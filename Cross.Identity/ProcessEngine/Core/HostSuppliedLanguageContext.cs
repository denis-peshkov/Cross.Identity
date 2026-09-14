namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Preferred notification template language supplied by the host, carried through flows.
/// </summary>
/// <param name="LanguageCode">
/// Optional ISO 639-1 code (exactly two letters), e.g. <c>en</c>, <c>ru</c>, <c>ro</c>.
/// </param>
/// <remarks>
/// <para>
/// Cross.Identity does not read <c>Accept-Language</c> or ambient culture. The <b>host</b> sets
/// <c>collectForm.LanguageCode</c> in the flow bag (or constructs <c>new HostSuppliedLanguageContext(...)</c>)
/// before calling the library. Flow steps that load templates use <see cref="Read"/> and
/// <see cref="ResolveTemplate"/>.
/// </para>
/// <para>
/// When the code is missing/invalid, or the template file for that language is absent,
/// resolution falls back to <see cref="DefaultLanguageCode"/> (<c>en</c>).
/// </para>
/// </remarks>
public sealed record HostSuppliedLanguageContext(string? LanguageCode)
{
    /// <summary>Default template language when bag value is missing or invalid.</summary>
    public const string DefaultLanguageCode = "en";

    /// <summary><c>LanguageCode</c> is null or whitespace.</summary>
    public static HostSuppliedLanguageContext Empty { get; } = new HostSuppliedLanguageContext((string?)null);

    /// <summary>Bag prefix of collectForm (<c>CollectFormStepFactory.GetKind</c>).</summary>
    public const string CollectFormKind = HostSuppliedClientContext.CollectFormKind;

    /// <summary><c>collectForm.LanguageCode</c> (exactly 2 letters when present).</summary>
    public const string LanguageCodeField = "LanguageCode";

    /// <summary><c>true</c> when <see cref="LanguageCode"/> is null or whitespace.</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(LanguageCode);

    /// <summary>Reads host-supplied language from <c>collectForm.LanguageCode</c>.</summary>
    public static HostSuppliedLanguageContext Read(Bag bag)
    {
        ArgumentNullException.ThrowIfNull(bag);

        var key = BagKey.Qualify(CollectFormKind, LanguageCodeField);
        return bag.TryGet<string?>(key, out var value)
            ? new HostSuppliedLanguageContext(value)
            : Empty;
    }

    /// <summary>
    /// Normalized two-letter language code, or <see cref="DefaultLanguageCode"/> when missing/invalid.
    /// </summary>
    public string ResolveLanguageCode()
    {
        if (string.IsNullOrWhiteSpace(LanguageCode))
        {
            return DefaultLanguageCode;
        }

        var code = LanguageCode.Trim();
        if (code.Length != 2 || !code.All(char.IsLetter))
        {
            return DefaultLanguageCode;
        }

        return code.ToLowerInvariant();
    }

    /// <summary>
    /// Loads a template for <see cref="ResolveLanguageCode"/>; if missing, falls back to
    /// <see cref="DefaultLanguageCode"/>.
    /// </summary>
    internal string ResolveTemplate(
        IProcessDefinitionProvider provider,
        string name,
        string format)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        var language = ResolveLanguageCode();
        try
        {
            return provider.GetTemplate(name, language, format);
        }
        catch (KeyNotFoundException) when (!language.Equals(DefaultLanguageCode, StringComparison.OrdinalIgnoreCase))
        {
            return provider.GetTemplate(name, DefaultLanguageCode, format);
        }
    }
}
