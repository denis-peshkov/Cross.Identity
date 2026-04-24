namespace Cross.Identity.UnitTests.Entities;

[TestFixture]
public sealed class EntitiesAndJwtKeysTests
{
    [Test]
    public void ProviderEntity_And_UserExternalLoginEntity_ShouldSetProperties()
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
            Id = 123,
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
            LastUsedAt = DateTime.UtcNow
        };

        provider.ExternalLogins.Add(login);

        provider.ExternalLogins.Should().ContainSingle();
        login.ProviderEntity.Name.Should().Be("Google");
        login.UserAccount.Should().BeSameAs(user);
        login.ProviderEmail.Should().Be("u@example.com");
    }

    [Test]
    public void JwtKeys_GetRsaKey_ShouldReturnKeyWithKeyId()
    {
        var key = JwtKeys.GetRsaKey();
        key.Should().NotBeNull();
        key.KeyId.Should().NotBeNullOrWhiteSpace();
        key.Rsa.Should().NotBeNull();
    }
}
