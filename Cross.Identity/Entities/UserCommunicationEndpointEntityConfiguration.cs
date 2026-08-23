namespace Cross.Identity.Entities;

internal class UserCommunicationEndpointEntityConfiguration : IEntityTypeConfiguration<UserCommunicationEndpointEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserCommunicationEndpointEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.UsersCommunicationEndpoints), IdentityContext.DefaultSchema);
        builder.Property(x => x.Id).HasColumnName("UserCommunicationEndpointId");
        builder.Property(x => x.Channel).HasColumnType("smallint");
        builder.Property(x => x.Source).HasColumnType("smallint");
        builder.Property(x => x.Address).HasMaxLength(320).IsRequired();
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}");

        builder.HasIndex(x => new { x.UserAccountId, x.Channel, x.Address })
            .IsUnique()
            .HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}_User_Channel_Address");

        builder.HasIndex(x => x.UserAccountId, $"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}_UserAccountId")
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}_UserAccountId");

        builder.HasIndex(x => x.UserAccountId, $"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}_User_Preferred")
            .IsUnique()
            .HasFilter("[IsPreferred] = 1")
            .HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}_User_Preferred");

        builder.HasIndex(x => x.EntityId)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}_EntityId");

        builder.HasOne(x => x.UserAccount)
            .WithMany(x => x.CommunicationEndpoints)
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName($"FK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}_UserAccount");
    }
}
