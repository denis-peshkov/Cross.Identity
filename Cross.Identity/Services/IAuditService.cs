namespace Cross.Identity.Services;

/// <summary>
/// Append-only audit writer against the shared <see cref="IdentityContext"/>.
/// Does not call <c>SaveChanges</c> — the caller persists.
/// </summary>
internal interface IAuditService
{
    /// <summary>Enqueue a fully built audit row.</summary>
    void Record(AuditEntity audit);

    /// <summary>
    /// Enqueue a token-revoke audit using existing <see cref="AuditEntity"/> fields
    /// (<see cref="AuditEntity.RevokedReason"/>, <see cref="AuditEntity.IpAddress"/>, …;
    /// <see cref="AuditEntity.CreatedAt"/> is the revoke time).
    /// </summary>
    void RecordTokenRevoked(
        Guid userAccountId,
        AuditEntityType entityType,
        Guid entityId,
        RefreshTokenRevokedReason? reason,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceFingerprint = null,
        string? notes = null);

    /// <summary>
    /// Enqueue a token-issue audit (client IP / UA / fingerprint on <see cref="AuditEntity"/>).
    /// </summary>
    void RecordTokenIssued(
        Guid userAccountId,
        AuditEntityType entityType,
        Guid entityId,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceFingerprint = null,
        string? notes = null);
}
