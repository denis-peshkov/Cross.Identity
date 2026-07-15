namespace Cross.Identity.Dtos;

/// <summary>
/// User lookup settings for <see cref="CodeAuthStep"/>.
/// </summary>
public sealed record ResolveBy
{
    /// <summary>Field to look up the user by (e.g. "Email", "Phone", "UserName", "Id").</summary>
    public required string Field { get; init; }

    /// <summary>Whether to fail if the user is not found.</summary>
    public bool Required { get; init; } = true;

    /// <summary>Whether to ignore case in server-side comparison (if applicable).</summary>
    public bool CaseInsensitive { get; init; } = true;

    public static ResolveBy FromJson(JsonElement resolveEl)
        => new()
        {
            Field = resolveEl.Str("field"),
            Required = resolveEl.BoolOpt("required") ?? true,
            CaseInsensitive = resolveEl.BoolOpt("caseInsensitive") ?? true
        };

    public static ResolveBy DefaultFor(ChannelEnum channel)
        => channel switch
        {
            ChannelEnum.Email    => new ResolveBy { Field = "Email" },

            ChannelEnum.Telegram or
            ChannelEnum.Viber    or
            ChannelEnum.WatsApp  or
            ChannelEnum.Sms      => new ResolveBy { Field = "PhoneNumber" },

            _                    => new ResolveBy { Field = "UserName" }
        };
}
