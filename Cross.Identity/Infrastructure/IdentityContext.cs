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

    public IdentityContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityContext).Assembly);
    }
}

public class ReadonlyIdentityContext : IdentityContext
{
    private readonly IdentityContext _context;

    public DbSet<AccessTokenEntity> AccessTokens => _context.AccessTokens;
    public DbSet<RefreshTokenEntity> RefreshTokens => _context.RefreshTokens;
    public DbSet<ProviderEntity> Providers => _context.Providers;
    public DbSet<EmailVerificationEntity> EmailVerifications => _context.EmailVerifications;
    public DbSet<PhoneVerificationEntity> PhoneVerifications => _context.PhoneVerifications;
    public DbSet<UserAccountEntity> UsersAccounts => _context.UsersAccounts;
    public DbSet<UserExternalLoginEntity> UsersExternalLogins => _context.UsersExternalLogins;

    public ReadonlyIdentityContext(DbContextOptions options, IdentityContext context)
        : base(options)
    {
        _context = context;
    }
}
