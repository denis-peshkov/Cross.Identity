namespace Cross.Identity.UnitTests.Services;

[TestFixture]
public class JwtTokenService_Tests : EFTestsBase
{
    private JwtTokenService _jwtTokenService = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessor = null!;
    private const string SignKeyBase64 = "tTPm5yP2Q+1m7UQlM3N2AVnleqk7D4HhR0YzF9o5+Xw="; // 32+ bytes
    private const string EncKeyBase64 = "r9lZJcR8CdpqgGgxP1VbUk2OQhlnwFJSwVOrMDyk4Lc="; // 32 bytes

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        var options = new Mock<IOptionsSnapshot<AuthenticationOptions>>();
        options.Setup(o => o.Value).Returns(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Issuer = "test-issuer",
                Audience = "test-audience",
                Key = SignKeyBase64,
                EncryptionKey = EncKeyBase64,
                UseEncryption = false,
                AccessTokenExpires = TimeSpan.FromMinutes(15),
                RefreshTokenExpires = TimeSpan.FromMinutes(60),
                RefreshTokenAbsoluteExpires = TimeSpan.FromDays(30),
            }
        });

        _jwtTokenService = new JwtTokenService(Context, options.Object, _httpContextAccessor.Object);
    }

    [Test]
    public void Constructor_ShouldThrow_WhenEncryptionKeyNot32Bytes()
    {
        var options = new Mock<IOptionsSnapshot<AuthenticationOptions>>();
        options.Setup(o => o.Value).Returns(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Key = SignKeyBase64,
                EncryptionKey = Convert.ToBase64String(new byte[16]),
                UseEncryption = false,
            }
        });

        var act = () => new JwtTokenService(Context, options.Object, _httpContextAccessor.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt.EncryptionKey must be 32 bytes*");
    }

    [Test]
    public void Constructor_ShouldThrow_WhenSignKeyLessThan32Bytes()
    {
        var options = new Mock<IOptionsSnapshot<AuthenticationOptions>>();
        options.Setup(o => o.Value).Returns(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Key = Convert.ToBase64String(new byte[16]),
                EncryptionKey = EncKeyBase64,
                UseEncryption = false,
            }
        });

        var act = () => new JwtTokenService(Context, options.Object, _httpContextAccessor.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt.Key should be at least 32 bytes*");
    }

    [Test]
    public async Task GenerateIdTokenAsync_ShouldReturnValidToken()
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()) };

        var token = await _jwtTokenService.GenerateIdTokenAsync(claims);

        token.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task GenerateAccessTokenAsync_ShouldPersistAndReturnToken()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var permissions = new List<string> { "read" };
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId.ToString()) };

        var token = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, permissions, claims);

        token.Should().NotBeNullOrEmpty();
        var entity = await Context.AccessTokens.FirstOrDefaultAsync(x => x.UserId == userId);
        entity.Should().NotBeNull();
        entity!.TokenHash.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task GenerateRefreshTokenAsync_ShouldPersistAndReturnToken()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId.ToString()) };

        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, claims);

        token.Should().NotBeNullOrEmpty();
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var entity = await Context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
        entity.Should().NotBeNull();
    }

    [Test]
    public async Task ValidateAccessTokenAsync_ShouldReturnTrue_WhenTokenValid()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), new List<Claim>());

        var result = await _jwtTokenService.ValidateAccessTokenAsync(token);

        result.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAccessTokenAsync_ShouldReturnFalse_WhenTokenExpired()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), new List<Claim>());
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserId == userId);
        entity.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await Context.SaveChangesAsync();

        var result = await _jwtTokenService.ValidateAccessTokenAsync(token);

        result.Should().BeFalse();
    }

    [Test]
    public async Task ValidateAccessTokenAsync_ShouldReturnFalse_WhenTokenRevoked()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), new List<Claim>());
        await _jwtTokenService.RevokeAccessTokenAsync((await Context.AccessTokens.FirstAsync(x => x.UserId == userId)).Id);

        var result = await _jwtTokenService.ValidateAccessTokenAsync(token);

        result.Should().BeFalse();
    }

    [Test]
    public async Task ValidateAccessTokenAsync_ShouldReturnFalse_WhenTokenNotInDb()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), new List<Claim>());
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserId == userId);
        Context.AccessTokens.Remove(entity);
        await Context.SaveChangesAsync();

        var result = await _jwtTokenService.ValidateAccessTokenAsync(token);

        result.Should().BeFalse();
    }

    [Test]
    public async Task ValidateRefreshTokenAsync_ShouldReturnTrue_WhenTokenValid()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());

        var result = await _jwtTokenService.ValidateRefreshTokenAsync(token);

        result.Should().BeTrue();
    }

    [Test]
    public async Task ValidateRefreshTokenAsync_ShouldReturnFalse_WhenTokenNotFound()
    {
        var result = await _jwtTokenService.ValidateRefreshTokenAsync("not-a-valid-token-string");

        result.Should().BeFalse();
    }

    [Test]
    public async Task RevokeAccessTokenAsync_ShouldSetRevokedAt()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), new List<Claim>());
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserId == userId);

        await _jwtTokenService.RevokeAccessTokenAsync(entity.Id);

        await Context.Entry(entity).ReloadAsync();
        entity.RevokedAt.Should().NotBeNull();
    }

    [Test]
    public async Task RevokeAccessTokenAsync_WhenEntryNull_ShouldNotThrow()
    {
        var act = () => _jwtTokenService.RevokeAccessTokenAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task CleanupExpiredAccessTokensAsync_ShouldRemoveExpiredTokens()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), new List<Claim>());
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserId == userId);
        entity.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await Context.SaveChangesAsync();

        await _jwtTokenService.CleanupExpiredAccessTokensAsync();

        var found = await Context.AccessTokens.FirstOrDefaultAsync(x => x.Id == entity.Id);
        found.Should().BeNull();
    }

    [Test]
    public async Task GetClaimValueAsync_ShouldReturnClaimValue()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(),
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId.ToString()) });

        var value = await _jwtTokenService.GetClaimValueAsync(token, JwtRegisteredClaimNames.Sub);

        value.Should().Be(userId.ToString());
    }

    [Test]
    public async Task GetClaimValueAsync_WhenTokenNull_ShouldThrow()
    {
        var act = () => _jwtTokenService.GetClaimValueAsync(null!, "sub");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task GetRefreshTokenAsync_ShouldReturnEntity_WhenTokenExists()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());

        var entity = await _jwtTokenService.GetRefreshTokenAsync(token, CancellationToken.None);

        entity.Should().NotBeNull();
        entity!.UserId.Should().Be(userId);
    }

    [Test]
    public async Task GetRefreshTokenAsync_ShouldReturnNull_WhenTokenNotFound()
    {
        var entity = await _jwtTokenService.GetRefreshTokenAsync("not-a-valid-token", CancellationToken.None);

        entity.Should().BeNull();
    }

    [Test]
    public async Task InvalidateRefreshTokenAsync_ShouldSetReplacedByAndRevokedAt()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var oldToken = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());
        var newJti = Guid.NewGuid().ToString();

        await _jwtTokenService.InvalidateRefreshTokenAsync(oldToken, newJti, CancellationToken.None);

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(oldToken)));
        var entity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == hash);
        entity.RevokedAt.Should().NotBeNull();
        entity.ReplacedByTokenId.Should().Be(Guid.Parse(newJti));
    }

    [Test]
    public async Task InvalidateRefreshTokenAsync_WhenTokenNotFound_ShouldThrow()
    {
        var act = () => _jwtTokenService.InvalidateRefreshTokenAsync("nonexistent-token", Guid.NewGuid().ToString(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Refresh token not found*");
    }

    [Test]
    public void AccessTokenExpiresInSeconds_ShouldReturnPositive()
    {
        _jwtTokenService.AccessTokenExpiresInSeconds.Should().Be(15 * 60);
    }
}
