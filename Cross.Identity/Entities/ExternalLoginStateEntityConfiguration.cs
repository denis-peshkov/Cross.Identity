namespace Cross.Identity.Entities;

internal class ExternalLoginStateEntityConfiguration : IEntityTypeConfiguration<ExternalLoginStateEntity>
{
    public void Configure(EntityTypeBuilder<ExternalLoginStateEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.ExternalLoginStates), IdentityContext.DefaultSchema);

        builder.Property(x => x.Id).HasColumnName("ExternalLoginStateId");
        builder.Property(x => x.Nonce).IsRequired();
        builder.Property(x => x.Provider).IsRequired();
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.ExternalLoginStates)}");
        builder.HasIndex(x => x.Nonce)
            .IsUnique()
            .HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.ExternalLoginStates)}_Nonce");
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.ExternalLoginStates)}_ExpiresAt");
    }
}
