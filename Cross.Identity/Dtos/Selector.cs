namespace Cross.Identity.Dtos;

/// <summary>
/// Identity selector: bag always uses <c>collectForm.Field</c> / <c>collectForm.Value</c>.
/// After <see cref="Resolve"/>, use the returned <c>Field</c> / <c>Value</c>.
/// <para>
/// On <c>collectForm</c>, JSON <c>candidates</c> binds the slot (first non-empty form field wins).
/// Other steps just call <see cref="Resolve"/> — no JSON config needed.
/// </para>
/// </summary>
public sealed record Selector
{
    /// <summary>Bag key for the resolved identity field name.</summary>
    public string FieldKey => "collectForm.Field";

    /// <summary>Bag key for the resolved identity value.</summary>
    public string ValueKey => "collectForm.Value";

    /// <summary>Whether to fail if the user is not found (consumers that honor it).</summary>
    public bool Required => true;

    /// <summary>Whether to ignore case in server-side comparison (if applicable).</summary>
    public bool CaseInsensitive => true;

    /// <summary>
    /// Preference-ordered form field keys (first non-empty wins). Written as the lookup field name.
    /// </summary>
    public IReadOnlyList<string>? Candidates { get; init; }

    /// <summary>Parse <c>collectForm</c> selector object (<c>candidates</c> / …).</summary>
    public static Selector FromJson(JsonElement el)
        => new()
        {
            Candidates = CandidatesOpt(el)
        };

    public static Selector? TryFromStepJson(JsonElement cfg)
    {
        if (!cfg.TryGetProperty("selector", out var el) || el.ValueKind != JsonValueKind.Object)
            return null;
        return FromJson(el);
    }

    private static IReadOnlyList<string>? CandidatesOpt(JsonElement el)
    {
        if (!el.TryGetProperty("candidates", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return null;

        return arr.EnumerateArray()
            .Select(e => e.GetString() ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
    }

    /// <summary>
    /// Read field name + value from <see cref="FieldKey"/> / <see cref="ValueKey"/>.
    /// </summary>
    public (string Field, string Value) Resolve(Bag ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return (ctx.Get<string>(FieldKey), ctx.Get<string>(ValueKey));
    }

    /// <summary>
    /// Write field name + value into <see cref="FieldKey"/> / <see cref="ValueKey"/>.
    /// Relative <see cref="Candidates"/> are qualified under <c>collectForm</c>.
    /// </summary>
    public void Bind(Bag ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        if (Candidates is not { Count: > 0 })
            throw new InvalidOperationException("selector bind requires 'candidates' (collectForm).");

        const string slot = "collectForm";

        string? chosenKey = null;
        string? chosenValue = null;
        foreach (var candidate in Candidates)
        {
            var bagKey = BagKey.Qualify(slot, candidate);
            if (!ctx.TryGet<string?>(bagKey, out var v) || string.IsNullOrWhiteSpace(v))
                continue;
            chosenKey = candidate;
            chosenValue = v;
            break;
        }

        if (chosenKey is null || chosenValue is null)
            throw new ValidationException("Provide an email, phone, or user name.");

        ctx.Set(FieldKey, LastSegment(chosenKey));
        ctx.Set(ValueKey, chosenValue);
    }

    public static Selector DefaultFor(ChannelEnum channel)
        => channel switch
        {
            ChannelEnum.Email => new Selector { Candidates = new[] { "Email" } },

            _ when channel.IsPhoneChannel() => new Selector { Candidates = new[] { "PhoneNumber" } },

            _ => new Selector { Candidates = new[] { "UserName" } }
        };

    /// <summary>
    /// Legacy fallback: login field → default channel when no user endpoints are available.
    /// Prefer <see cref="ICommunicationEndpointService.ResolveDeliveryChannelAsync"/>.
    /// </summary>
    public static ChannelEnum? ChannelForField(string field)
        => field.ToLowerInvariant() switch
        {
            "email" => ChannelEnum.Email,
            "phonenumber" or "phone" => ChannelEnum.Sms,
            _ => null
        };

    private static string LastSegment(string key)
    {
        var i = key.LastIndexOf('.');
        return i < 0 ? key : key[(i + 1)..];
    }
}
