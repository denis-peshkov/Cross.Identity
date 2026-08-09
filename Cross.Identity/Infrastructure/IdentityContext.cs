namespace Cross.Identity.Infrastructure;

public class IdentityContext : DbContext
{
    public static string DefaultSchema => "auth";

    public DbSet<AccessTokenEntity> AccessTokens { get; set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
    public DbSet<ProviderEntity> Providers  { get; set; }
    public DbSet<EmailVerificationEntity> EmailVerifications  { get; set; }
    public DbSet<PhoneVerificationEntity> PhoneVerifications  { get; set; }
    public DbSet<UserAccountEntity> UsersAccounts { get; set; }
    public DbSet<UserExternalLoginEntity> UsersExternalLogins  { get; set; }
    public DbSet<UserCommunicationEndpointEntity> UsersCommunicationEndpoints { get; set; }
    public DbSet<ExternalLoginStateEntity> ExternalLoginStates { get; set; }

    public IdentityContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!HasConcurrencyStampInterceptor(optionsBuilder))
        {
            optionsBuilder.AddInterceptors(ConcurrencyStampInterceptor.Instance);
        }

        base.OnConfiguring(optionsBuilder);
    }

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