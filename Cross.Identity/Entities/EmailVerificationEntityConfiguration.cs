namespace Cross.Identity.Entities;

public class EmailVerificationEntityConfiguration : IEntityTypeConfiguration<EmailVerificationEntity>
{
    public void Configure(EntityTypeBuilder<EmailVerificationEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.EmailVerifications), IdentityContext.DefaultSchema);

        builder.Property(x => x.Id).HasColumnName("EmailVerificationId");
        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
        builder.Property(x => x.TokenHash).IsRequired().HasColumnType("binary(32)");
        builder.Property(x => x.TokenLength).IsRequired();
        builder.Property(x => x.Attempts).IsRequired();
        builder.Property(x => x.MaxAttempts).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnType("datetime2(7)");
        builder.Property(x => x.UsedAt).HasColumnType("datetime2(7)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasKey(x => x.Id).HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.EmailVerifications)}");
        builder.HasIndex(x => x.UserAccountId).HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.EmailVerifications)}_UserAccount");
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.EmailVerifications)}_ExpiresAt");
        builder.HasIndex(x => x.TokenHash).HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.EmailVerifications)}_TokenHash");
    }
}
