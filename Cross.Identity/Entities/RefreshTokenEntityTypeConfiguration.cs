namespace Cross.Identity.Entities;

internal class RefreshTokenEntityTypeConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.RefreshTokens), IdentityContext.DefaultSchema);
        builder.Property(x => x.Id).HasColumnName("RefreshTokenId");
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.RefreshTokens)}");
        builder.HasIndex(x => x.TokenHash)
            .IsUnique(false)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.RefreshTokens)}_TokenHash");
        builder.HasIndex(x => x.UserAccountId)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.RefreshTokens)}_UserAccountId");

        builder.HasOne(x => x.UserAccount)
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName($"FK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.RefreshTokens)}_UserAccount");
    }
}
