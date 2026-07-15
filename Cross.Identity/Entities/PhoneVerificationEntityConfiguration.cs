namespace Cross.Identity.Entities;

internal class PhoneVerificationEntityConfiguration : IEntityTypeConfiguration<PhoneVerificationEntity>
{
    public void Configure(EntityTypeBuilder<PhoneVerificationEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.PhoneVerifications), IdentityContext.DefaultSchema);

        builder.Property(x => x.Id).HasColumnName("PhoneVerificationId");
        builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20); // E.164
        builder.Property(x => x.CodeHash).IsRequired().HasColumnType("binary(32)");
        builder.Property(x => x.CodeLength).IsRequired();
        builder.Property(x => x.Attempts).IsRequired();
        builder.Property(x => x.MaxAttempts).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnType("datetime2(7)");
        builder.Property(x => x.UsedAt).HasColumnType("datetime2(7)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(7)");

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.PhoneVerifications)}");
        builder.HasIndex(x => x.UserAccountId)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.PhoneVerifications)}_UserAccount");
        builder.HasIndex(x => x.CodeHash)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.PhoneVerifications)}_CodeHash");
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.PhoneVerifications)}_ExpiresAt");
    }
}
