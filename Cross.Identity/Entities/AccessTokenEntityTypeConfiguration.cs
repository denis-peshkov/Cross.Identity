namespace Cross.Identity.Entities;

internal class AccessTokenEntityTypeConfiguration : IEntityTypeConfiguration<AccessTokenEntity>
{
    public void Configure(EntityTypeBuilder<AccessTokenEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.AccessTokens), IdentityContext.DefaultSchema);
        builder.Property(x => x.Id).HasColumnName("AccessTokenId");
        builder.Property(x => x.RevokedReason).HasConversion<short?>();
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.AccessTokens)}");
        builder.HasIndex(x => x.TokenHash)
            .IsUnique(false)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.AccessTokens)}_TokenHash");
    }
}
