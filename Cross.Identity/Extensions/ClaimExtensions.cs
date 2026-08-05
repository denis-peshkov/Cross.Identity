namespace Cross.Identity.Extensions;

public static class ClaimExtensions
{
    /// <summary>
    /// Adds a claim only when the value is non-empty (null/empty string ignored).
    /// </summary>
    public static List<Claim> AddIfNotNull(this List<Claim> claims, string claimType, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            claims.Add(new Claim(claimType, value));

        return claims;
    }
}
