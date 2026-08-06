namespace Cross.Identity.Tests.Services;

[TestFixture]
public class JwtTokenServiceTests : EFTestsBase
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
    public void GivenEncryptionKeyNot32Bytes_WhenConstructing_ThenThrowsInvalidOperationException()
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
    public void GivenSignKeyLessThan32Bytes_WhenConstructing_ThenThrowsInvalidOperationException()
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
    public async Task GivenValidClaims_WhenGenerateIdTokenAsync_ThenReturnsValidTokenAsync()
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()) };

        var token = await _jwtTokenService.GenerateIdTokenAsync(claims);

        token.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task GivenValidUser_WhenGenerateAccessTokenAsync_ThenPersistsAndReturnsTokenAsync()
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
    public async Task GivenValidUser_WhenGenerateRefreshTokenAsync_ThenPersistsAndReturnsTokenAsync()
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
    public async Task GivenValidAccessToken_WhenValidateAccessTokenAsync_ThenReturnsTrueAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), new List<Claim>());

        var result = await _jwtTokenService.ValidateAccessTokenAsync(token);

        result.Should().BeTrue();
    }

    [Test]
    public async Task GivenAccessTokenEntity_WhenValidateAccessTokenJtiAsync_ThenReflectsRevokedStateAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        _ = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), new List<Claim>());
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserId == userId);

        (await _jwtTokenService.ValidateAccessTokenJtiAsync(entity.Id, CancellationToken.None)).Should().BeTrue();

        entity.RevokedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync();

        (await _jwtTokenService.ValidateAccessTokenJtiAsync(entity.Id, CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    public async Task GivenExpiredAccessToken_WhenValidateAccessTokenAsync_ThenReturnsFalseAsync()
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
    public async Task GivenRevokedAccessToken_WhenValidateAccessTokenAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), new List<Claim>());
        await _jwtTokenService.RevokeAccessTokenAsync((await Context.AccessTokens.FirstAsync(x => x.UserId == userId)).Id);

        var result = await _jwtTokenService.ValidateAccessTokenAsync(token);

        result.Should().BeFalse();
    }

    [Test]
    public async Task GivenAccessTokenNotInDatabase_WhenValidateAccessTokenAsync_ThenReturnsFalseAsync()
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
    public async Task GivenForgedAccessTokenWithRealJti_WhenValidateAccessTokenAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        _ = await _jwtTokenService.GenerateAccessTokenAsync(
            userId, familyId, new List<string>(),
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId.ToString()) });
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserId == userId);

        var forged = CreateJwtSignedWithWrongKey(
            jti: entity.Id,
            sub: userId,
            issuer: "test-issuer",
            audience: "test-audience");

        var result = await _jwtTokenService.ValidateAccessTokenAsync(forged);

        result.Should().BeFalse();
    }

    [Test]
    public async Task GivenEncryptedAccessToken_WhenValidateAccessTokenAsync_ThenReturnsTrueAsync()
    {
        var options = new Mock<IOptionsSnapshot<AuthenticationOptions>>();
        options.Setup(o => o.Value).Returns(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Issuer = "test-issuer",
                Audience = "test-audience",
                Key = SignKeyBase64,
                EncryptionKey = EncKeyBase64,
                UseEncryption = true,
                AccessTokenExpires = TimeSpan.FromMinutes(15),
                RefreshTokenExpires = TimeSpan.FromMinutes(60),
                RefreshTokenAbsoluteExpires = TimeSpan.FromDays(30),
            }
        });
        var encryptedService = new JwtTokenService(Context, options.Object, _httpContextAccessor.Object);

        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await encryptedService.GenerateAccessTokenAsync(
            userId, familyId, new List<string>(),
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId.ToString()) });
        token.Split('.').Length.Should().Be(5);

        var result = await encryptedService.ValidateAccessTokenAsync(token);

        result.Should().BeTrue();
    }

    [Test]
    public async Task GivenValidRefreshToken_WhenValidateRefreshTokenAsync_ThenReturnsTrueAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());

        var result = await _jwtTokenService.ValidateRefreshTokenAsync(token);

        result.Should().BeTrue();
    }

    [Test]
    public async Task GivenUnknownRefreshToken_WhenValidateRefreshTokenAsync_ThenReturnsFalseAsync()
    {
        var result = await _jwtTokenService.ValidateRefreshTokenAsync("not-a-valid-token-string");

        result.Should().BeFalse();
    }

    [Test]
    public async Task GivenExistingAccessToken_WhenRevokeAccessTokenAsync_ThenSetsRevokedAtAsync()
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
    public async Task GivenMissingAccessTokenEntry_WhenRevokeAccessTokenAsync_ThenDoesNotThrowAsync()
    {
        var act = () => _jwtTokenService.RevokeAccessTokenAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task GivenExpiredAccessTokens_WhenCleanupExpiredAccessTokensAsync_ThenRemovesExpiredTokensAsync()
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
    public async Task GivenRefreshTokensPastAbsoluteExpiry_WhenCleanupExpiredRefreshTokensAsync_ThenRemovesTokensAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());
        var entity = await Context.RefreshTokens.FirstAsync(x => x.UserId == userId);
        entity.AbsoluteExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await Context.SaveChangesAsync();

        await _jwtTokenService.CleanupExpiredRefreshTokensAsync(CancellationToken.None);

        var found = await Context.RefreshTokens.FirstOrDefaultAsync(x => x.Id == entity.Id);
        found.Should().BeNull();
    }

    [Test]
    public async Task GivenValidRefreshTokens_WhenCleanupExpiredRefreshTokensAsync_ThenKeepsTokensAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());
        var entity = await Context.RefreshTokens.FirstAsync(x => x.UserId == userId);

        await _jwtTokenService.CleanupExpiredRefreshTokensAsync(CancellationToken.None);

        var found = await Context.RefreshTokens.FirstOrDefaultAsync(x => x.Id == entity.Id);
        found.Should().NotBeNull();
    }

    [Test]
    public async Task GivenAccessTokenWithClaim_WhenGetClaimValue_ThenReturnsClaimValueAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(),
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId.ToString()) });

        var value = _jwtTokenService.GetClaimValue(token, JwtRegisteredClaimNames.Sub);

        value.Should().Be(userId.ToString());
    }

    [Test]
    public void GivenNullToken_WhenGetClaimValue_ThenThrowsArgumentNullException()
    {
        var act = () => _jwtTokenService.GetClaimValue(null!, "sub");

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task GivenExistingRefreshToken_WhenGetRefreshTokenAsync_ThenReturnsEntityAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());

        var entity = await _jwtTokenService.GetRefreshTokenAsync(token, CancellationToken.None);

        entity.Should().NotBeNull();
        entity!.UserId.Should().Be(userId);
    }

    [Test]
    public async Task GivenUnknownRefreshToken_WhenGetRefreshTokenAsync_ThenReturnsNullAsync()
    {
        var entity = await _jwtTokenService.GetRefreshTokenAsync("not-a-valid-token", CancellationToken.None);

        entity.Should().BeNull();
    }

    [Test]
    public async Task GivenActiveRefreshToken_WhenInvalidateRefreshTokenAsync_ThenSetsReplacedByAndRevokedAtAsync()
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
    public async Task GivenMissingRefreshToken_WhenInvalidateRefreshTokenAsync_ThenThrowsInvalidOperationExceptionAsync()
    {
        var act = () => _jwtTokenService.InvalidateRefreshTokenAsync("nonexistent-token", Guid.NewGuid().ToString(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Refresh token not found*");
    }

    [Test]
    public async Task GivenRevokedRefreshToken_WhenInvalidateRefreshTokenAsync_ThenThrowsConflictExceptionAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());
        await _jwtTokenService.InvalidateRefreshTokenAsync(token, Guid.NewGuid().ToString(), CancellationToken.None);

        var act = () => _jwtTokenService.InvalidateRefreshTokenAsync(token, Guid.NewGuid().ToString(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already been used*");
    }

    [Test]
    public async Task GivenReplayDetectedRefreshToken_WhenInvalidateRefreshTokenAsync_ThenRevokesEntireFamilyAsync()
    {
        // Theft race: attacker refreshed first (R1 → R2); victim reuses R1 → family must die.
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var otherFamilyId = Guid.NewGuid();

        var r1 = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());
        var r2 = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());
        var accessInFamily = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), new List<Claim>());
        var otherFamilyRefresh = await _jwtTokenService.GenerateRefreshTokenAsync(userId, otherFamilyId, new List<Claim>());

        await _jwtTokenService.InvalidateRefreshTokenAsync(r1, Guid.NewGuid().ToString(), CancellationToken.None);

        var act = () => _jwtTokenService.InvalidateRefreshTokenAsync(r1, Guid.NewGuid().ToString(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already been used*");

        (await _jwtTokenService.ValidateRefreshTokenAsync(r2)).Should().BeFalse();
        (await _jwtTokenService.ValidateAccessTokenAsync(accessInFamily)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(otherFamilyRefresh)).Should().BeTrue();

        var r1Entity = await Context.RefreshTokens.SingleAsync(x =>
            x.TokenHash == Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(r1))));
        r1Entity.RevokeReason.Should().Be(RefreshTokenRevokeReason.REPLAY_DETECTED);

        var r2Entity = await Context.RefreshTokens.SingleAsync(x =>
            x.TokenHash == Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(r2))));
        r2Entity.RevokedAt.Should().NotBeNull();
        r2Entity.RevokeReason.Should().Be(RefreshTokenRevokeReason.REPLAY_DETECTED);

        var accessEntity = await Context.AccessTokens.SingleAsync(x =>
            x.TokenHash == Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(accessInFamily))));
        accessEntity.RevokedAt.Should().NotBeNull();
        accessEntity.RevokeReason.Should().Be(RefreshTokenRevokeReason.REPLAY_DETECTED);
    }

    [Test]
    public async Task GivenActiveRefreshToken_WhenEnsureRefreshTokenActiveForRotationAsync_ThenDoesNotThrowAsync()
    {
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(Guid.NewGuid(), Guid.NewGuid(), new List<Claim>());

        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync(token, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task GivenMissingRefreshToken_WhenEnsureRefreshTokenActiveForRotationAsync_ThenThrowsNotAuthorizedAsync()
    {
        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync("missing-token", CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }

    [Test]
    public async Task GivenRevokedRefreshToken_WhenEnsureRefreshTokenActiveForRotationAsync_ThenRevokesFamilyAndThrowsConflictAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var r1 = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());
        var r2 = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());
        await _jwtTokenService.InvalidateRefreshTokenAsync(r1, Guid.NewGuid().ToString(), CancellationToken.None);

        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync(r1, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already been used*");

        (await _jwtTokenService.ValidateRefreshTokenAsync(r2)).Should().BeFalse();
        var r2Entity = await Context.RefreshTokens.SingleAsync(x =>
            x.TokenHash == Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(r2))));
        r2Entity.RevokeReason.Should().Be(RefreshTokenRevokeReason.REPLAY_DETECTED);
    }

    [Test]
    public async Task GivenActiveRefreshToken_WhenRevokeRefreshTokenForLogoutAsync_ThenSetsRevokedAtWithUserLogoutReasonAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());

        await _jwtTokenService.RevokeRefreshTokenForLogoutAsync(token, CancellationToken.None);

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var entity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == hash);
        entity.RevokedAt.Should().NotBeNull();
        entity.RevokeReason.Should().Be(RefreshTokenRevokeReason.USER_LOGOUT);
    }

    [Test]
    public async Task GivenNullOrEmptyRefreshToken_WhenRevokeRefreshTokenForLogoutAsync_ThenDoesNotThrowAsync()
    {
        var act = async () =>
        {
            await _jwtTokenService.RevokeRefreshTokenForLogoutAsync(null, CancellationToken.None);
            await _jwtTokenService.RevokeRefreshTokenForLogoutAsync("   ", CancellationToken.None);
        };

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task GivenAlreadyRevokedRefreshToken_WhenRevokeRefreshTokenForLogoutAsync_ThenDoesNotThrowAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());
        await _jwtTokenService.RevokeRefreshTokenForLogoutAsync(token, CancellationToken.None);

        var act = () => _jwtTokenService.RevokeRefreshTokenForLogoutAsync(token, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task GivenMultipleUserTokens_WhenRevokeAllTokensForLogoutAsync_ThenRevokesAllUserTokensWithUserLogoutAllAsync()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var familyA = Guid.NewGuid();
        var familyB = Guid.NewGuid();

        var refreshA = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyA, new List<Claim>());
        var refreshB = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyB, new List<Claim>());
        var accessA = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyA, new List<string>(), new List<Claim>());
        var otherRefresh = await _jwtTokenService.GenerateRefreshTokenAsync(otherUserId, Guid.NewGuid(), new List<Claim>());

        await _jwtTokenService.RevokeAllTokensForLogoutAsync(refreshA, CancellationToken.None);

        (await _jwtTokenService.ValidateRefreshTokenAsync(refreshA)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(refreshB)).Should().BeFalse();
        (await _jwtTokenService.ValidateAccessTokenAsync(accessA)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(otherRefresh)).Should().BeTrue();

        var userTokens = await Context.RefreshTokens.Where(x => x.UserId == userId).ToListAsync();
        userTokens.Should().OnlyContain(t =>
            t.RevokedAt != null && t.RevokeReason == RefreshTokenRevokeReason.USER_LOGOUT_ALL);
    }

    [Test]
    public async Task GivenNullOrEmptyRefreshToken_WhenRevokeAllTokensForLogoutAsync_ThenDoesNotThrowAsync()
    {
        var act = async () =>
        {
            await _jwtTokenService.RevokeAllTokensForLogoutAsync(null, CancellationToken.None);
            await _jwtTokenService.RevokeAllTokensForLogoutAsync("   ", CancellationToken.None);
        };

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task GivenInvalidRefreshToken_WhenRevokeAllTokensForLogoutAsync_ThenThrowsNotAuthorizedAsync()
    {
        var act = () => _jwtTokenService.RevokeAllTokensForLogoutAsync("not-a-token", CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }

    [Test]
    public async Task GivenAlreadyRevokedRefreshToken_WhenRevokeAllTokensForLogoutAsync_ThenThrowsNotAuthorizedAsync()
    {
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(Guid.NewGuid(), Guid.NewGuid(), new List<Claim>());
        await _jwtTokenService.RevokeRefreshTokenForLogoutAsync(token, CancellationToken.None);

        var act = () => _jwtTokenService.RevokeAllTokensForLogoutAsync(token, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }

    [Test]
    public async Task GivenActiveUserTokens_WhenRevokeAllTokensForUserAsync_ThenRevokesAccessAndRefreshTokensAsync()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), new List<Claim>());
        var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, new List<Claim>());
        var otherRefresh = await _jwtTokenService.GenerateRefreshTokenAsync(otherUserId, Guid.NewGuid(), new List<Claim>());

        await _jwtTokenService.RevokeAllTokensForUserAsync(userId, RefreshTokenRevokeReason.PASSWORD_CHANGED, CancellationToken.None);
        await Context.SaveChangesAsync();

        (await _jwtTokenService.ValidateAccessTokenAsync(accessToken)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(refreshToken)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(otherRefresh)).Should().BeTrue();

        var userRefresh = await Context.RefreshTokens.SingleAsync(x => x.UserId == userId);
        userRefresh.RevokedAt.Should().NotBeNull();
        userRefresh.RevokeReason.Should().Be(RefreshTokenRevokeReason.PASSWORD_CHANGED);
    }

    [Test]
    public void GivenConfiguredJwtOptions_WhenReadingAccessTokenExpiresInSeconds_ThenReturnsConfiguredLifetime()
    {
        _jwtTokenService.AccessTokenExpiresInSeconds.Should().Be(15 * 60);
    }

    [Test]
    public async Task GivenConfiguredRefreshTokenLifetime_WhenGenerateRefreshTokenAsync_ThenUsesConfiguredRollingLifetimeAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId.ToString()) };

        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, claims);

        token.Should().NotBeNullOrEmpty();
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var entity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == hash);
        (entity.AbsoluteExpiresAt - entity.CreatedAt).Should().BeCloseTo(TimeSpan.FromDays(30), TimeSpan.FromSeconds(2));
        (entity.ExpiresAt - entity.CreatedAt).Should().BeCloseTo(TimeSpan.FromMinutes(60), TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task GivenRotatedRefreshToken_WhenGenerateRefreshTokenAsync_ThenPreservesChainAbsoluteExpiresAtAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId.ToString()) };

        var firstToken = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, claims);
        var firstHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(firstToken)));
        var firstEntity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == firstHash);

        var secondToken = await _jwtTokenService.GenerateRefreshTokenAsync(userId, familyId, claims);
        var secondHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secondToken)));
        var secondEntity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == secondHash);

        secondEntity.AbsoluteExpiresAt.Should().Be(firstEntity.AbsoluteExpiresAt);
    }

    private static string CreateJwtSignedWithWrongKey(Guid jti, Guid sub, string issuer, string audience)
    {
        var wrongKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
        var handler = new JsonWebTokenHandler();
        var createdAt = DateTime.UtcNow;
        return handler.CreateToken(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, jti.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, sub.ToString()),
            }),
            Issuer = issuer,
            Audience = audience,
            IssuedAt = createdAt,
            NotBefore = createdAt.AddSeconds(-1),
            Expires = createdAt.AddMinutes(15),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                wrongKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256),
        });
    }
}
