namespace Cross.Identity.Entities;

internal class UserExternalLoginEntityConfiguration : IEntityTypeConfiguration<UserExternalLoginEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserExternalLoginEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.UsersExternalLogins), IdentityContext.DefaultSchema);
        builder.Property(x => x.Id).HasColumnName("UserExternalLoginId");
        builder.Property(x => x.ProviderUserId).IsRequired();
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersExternalLogins)}");
        builder.HasIndex(x => x.UserAccountId)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersExternalLogins)}_UserAccountId");
        builder.HasIndex(x => new { x.ProviderId, x.ProviderUserId })
            .IsUnique()
            .HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersExternalLogins)}_Provider_User");
        builder.HasIndex(x => x.ProviderUserId)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersExternalLogins)}_ProviderUserId");
    }
}
