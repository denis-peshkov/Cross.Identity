namespace Cross.Identity.Infrastructure;

/// <summary>
/// EF Core database context for Cross.Identity auth schema (<c>auth.*</c> tables).
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
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!HasConcurrencyStampInterceptor(optionsBuilder))
        {
            optionsBuilder.AddInterceptors(ConcurrencyStampInterceptor.Instance);
        }

        base.OnConfiguring(optionsBuilder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityContext).Assembly);
    }

    private static bool HasConcurrencyStampInterceptor(DbContextOptionsBuilder optionsBuilder)
    {
        var core = optionsBuilder.Options.FindExtension<CoreOptionsExtension>();
        return core?.Interceptors?.OfType<ConcurrencyStampInterceptor>().Any() == true;
    }
}
