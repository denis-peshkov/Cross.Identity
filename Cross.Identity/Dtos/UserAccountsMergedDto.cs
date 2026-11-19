
namespace Cross.Identity.Dtos;

/// <summary>
/// Result of merging absorbed account B into survivor account A after an external-login link conflict.
/// </summary>
public sealed class UserAccountsMergedDto
{
    /// <summary>Survivor account id (A) — kept active.</summary>
    public Guid SurvivorUserAccountId { get; init; }

    /// <summary>Absorbed account id (B) — deactivated.</summary>
    public Guid AbsorbedUserAccountId { get; init; }

    /// <summary>Provider names whose external logins were moved from B to A.</summary>
    public IReadOnlyList<string> MovedProviders { get; init; } = Array.Empty<string>();
}
