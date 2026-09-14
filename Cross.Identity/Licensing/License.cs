namespace Cross.Identity.Licensing;

internal sealed class License
{
    internal License(params Claim[] claims)
        : this(new ClaimsPrincipal(new ClaimsIdentity(claims)))
    {
    }

    public License(ClaimsPrincipal claims)
    {
        if (Guid.TryParse(claims.FindFirst("sub_id")?.Value, out var subscriptionId))
        {
            SubscriptionId = subscriptionId;
        }

        if (Guid.TryParse(claims.FindFirst("user_id")?.Value, out var userId))
        {
            UserId = userId;
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

        if (Enum.TryParse<EditionEnum>(claims.FindFirst("edition")?.Value, out var edition))
        {
            Edition = edition;
        }

        // License JWT uses dotted product names (e.g. Cross.Identity); enum members use underscores.
        var typeClaim = claims.FindFirst("type")?.Value?.Replace('.', '_');
        if (Enum.TryParse<ProductTypeEnum>(typeClaim, out var productType))
        {
            ProductType = productType;
        }

        IsConfigured = SubscriptionId != null
                       && UserId != null
                       && NotBeforeDate != null
                       && StartDate != null
                       && ExpirationDate != null
                       && Edition != null
                       && ProductType != null;
    }

    public Guid? UserId { get; }
    public Guid? SubscriptionId { get; }
    public DateTimeOffset? StartDate { get; }
    public DateTimeOffset? NotBeforeDate { get; }
    public DateTimeOffset? ExpirationDate { get; }
    public EditionEnum? Edition { get; }
    public ProductTypeEnum? ProductType { get; }

    public bool IsConfigured { get; }
}
