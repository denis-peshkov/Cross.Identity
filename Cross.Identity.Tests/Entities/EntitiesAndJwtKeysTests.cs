namespace Cross.Identity.Tests.Entities;

[TestFixture]
public sealed class EntitiesAndJwtKeysTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenProviderAndExternalLogin_WhenCreated_ThenSetsProperties()
    {
        var user = new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var provider = new ProviderEntity
        {
            Id = 7,
            Name = "Google",
            Scheme = "google",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        var login = new UserExternalLoginEntity
        {
            Id = Guid.NewGuid(),
            UserAccountId = user.Id,
            UserAccount = user,
            ProviderId = provider.Id,
            ProviderEntity = provider,
            ProviderUserId = "ext-1",
            ProviderEmail = "u@example.com",
            DisplayName = "User",
            AvatarUrl = "https://a",
            ProfileUrl = "https://p",
            AccessTokenEnc = new byte[] { 1, 2, 3 },
            RefreshTokenEnc = new byte[] { 4, 5, 6 },
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Scope = "openid profile",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        provider.ExternalLogins.Add(login);

        provider.ExternalLogins.Should().ContainSingle();
        login.ProviderEntity.Name.Should().Be("Google");
        login.UserAccount.Should().BeSameAs(user);
        login.ProviderEmail.Should().Be("u@example.com");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenAudit_WhenCreated_ThenSetsProperties()
    {
        var userAccountId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var audit = new AuditEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = createdAt,
            UserAccountId = userAccountId,
            UserAccount = null!,
            Operation = AuditOperation.TokenRevoked,
            EntityType = AuditEntityType.RefreshToken,
            EntityId = tokenId.ToString(),
            RevokedReason = RefreshTokenRevokedReason.USER_LOGOUT,
            IpAddress = "10.0.0.1",
            UserAgent = "TestAgent/1.0",
            DeviceFingerprint = "fp-1",
            Notes = "Logout from current device",
        };

        audit.Operation.Should().Be(AuditOperation.TokenRevoked);
        audit.EntityType.Should().Be(AuditEntityType.RefreshToken);
        audit.EntityId.Should().Be(tokenId.ToString());
        audit.RevokedReason.Should().Be(RefreshTokenRevokedReason.USER_LOGOUT);
        audit.Notes.Should().Be("Logout from current device");
        audit.UserAccountId.Should().Be(userAccountId);
        audit.CreatedAt.Should().Be(createdAt);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenAudit_WhenPersisted_ThenRoundTripsThroughIdentityContext()
    {
        using var ctx = InMemoryDbHelper.CreateContext();
        var audit = new AuditEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UserAccountId = Guid.NewGuid(),
            UserAccount = null!,
            Operation = AuditOperation.PasswordChanged,
            EntityType = AuditEntityType.UserAccount,
            EntityId = Guid.NewGuid().ToString(),
            IpAddress = "127.0.0.1",
            Notes = "change-password flow",
        };

        ctx.Audits.Add(audit);
        ctx.SaveChanges();

        var loaded = ctx.Audits.Single(a => a.Id == audit.Id);
        loaded.Operation.Should().Be(AuditOperation.PasswordChanged);
        loaded.EntityType.Should().Be(AuditEntityType.UserAccount);
        loaded.Notes.Should().Be("change-password flow");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenJwtKeys_WhenGetRsaKey_ThenReturnsKeyWithKeyId()
    {
        var key = JwtKeys.GetRsaKey();
        key.Should().NotBeNull();
        key.KeyId.Should().NotBeNullOrWhiteSpace();
        key.Rsa.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenAuditEntityTypeMembers_WhenReadingNumericValues_ThenEachValueIsUnique()
    {
        var values = Enum.GetValues<AuditEntityType>().Select(x => (short)x).ToList();

        values.Should().OnlyHaveUniqueItems();
        ((short)AuditEntityType.LinkedMessenger).Should().Be(9);
        ((short)AuditEntityType.UserCommunicationEndpoint).Should().Be(7);
        ((short)AuditEntityType.ExternalLoginState).Should().Be(8);
    }
}
