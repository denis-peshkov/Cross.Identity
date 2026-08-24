namespace Cross.Identity.Infrastructure;

/// <summary>
/// EF Core database context for Cross.Identity auth schema (<c>auth.*</c> tables).
/// Rotates <see cref="IHasConcurrencyStamp.ConcurrencyStamp"/> on tracked insert/update in
/// <see cref="SaveChanges(bool)"/> / <see cref="SaveChangesAsync(bool, CancellationToken)"/>
/// so hosts need not register an interceptor (works with pooled and non-pooled registration).
/// Bulk updates (<c>ExecuteUpdateAsync</c> / <c>ExecuteDeleteAsync</c>) bypass this path and
/// must set <c>ConcurrencyStamp</c> explicitly.
/// </summary>
public class IdentityContext : DbContext
{
    /// <summary>Default SQL schema for all identity tables.</summary>
    public static string DefaultSchema => "auth";

    /// <summary>Issued access tokens (<c>jti</c> rows).</summary>
    public DbSet<AccessTokenEntity> AccessTokens { get; set; }

    /// <summary>Refresh-token rotation chains and session anchors.</summary>
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }

    /// <summary>Registered external OAuth/OIDC providers.</summary>
    public DbSet<ProviderEntity> Providers  { get; set; }

    /// <summary>Pending email verification / OTP codes.</summary>
    public DbSet<EmailVerificationEntity> EmailVerifications  { get; set; }

    /// <summary>Pending phone verification / OTP codes.</summary>
    public DbSet<PhoneVerificationEntity> PhoneVerifications  { get; set; }

    /// <summary>Local user accounts.</summary>
    public DbSet<UserAccountEntity> UsersAccounts { get; set; }

    /// <summary>Linked external provider identities per user.</summary>
    public DbSet<UserExternalLoginEntity> UsersExternalLogins  { get; set; }

    /// <summary>Verified and candidate communication endpoints per user.</summary>
    public DbSet<UserCommunicationEndpointEntity> UsersCommunicationEndpoints { get; set; }

    /// <summary>Short-lived OAuth state/nonces for external login flows.</summary>
    public DbSet<ExternalLoginStateEntity> ExternalLoginStates { get; set; }

    /// <summary>Security and token lifecycle audit rows.</summary>
    public DbSet<AuditEntity> Audits { get; set; }

    /// <summary>Creates the context with host-supplied options.</summary>
    /// <param name="options">EF Core options (provider, connection, interceptors).</param>
    public IdentityContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RotateConcurrencyStamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        RotateConcurrencyStamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityContext).Assembly);
    }

    private void RotateConcurrencyStamps()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            if (entry.Entity is IHasConcurrencyStamp stamped && stamped.ConcurrencyStamp == Guid.Empty)
            {
                stamped.ConcurrencyStamp = Guid.NewGuid();
            }
        }
    }
}
