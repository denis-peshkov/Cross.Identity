namespace Cross.Identity.Entities;

internal class UserAccountEntityConfiguration : IEntityTypeConfiguration<UserAccountEntity>
{
    public void Configure(EntityTypeBuilder<UserAccountEntity> builder)
    {
        builder.ToTable(nameof(IdentityContext.UsersAccounts), IdentityContext.DefaultSchema);

        builder.Property(x => x.Id).HasColumnName("UserAccountId");

        builder.Property(x => x.UserName).HasMaxLength(200);
        builder.Property(x => x.NormalizedUserName).HasMaxLength(200);

        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.NormalizedEmail).HasMaxLength(200);

        builder.Property(x => x.PhoneNumber).HasMaxLength(20); // E.164

        builder.Property(x => x.PasswordPhc).HasMaxLength(800);
        // builder.Property(x => x.PasswordHash).HasColumnType("binary(32)");
        // builder.Property(x => x.PasswordSalt).HasMaxLength(200);

        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(7)");
        builder.Property(x => x.LastLoginAt).HasColumnType("datetime2(7)");
        builder.Property(x => x.LockoutEnd).HasColumnType("datetimeoffset(0)");

        builder.HasKey(x => x.Id).HasName($"PK_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersAccounts)}");
        builder.HasIndex(x => x.NormalizedUserName).IsUnique().HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersAccounts)}_UserName");
        builder.HasIndex(x => x.NormalizedEmail).IsUnique().HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersAccounts)}_Email");
        builder.HasIndex(x => x.PhoneNumber).IsUnique().HasDatabaseName($"UX_{IdentityContext.DefaultSchema}_{nameof(IdentityContext.UsersAccounts)}_Phone");
        builder.HasMany(x => x.ExternalLogins)
            .WithOne(x => x.UserAccount)
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
