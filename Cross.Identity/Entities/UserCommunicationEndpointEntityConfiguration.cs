namespace Cross.Identity.Entities;

internal class UserCommunicationEndpointEntityConfiguration : IEntityTypeConfiguration<UserCommunicationEndpointEntity>
{
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

        builder.HasIndex(x => new { x.UserId, x.Channel, x.Address })
            .IsUnique()
            .HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}_User_Channel_Address");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName($"IX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}_UserId");

        builder.HasOne(x => x.UserAccount)
            .WithMany(x => x.CommunicationEndpoints)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName($"FK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}_User");
    }
}
