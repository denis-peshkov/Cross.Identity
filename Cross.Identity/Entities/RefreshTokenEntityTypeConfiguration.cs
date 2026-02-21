namespace Cross.Identity.Entities;

public class RefreshTokenEntityTypeConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.RefreshTokens), IdentityContext.DefaultSchema);

        builder.Property(x => x.Id).HasColumnName("RefreshTokenId");

        builder.Property(x => x.RevokeReason).HasColumnType("smallint");

        builder.Property(x => x.RowVersion)
            .IsRowVersion()           // для SQL Server → rowversion/timestamp
            .IsConcurrencyToken();    // говорить EF: проверяй при UPDATE

        builder.HasKey(u => u.Id).HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.RefreshTokens)}");
        builder.HasIndex(x => x.TokenHash).IsUnique(false);
    }
}
