namespace Cross.Identity.Entities;

internal class UserAccountEntityConfiguration : IEntityTypeConfiguration<UserAccountEntity>
{
    public void Configure(EntityTypeBuilder<UserAccountEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.UsersAccounts), IdentityContext.DefaultSchema);
        builder.Property(x => x.Id).HasColumnName("UserAccountId");
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasKey(x => x.Id)
            .HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersAccounts)}");
        builder.HasIndex(x => x.NormalizedUserName)
            .IsUnique()
            .HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersAccounts)}_UserName");
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersAccounts)}_Email")
            .HasFilter("[EmailConfirmed] = 1 AND [Email] IS NOT NULL");
        builder.HasIndex(x => x.PhoneNumber)
            .IsUnique()
            .HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersAccounts)}_Phone")
            .HasFilter("[PhoneNumberConfirmed] = 1 AND [PhoneNumber] IS NOT NULL");
        builder.HasMany(x => x.ExternalLogins)
            .WithOne(x => x.UserAccount)
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName($"FK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersExternalLogins)}_UserAccount");
        builder.HasMany(x => x.CommunicationEndpoints)
            .WithOne(x => x.UserAccount)
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName($"FK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersCommunicationEndpoints)}_UserAccount");
    }
}
