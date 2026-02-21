namespace Cross.Identity.Entities;

public class AccessTokenEntityTypeConfiguration : IEntityTypeConfiguration<AccessTokenEntity>
{
    public void Configure(EntityTypeBuilder<AccessTokenEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.AccessTokens), IdentityContext.DefaultSchema);

        builder.Property(x => x.Id).HasColumnName("AccessTokenId");

        builder.Property(x => x.RevokeReason).HasColumnType("smallint");

        builder.HasKey(u => u.Id).HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.AccessTokens)}");
        builder.HasIndex(x => x.TokenHash).IsUnique(false);
    }
}
