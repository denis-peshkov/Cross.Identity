namespace Cross.Identity.Entities;

internal class PhoneVerificationEntityConfiguration : IEntityTypeConfiguration<PhoneVerificationEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PhoneVerificationEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.PhoneVerifications), IdentityContext.DefaultSchema);

        builder.Property(x => x.Id).HasColumnName("PhoneVerificationId");
        builder.Property(x => x.PhoneNumber).IsRequired();
        builder.Property(x => x.CodeHash).IsRequired();
        builder.Property(x => x.CodeLength).IsRequired();
        builder.Property(x => x.Attempts).IsRequired();
        builder.Property(x => x.MaxAttempts).IsRequired();
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.PhoneVerifications)}");
        builder.HasIndex(x => x.UserAccountId)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.PhoneVerifications)}_UserAccount");
        builder.HasIndex(x => x.CodeHash)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.PhoneVerifications)}_CodeHash");
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.PhoneVerifications)}_ExpiresAt");

        builder.HasOne(x => x.UserAccount)
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName($"FK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.PhoneVerifications)}_UserAccount");
    }
}
