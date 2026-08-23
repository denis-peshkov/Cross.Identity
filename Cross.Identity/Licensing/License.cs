namespace Cross.Identity.Licensing;

internal sealed class License
{
    internal License(params Claim[] claims)
        : this(new ClaimsPrincipal(new ClaimsIdentity(claims)))
    {
    }

    internal License()
    {
        IsConfigured = false;
    }

    public License(ClaimsPrincipal claims)
    {
        if (Guid.TryParse(claims.FindFirst("sub_id")?.Value, out var subscriptionId))
        {
            SubscriptionId = subscriptionId;
        }

        if (Guid.TryParse(claims.FindFirst("user_id")?.Value, out var userAccountId))
        {
            UserAccountId = userAccountId;
        }

        if (long.TryParse(claims.FindFirst("iat")?.Value, out var iat))
        {
            StartDate = DateTimeOffset.FromUnixTimeSeconds(iat);
        }

        if (long.TryParse(claims.FindFirst("nbf")?.Value, out var nbf))
        {
            NotBeforeDate = DateTimeOffset.FromUnixTimeSeconds(nbf);
        }

        if (long.TryParse(claims.FindFirst("exp")?.Value, out var exp))
        {
            ExpirationDate = DateTimeOffset.FromUnixTimeSeconds(exp);
        }

        if (Enum.TryParse(claims.FindFirst("edition")?.Value, out EditionEnum edition))
        {
            Edition = edition;
        }

        if (Enum.TryParse(claims.FindFirst("type")?.Value, out ProductTypeEnum productType))
        {
            ProductType = productType;
        }

        IsConfigured = SubscriptionId != null
                       && UserAccountId != null
                       && NotBeforeDate != null
                       && StartDate != null
                       && ExpirationDate != null
                       && Edition != null
                       && ProductType != null;
    }

    public Guid? UserAccountId { get; }

    public Guid? SubscriptionId { get; }

    public DateTimeOffset? StartDate { get; }

    public DateTimeOffset? NotBeforeDate { get; }

    public DateTimeOffset? ExpirationDate { get; }

    public EditionEnum? Edition { get; }

    public ProductTypeEnum? ProductType { get; }

    public bool IsConfigured { get; }
}
