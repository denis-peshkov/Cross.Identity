namespace Cross.Identity.Entities;

internal class ProviderEntityConfiguration : IEntityTypeConfiguration<ProviderEntity>
{
    public void Configure(EntityTypeBuilder<ProviderEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.Providers), IdentityContext.DefaultSchema);

        builder.Property(x => x.Id).HasColumnName("ProviderId");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Scheme).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2(7)");

        builder.HasKey(x => x.Id).HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.Providers)}");
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.Providers)}_Name");
        builder.HasIndex(x => x.Scheme).IsUnique().HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.Providers)}_Scheme");
    }
}
