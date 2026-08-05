namespace Cross.Identity.Entities;

internal class EmailVerificationEntityConfiguration : IEntityTypeConfiguration<EmailVerificationEntity>
{
    public void Configure(EntityTypeBuilder<EmailVerificationEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.EmailVerifications), IdentityContext.DefaultSchema);

        builder.Property(x => x.Id).HasColumnName("EmailVerificationId");
        builder.Property(x => x.Email).IsRequired();
        builder.Property(x => x.TokenHash).IsRequired();
        builder.Property(x => x.TokenLength).IsRequired();
        builder.Property(x => x.Attempts).IsRequired();
        builder.Property(x => x.MaxAttempts).IsRequired();
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.EmailVerifications)}");
        builder.HasIndex(x => x.UserAccountId)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.EmailVerifications)}_UserAccount");
        builder.HasIndex(x => x.Email)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.EmailVerifications)}_Email");
        builder.HasIndex(x => x.TokenHash)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.EmailVerifications)}_TokenHash");
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.EmailVerifications)}_ExpiresAt");
    }
}
