namespace Cross.Identity.Entities;

internal class AuditEntityConfiguration : IEntityTypeConfiguration<AuditEntity>
{
    public void Configure(EntityTypeBuilder<AuditEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.Audits), IdentityContext.DefaultSchema);
        builder.Property(x => x.Id).HasColumnName("AuditId");
        builder.Property(x => x.Operation).HasConversion<short>();
        builder.Property(x => x.EntityType).HasConversion<short>();
        builder.Property(x => x.RevokedReason).HasConversion<short?>();
        builder.Property(x => x.EntityId).HasMaxLength(64);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.DeviceFingerprint).HasMaxLength(128);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.Audits)}");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.Audits)}_CreatedAt");

        builder.HasIndex(x => x.UserAccountId)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.Audits)}_UserAccountId");

        builder.HasIndex(x => x.Operation)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.Audits)}_Operation");

        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.Audits)}_Entity");

        builder.HasOne(x => x.UserAccount)
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName($"FK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.Audits)}_UserAccount");
    }
}
