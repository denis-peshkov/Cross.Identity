namespace Cross.Identity.Entities;

internal class AccountMergePendingEntityConfiguration : IEntityTypeConfiguration<AccountMergePendingEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AccountMergePendingEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.AccountMergePendings), IdentityContext.DefaultSchema);

        builder.Property(x => x.Id).HasColumnName("AccountMergePendingId");
        builder.Property(x => x.MergeToken).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.AccountMergePendings)}");

        builder.HasIndex(x => x.MergeToken)
            .IsUnique()
            .HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.AccountMergePendings)}_MergeToken");

        builder.HasOne(x => x.SurvivorUserAccount)
            .WithMany()
            .HasForeignKey(x => x.SurvivorUserAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired()
            .HasConstraintName($"FK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.AccountMergePendings)}_Survivor");

        builder.HasOne(x => x.AbsorbedUserAccount)
            .WithMany()
            .HasForeignKey(x => x.AbsorbedUserAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired()
            .HasConstraintName($"FK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.AccountMergePendings)}_Absorbed");
    }
}
