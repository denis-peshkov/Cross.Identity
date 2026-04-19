namespace Cross.Identity.Entities;

internal class UserExternalLoginEntityConfiguration : IEntityTypeConfiguration<UserExternalLoginEntity>
{
    public void Configure(EntityTypeBuilder<UserExternalLoginEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.UsersExternalLogins), IdentityContext.DefaultSchema);

        builder.Property(x => x.Id).HasColumnName("UserExternalLoginId");

        builder.Property(x => x.ProviderUserId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ProviderEmail).HasMaxLength(200);
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);
        builder.Property(x => x.ProfileUrl).HasMaxLength(500);
        builder.Property(x => x.Scope).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(7)");
        builder.Property(x => x.LastUsedAt).HasColumnType("datetime2(7)");

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersExternalLogins)}");
        builder.HasIndex(x => new { x.ProviderId, x.ProviderUserId }).IsUnique()
            .HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersExternalLogins)}_Provider_User");
        builder.HasIndex(x => x.UserAccountId)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersExternalLogins)}_UserAccountId");
        builder.HasIndex(x => x.ProviderUserId)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersExternalLogins)}_ProviderUserId");
    }
}
