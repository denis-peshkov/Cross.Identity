namespace Cross.Identity.Entities;

internal class RefreshTokenEntityTypeConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.RefreshTokens), IdentityContext.DefaultSchema);
        builder.Property(x => x.Id).HasColumnName("RefreshTokenId");
        builder.Property(x => x.RevokeReason).HasColumnType("smallint");
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken(); // tell EF to check on UPDATE

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.RefreshTokens)}");
        builder.HasIndex(x => x.TokenHash)
            .IsUnique(false)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.RefreshTokens)}_TokenHash");
    }
}
