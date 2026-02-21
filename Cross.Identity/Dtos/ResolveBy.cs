namespace Cross.Identity.Dtos;

/// <summary>
/// Настройки поиска пользователя для <see cref="CodeAuthStep"/>.
/// </summary>
public sealed record ResolveBy
{
    /// <summary>Поле, по которому ищем пользователя (например, "Email", "Phone", "UserName", "Id").</summary>
    public required string Field { get; init; }

    /// <summary>Нужно ли падать с ошибкой, если пользователь не найден.</summary>
    public bool Required { get; init; } = true;

    /// <summary>Игнорировать ли регистр при сравнении на стороне сервиса (если применимо).</summary>
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
