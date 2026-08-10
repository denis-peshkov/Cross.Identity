namespace Cross.Identity.Services;

internal sealed class AuditService : IAuditService
{
    private readonly IdentityContext _context;

    public AuditService(IdentityContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public void Record(AuditEntity audit)
    {
        ArgumentNullException.ThrowIfNull(audit);

        if (audit.Id == Guid.Empty)
            audit.Id = Guid.NewGuid();
        if (audit.CreatedAt == default)
            audit.CreatedAt = DateTime.UtcNow;

        _context.Audits.Add(audit);
    }

    /// <inheritdoc />
    public void RecordTokenRevoked(
        Guid userAccountId,
        AuditEntityType entityType,
        Guid entityId,
        RefreshTokenRevokedReason? reason,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceFingerprint = null,
        string? notes = null)
    {
        Record(new AuditEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UserAccountId = userAccountId,
            UserAccount = null!,
            Operation = MapRevokeOperation(reason),
            EntityType = entityType,
            EntityId = entityId.ToString(),
            RevokedReason = reason,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint,
            Notes = notes,
        });
    }

    /// <inheritdoc />
    public void RecordTokenIssued(
        Guid userAccountId,
        AuditEntityType entityType,
        Guid entityId,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceFingerprint = null,
        string? notes = null)
    {
        Record(new AuditEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UserAccountId = userAccountId,
            UserAccount = null!,
            Operation = AuditOperation.TokenIssued,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint,
            Notes = notes,
        });
    }

    private static AuditOperation MapRevokeOperation(RefreshTokenRevokedReason? reason)
        => reason switch
        {
            RefreshTokenRevokedReason.USER_LOGOUT => AuditOperation.Logout,
            RefreshTokenRevokedReason.USER_LOGOUT_ALL => AuditOperation.LogoutAll,
            RefreshTokenRevokedReason.ROTATION_REQUIRED => AuditOperation.TokenRefreshed,
            _ => AuditOperation.TokenRevoked,
        };
}
