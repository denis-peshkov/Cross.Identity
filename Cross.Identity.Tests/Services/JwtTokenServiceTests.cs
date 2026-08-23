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

        _jwtTokenService = new JwtTokenService(Context, new AuditService(Context), options.Object);
    }

    private void SeedUser(Guid userAccountId, bool isActive = true, Guid? securityStamp = null)
    {
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = $"{userAccountId:N}@jwt.test",
            IsActive = isActive,
            SecurityStamp = securityStamp ?? Guid.NewGuid(),
        });
    }

    [Test]
    [Category(TestCategory.UNIT)]
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

        var act = () => new JwtTokenService(Context, new AuditService(Context), options.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt.EncryptionKey must be 32 bytes*");
    }

    [Test]
    [Category(TestCategory.UNIT)]
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

        var act = () => new JwtTokenService(Context, new AuditService(Context), options.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt.Key should be at least 32 bytes*");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidClaims_WhenGenerateIdToken_ThenReturnsValidToken()
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()) };

        var token = _jwtTokenService.GenerateIdToken(claims);

        token.Should().NotBeNullOrEmpty();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidUser_WhenGenerateAccessTokenAsync_ThenPersistsAndReturnsTokenAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var permissions = new List<string> { "read" };
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) };

        var token = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, permissions, claims, ClientContext.Empty, CancellationToken.None);

        token.Should().NotBeNullOrEmpty();
        var entity = await Context.AccessTokens.FirstOrDefaultAsync(x => x.UserAccountId == userAccountId);
        entity.Should().NotBeNull();
        entity!.TokenHash.Should().NotBeNullOrEmpty();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidUser_WhenGenerateRefreshTokenAsync_ThenPersistsAndReturnsTokenAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) };

        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, claims, ClientContext.Empty, CancellationToken.None);

        token.Should().NotBeNullOrEmpty();
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var entity = await Context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
        entity.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidAccessToken_WhenValidateAccessTokenAsync_ThenReturnsTrueAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        var result = await _jwtTokenService.ValidateAccessTokenAsync(token, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAccessToken_WhenGenerated_ThenContainsSecurityStampClaimAsync()
    {
        var userAccountId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        SeedUser(userAccountId, securityStamp: stamp);
        var token = await _jwtTokenService.GenerateAccessTokenAsync(
            userAccountId, Guid.NewGuid(), new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        _jwtTokenService.GetClaimValue(token, ClaimConstants.SecurityStamp).Should().Be(stamp.ToString("D"));
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenCallerSpoofsSecurityStamp_WhenGenerateAccessTokenAsync_ThenUsesAccountStampAsync()
    {
        var userAccountId = Guid.NewGuid();
        var accountStamp = Guid.NewGuid();
        SeedUser(userAccountId, securityStamp: accountStamp);
        var spoofed = new List<Claim> { new(ClaimConstants.SecurityStamp, Guid.NewGuid().ToString("D")) };

        var token = await _jwtTokenService.GenerateAccessTokenAsync(
            userAccountId, Guid.NewGuid(), new List<string>(), spoofed, ClientContext.Empty, CancellationToken.None);

        _jwtTokenService.GetClaimValue(token, ClaimConstants.SecurityStamp).Should().Be(accountStamp.ToString("D"));
        (await _jwtTokenService.ValidateAccessTokenAsync(token, CancellationToken.None)).Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRotatedSecurityStamp_WhenValidateAccessTokenAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var token = await _jwtTokenService.GenerateAccessTokenAsync(
            userAccountId, Guid.NewGuid(), new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        var user = await Context.UsersAccounts.SingleAsync(x => x.Id == userAccountId);
        user.SecurityStamp = Guid.NewGuid();
        await Context.SaveChangesAsync();

        (await _jwtTokenService.ValidateAccessTokenAsync(token, CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRotatedSecurityStamp_WhenValidateRefreshTokenAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, Guid.NewGuid(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        var user = await Context.UsersAccounts.SingleAsync(x => x.Id == userAccountId);
        user.SecurityStamp = Guid.NewGuid();
        await Context.SaveChangesAsync();

        (await _jwtTokenService.ValidateRefreshTokenAsync(token, CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRotatedSecurityStamp_WhenValidateAccessTokenJtiAsyncWithStamp_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        SeedUser(userAccountId, securityStamp: stamp);
        var token = await _jwtTokenService.GenerateAccessTokenAsync(
            userAccountId, Guid.NewGuid(), new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var jti = Guid.Parse(_jwtTokenService.GetClaimValue(token, JwtRegisteredClaimNames.Jti)!);

        var user = await Context.UsersAccounts.SingleAsync(x => x.Id == userAccountId);
        user.SecurityStamp = Guid.NewGuid();
        await Context.SaveChangesAsync();

        (await _jwtTokenService.ValidateAccessTokenJtiAsync(jti, stamp, CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAccessTokenEntity_WhenValidateAccessTokenJtiAsync_ThenReflectsRevokedStateAsync()
    {
        var userAccountId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        SeedUser(userAccountId, securityStamp: stamp);
        var familyId = Guid.NewGuid();
        _ = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserAccountId == userAccountId);

        (await _jwtTokenService.ValidateAccessTokenJtiAsync(entity.Id, stamp, CancellationToken.None)).Should().BeTrue();

        entity.RevokedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync();

        (await _jwtTokenService.ValidateAccessTokenJtiAsync(entity.Id, stamp, CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAccountSecurityStamp_WhenValidateAccessTokenJtiAsyncWithoutStamp_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var token = await _jwtTokenService.GenerateAccessTokenAsync(
            userAccountId, Guid.NewGuid(), new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var jti = Guid.Parse(_jwtTokenService.GetClaimValue(token, JwtRegisteredClaimNames.Jti)!);

        (await _jwtTokenService.ValidateAccessTokenJtiAsync(jti, securityStamp: null, CancellationToken.None))
            .Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExpiredAccessToken_WhenValidateAccessTokenAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserAccountId == userAccountId);
        entity.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await Context.SaveChangesAsync();

        var result = await _jwtTokenService.ValidateAccessTokenAsync(token, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRevokedAccessToken_WhenValidateAccessTokenAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        await _jwtTokenService.RevokeAccessTokenAsync((await Context.AccessTokens.FirstAsync(x => x.UserAccountId == userAccountId)).Id, CancellationToken.None);

        var result = await _jwtTokenService.ValidateAccessTokenAsync(token, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAccessTokenNotInDatabase_WhenValidateAccessTokenAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserAccountId == userAccountId);
        Context.AccessTokens.Remove(entity);
        await Context.SaveChangesAsync();

        var result = await _jwtTokenService.ValidateAccessTokenAsync(token, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenForgedAccessTokenWithRealJti_WhenValidateAccessTokenAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        _ = await _jwtTokenService.GenerateAccessTokenAsync(
            userAccountId, familyId, new List<string>(),
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, ClientContext.Empty, CancellationToken.None);
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserAccountId == userAccountId);

        var forged = CreateJwtSignedWithWrongKey(
            jti: entity.Id,
            sub: userAccountId,
            issuer: "test-issuer",
            audience: "test-audience");

        var result = await _jwtTokenService.ValidateAccessTokenAsync(forged, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
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
        var encryptedService = new JwtTokenService(Context, new AuditService(Context), options.Object);

        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var familyId = Guid.NewGuid();
        var token = await encryptedService.GenerateAccessTokenAsync(
            userAccountId, familyId, new List<string>(),
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, ClientContext.Empty, CancellationToken.None);
        token.Split('.').Length.Should().Be(5);

        var result = await encryptedService.ValidateAccessTokenAsync(token, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidRefreshToken_WhenValidateRefreshTokenAsync_ThenReturnsTrueAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        var result = await _jwtTokenService.ValidateRefreshTokenAsync(token, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnknownRefreshToken_WhenValidateRefreshTokenAsync_ThenReturnsFalseAsync()
    {
        var result = await _jwtTokenService.ValidateRefreshTokenAsync("not-a-valid-token-string", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingAccessToken_WhenRevokeAccessTokenAsync_ThenSetsRevokedAtAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserAccountId == userAccountId);

        await _jwtTokenService.RevokeAccessTokenAsync(entity.Id, CancellationToken.None);

        await Context.Entry(entity).ReloadAsync();
        entity.RevokedAt.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingAccessTokenEntry_WhenRevokeAccessTokenAsync_ThenDoesNotThrowAsync()
    {
        var act = () => _jwtTokenService.RevokeAccessTokenAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExpiredAccessTokens_WhenCleanupExpiredAccessTokensAsync_ThenRemovesExpiredTokensAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var entity = await Context.AccessTokens.FirstAsync(x => x.UserAccountId == userAccountId);
        entity.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await Context.SaveChangesAsync();

        await _jwtTokenService.CleanupExpiredAccessTokensAsync(CancellationToken.None);

        var found = await Context.AccessTokens.FirstOrDefaultAsync(x => x.Id == entity.Id);
        found.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRefreshTokensPastAbsoluteExpiry_WhenCleanupExpiredRefreshTokensAsync_ThenRemovesTokensAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var entity = await Context.RefreshTokens.FirstAsync(x => x.UserAccountId == userAccountId);
        entity.AbsoluteExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await Context.SaveChangesAsync();

        await _jwtTokenService.CleanupExpiredRefreshTokensAsync(CancellationToken.None);

        var found = await Context.RefreshTokens.FirstOrDefaultAsync(x => x.Id == entity.Id);
        found.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidRefreshTokens_WhenCleanupExpiredRefreshTokensAsync_ThenKeepsTokensAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var entity = await Context.RefreshTokens.FirstAsync(x => x.UserAccountId == userAccountId);

        await _jwtTokenService.CleanupExpiredRefreshTokensAsync(CancellationToken.None);

        var found = await Context.RefreshTokens.FirstOrDefaultAsync(x => x.Id == entity.Id);
        found.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAccessTokenWithClaim_WhenGetClaimValue_ThenReturnsClaimValueAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, new List<string>(),
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, ClientContext.Empty, CancellationToken.None);

        var value = _jwtTokenService.GetClaimValue(token, JwtRegisteredClaimNames.Sub);

        value.Should().Be(userAccountId.ToString());
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEncryptedAccessToken_WhenGetClaimValue_ThenReturnsClaimValueAsync()
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
        var encryptedService = new JwtTokenService(Context, new AuditService(Context), options.Object);

        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await encryptedService.GenerateAccessTokenAsync(
            userAccountId, familyId, new List<string>(),
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, ClientContext.Empty, CancellationToken.None);
        token.Split('.').Length.Should().Be(5);

        var sub = encryptedService.GetClaimValue(token, JwtRegisteredClaimNames.Sub);
        var jti = encryptedService.GetClaimValue(token, JwtRegisteredClaimNames.Jti);

        sub.Should().Be(userAccountId.ToString());
        jti.Should().NotBeNullOrEmpty();
        Guid.TryParse(jti, out _).Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNullToken_WhenGetClaimValue_ThenThrowsArgumentNullException()
    {
        var act = () => _jwtTokenService.GetClaimValue(null!, "sub");

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenTokenWithWrongPartCount_WhenGetClaimValue_ThenThrowsArgumentException()
    {
        var act = () => _jwtTokenService.GetClaimValue("a.b", "sub");

        act.Should().Throw<ArgumentException>().WithMessage("*JWS*JWE*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingRefreshToken_WhenGetRefreshTokenAsync_ThenReturnsEntityAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        var entity = await _jwtTokenService.GetRefreshTokenAsync(token, CancellationToken.None);

        entity.Should().NotBeNull();
        entity!.UserAccountId.Should().Be(userAccountId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnknownRefreshToken_WhenGetRefreshTokenAsync_ThenReturnsNullAsync()
    {
        var entity = await _jwtTokenService.GetRefreshTokenAsync("not-a-valid-token", CancellationToken.None);

        entity.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenActiveRefreshToken_WhenInvalidateRefreshTokenAsync_ThenSetsReplacedByAndRevokedAtAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var oldToken = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var newJti = Guid.NewGuid().ToString();

        await _jwtTokenService.InvalidateRefreshTokenAsync(oldToken, newJti, ClientContext.Empty, CancellationToken.None);

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(oldToken)));
        var entity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == hash);
        entity.RevokedAt.Should().NotBeNull();
        entity.ReplacedByTokenId.Should().Be(Guid.Parse(newJti));
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingRefreshToken_WhenInvalidateRefreshTokenAsync_ThenThrowsInvalidOperationExceptionAsync()
    {
        var act = () => _jwtTokenService.InvalidateRefreshTokenAsync("nonexistent-token", Guid.NewGuid().ToString(), ClientContext.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Refresh token not found*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRevokedRefreshToken_WhenInvalidateRefreshTokenAsync_ThenThrowsConflictExceptionAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        await _jwtTokenService.InvalidateRefreshTokenAsync(token, Guid.NewGuid().ToString(), ClientContext.Empty, CancellationToken.None);

        var act = () => _jwtTokenService.InvalidateRefreshTokenAsync(token, Guid.NewGuid().ToString(), ClientContext.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already been used*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenReplayDetectedRefreshToken_WhenInvalidateRefreshTokenAsync_ThenRevokesEntireFamilyAsync()
    {
        // Theft race: attacker refreshed first (R1 → R2); victim reuses R1 → family must die.
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var familyId = Guid.NewGuid();
        var otherFamilyId = Guid.NewGuid();

        var r1 = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var r2 = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var accessInFamily = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var otherFamilyRefresh = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, otherFamilyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        await _jwtTokenService.InvalidateRefreshTokenAsync(r1, Guid.NewGuid().ToString(), ClientContext.Empty, CancellationToken.None);

        var act = () => _jwtTokenService.InvalidateRefreshTokenAsync(r1, Guid.NewGuid().ToString(), ClientContext.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already been used*");

        (await _jwtTokenService.ValidateRefreshTokenAsync(r2, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateAccessTokenAsync(accessInFamily, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(otherFamilyRefresh, CancellationToken.None)).Should().BeTrue();

        var r1Entity = await Context.RefreshTokens.SingleAsync(x =>
            x.TokenHash == Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(r1))));
        r1Entity.RevokedAt.Should().NotBeNull();
        Context.Audits.Should().Contain(a =>
            a.EntityId == r1Entity.Id.ToString()
            && a.RevokedReason == RefreshTokenRevokedReason.REPLAY_DETECTED);

        var r2Entity = await Context.RefreshTokens.SingleAsync(x =>
            x.TokenHash == Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(r2))));
        r2Entity.RevokedAt.Should().NotBeNull();
        Context.Audits.Should().Contain(a =>
            a.EntityId == r2Entity.Id.ToString()
            && a.RevokedReason == RefreshTokenRevokedReason.REPLAY_DETECTED);

        var accessEntity = await Context.AccessTokens.SingleAsync(x =>
            x.TokenHash == Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(accessInFamily))));
        accessEntity.RevokedAt.Should().NotBeNull();
        Context.Audits.Should().Contain(a =>
            a.EntityId == accessEntity.Id.ToString()
            && a.RevokedReason == RefreshTokenRevokedReason.REPLAY_DETECTED);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenActiveRefreshToken_WhenEnsureRefreshTokenActiveForRotationAsync_ThenDoesNotThrowAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, Guid.NewGuid(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync(token, ClientContext.Empty, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingRefreshToken_WhenEnsureRefreshTokenActiveForRotationAsync_ThenThrowsNotAuthorizedAsync()
    {
        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync("missing-token", ClientContext.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRevokedRefreshToken_WhenEnsureRefreshTokenActiveForRotationAsync_ThenRevokesFamilyAndThrowsConflictAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var r1 = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var r2 = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        await _jwtTokenService.InvalidateRefreshTokenAsync(r1, Guid.NewGuid().ToString(), ClientContext.Empty, CancellationToken.None);

        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync(r1, ClientContext.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already been used*");

        (await _jwtTokenService.ValidateRefreshTokenAsync(r2, CancellationToken.None)).Should().BeFalse();
        var r2Entity = await Context.RefreshTokens.SingleAsync(x =>
            x.TokenHash == Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(r2))));
        r2Entity.RevokedAt.Should().NotBeNull();
        Context.Audits.Should().Contain(a =>
            a.EntityId == r2Entity.Id.ToString()
            && a.RevokedReason == RefreshTokenRevokedReason.REPLAY_DETECTED);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenActiveRefreshToken_WhenRevokeRefreshTokenForLogoutAsync_ThenSetsRevokedAtWithUserLogoutReasonAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        await _jwtTokenService.RevokeRefreshTokenForLogoutAsync(token, ClientContext.Empty, CancellationToken.None);

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var entity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == hash);
        entity.RevokedAt.Should().NotBeNull();
        Context.Audits.Should().Contain(a =>
            a.EntityId == entity.Id.ToString()
            && a.RevokedReason == RefreshTokenRevokedReason.USER_LOGOUT);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenActiveRefreshAndAccessToken_WhenRevokeRefreshTokenForLogoutAsync_ThenRevokesAccessTokensInSameFamilyAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var familyId = Guid.NewGuid();
        var otherFamilyId = Guid.NewGuid();

        var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var otherAccessToken = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, otherFamilyId, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        await _jwtTokenService.RevokeRefreshTokenForLogoutAsync(refreshToken, ClientContext.Empty, CancellationToken.None);

        (await _jwtTokenService.ValidateAccessTokenAsync(accessToken, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateAccessTokenAsync(otherAccessToken, CancellationToken.None)).Should().BeTrue();

        var accessEntity = await Context.AccessTokens.SingleAsync(x => x.FamilyId == familyId);
        accessEntity.RevokedAt.Should().NotBeNull();
        Context.Audits.Should().Contain(a =>
            a.EntityId == accessEntity.Id.ToString()
            && a.RevokedReason == RefreshTokenRevokedReason.USER_LOGOUT);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenNullOrEmptyRefreshToken_WhenRevokeRefreshTokenForLogoutAsync_ThenDoesNotThrowAsync()
    {
        var act = async () =>
        {
            await _jwtTokenService.RevokeRefreshTokenForLogoutAsync(null, ClientContext.Empty, CancellationToken.None);
            await _jwtTokenService.RevokeRefreshTokenForLogoutAsync("   ", ClientContext.Empty, CancellationToken.None);
        };

        await act.Should().NotThrowAsync();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAlreadyRevokedRefreshToken_WhenRevokeRefreshTokenForLogoutAsync_ThenDoesNotThrowAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        await _jwtTokenService.RevokeRefreshTokenForLogoutAsync(token, ClientContext.Empty, CancellationToken.None);

        var act = () => _jwtTokenService.RevokeRefreshTokenForLogoutAsync(token, ClientContext.Empty, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMultipleUserTokens_WhenRevokeAllTokensForLogoutAsync_ThenRevokesAllUserTokensWithUserLogoutAllAsync()
    {
        var userAccountId = Guid.NewGuid();
        var otherUserAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        SeedUser(otherUserAccountId);
        var familyA = Guid.NewGuid();
        var familyB = Guid.NewGuid();

        var refreshA = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyA, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var refreshB = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyB, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var accessA = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyA, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var otherRefresh = await _jwtTokenService.GenerateRefreshTokenAsync(otherUserAccountId, Guid.NewGuid(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        await _jwtTokenService.RevokeAllTokensForLogoutAsync(refreshA, ClientContext.Empty, CancellationToken.None);

        (await _jwtTokenService.ValidateRefreshTokenAsync(refreshA, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(refreshB, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateAccessTokenAsync(accessA, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(otherRefresh, CancellationToken.None)).Should().BeTrue();

        var userTokens = await Context.RefreshTokens.Where(x => x.UserAccountId == userAccountId).ToListAsync();
        userTokens.Should().OnlyContain(t => t.RevokedAt != null);
        Context.Audits.Should().Contain(a =>
            a.UserAccountId == userAccountId && a.RevokedReason == RefreshTokenRevokedReason.USER_LOGOUT_ALL);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenNullOrEmptyRefreshToken_WhenRevokeAllTokensForLogoutAsync_ThenDoesNotThrowAsync()
    {
        var act = async () =>
        {
            await _jwtTokenService.RevokeAllTokensForLogoutAsync(null, ClientContext.Empty, CancellationToken.None);
            await _jwtTokenService.RevokeAllTokensForLogoutAsync("   ", ClientContext.Empty, CancellationToken.None);
        };

        await act.Should().NotThrowAsync();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidRefreshToken_WhenRevokeAllTokensForLogoutAsync_ThenThrowsNotAuthorizedAsync()
    {
        var act = () => _jwtTokenService.RevokeAllTokensForLogoutAsync("not-a-token", ClientContext.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAlreadyRevokedRefreshToken_WhenRevokeAllTokensForLogoutAsync_ThenThrowsNotAuthorizedAsync()
    {
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(Guid.NewGuid(), Guid.NewGuid(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        await _jwtTokenService.RevokeRefreshTokenForLogoutAsync(token, ClientContext.Empty, CancellationToken.None);

        var act = () => _jwtTokenService.RevokeAllTokensForLogoutAsync(token, ClientContext.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenActiveUserTokens_WhenRevokeAllTokensForUserAsync_ThenRevokesAccessAndRefreshTokensAsync()
    {
        var userAccountId = Guid.NewGuid();
        var otherUserAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        SeedUser(otherUserAccountId);
        var familyId = Guid.NewGuid();

        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(userAccountId, familyId, new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var otherRefresh = await _jwtTokenService.GenerateRefreshTokenAsync(otherUserAccountId, Guid.NewGuid(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        await _jwtTokenService.RevokeAllTokensForUserAsync(userAccountId, RefreshTokenRevokedReason.PASSWORD_CHANGED, ClientContext.Empty, CancellationToken.None);
        await Context.SaveChangesAsync();

        (await _jwtTokenService.ValidateAccessTokenAsync(accessToken, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(refreshToken, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(otherRefresh, CancellationToken.None)).Should().BeTrue();

        var userRefresh = await Context.RefreshTokens.SingleAsync(x => x.UserAccountId == userAccountId);
        userRefresh.RevokedAt.Should().NotBeNull();
        Context.Audits.Should().Contain(a =>
            a.EntityId == userRefresh.Id.ToString()
            && a.RevokedReason == RefreshTokenRevokedReason.PASSWORD_CHANGED);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenConfiguredJwtOptions_WhenReadingAccessTokenExpiresInSeconds_ThenReturnsConfiguredLifetime()
    {
        _jwtTokenService.AccessTokenExpiresInSeconds.Should().Be(15 * 60);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenConfiguredRefreshTokenLifetime_WhenGenerateRefreshTokenAsync_ThenUsesConfiguredRollingLifetimeAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) };

        var token = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, claims, ClientContext.Empty, CancellationToken.None);

        token.Should().NotBeNullOrEmpty();
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var entity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == hash);
        (entity.AbsoluteExpiresAt - entity.CreatedAt).Should().BeCloseTo(TimeSpan.FromDays(30), TimeSpan.FromSeconds(2));
        (entity.ExpiresAt - entity.CreatedAt).Should().BeCloseTo(TimeSpan.FromMinutes(60), TimeSpan.FromSeconds(2));
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRotatedRefreshToken_WhenGenerateRefreshTokenAsync_ThenPreservesChainAbsoluteExpiresAtAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) };

        var firstToken = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, claims, ClientContext.Empty, CancellationToken.None);
        var firstHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(firstToken)));
        var firstEntity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == firstHash);

        var secondToken = await _jwtTokenService.GenerateRefreshTokenAsync(userAccountId, familyId, claims, ClientContext.Empty, CancellationToken.None);
        var secondHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secondToken)));
        var secondEntity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == secondHash);

        secondEntity.AbsoluteExpiresAt.Should().Be(firstEntity.AbsoluteExpiresAt);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInactiveUser_WhenValidateAccessTokenAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId, isActive: false);
        var token = await _jwtTokenService.GenerateAccessTokenAsync(
            userAccountId, Guid.NewGuid(), new List<string>(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        (await _jwtTokenService.ValidateAccessTokenAsync(token, CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInactiveUser_WhenValidateRefreshTokenAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId, isActive: false);
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, Guid.NewGuid(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        (await _jwtTokenService.ValidateRefreshTokenAsync(token, CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInactiveUser_WhenEnsureRefreshTokenActiveForRotationAsync_ThenThrowsNotAuthorizedAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId, isActive: false);
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, Guid.NewGuid(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync(token, ClientContext.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Account is disabled*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenClientContext_WhenGenerateRefreshTokenAsync_ThenPersistsSessionBindingAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var context = new ClientContext("10.0.0.1", "Agent/1.0", "fp-abc");

        var token = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyId, new List<Claim>(), context, CancellationToken.None);

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var entity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == hash);
        entity.CreatedIpAddress.Should().Be("10.0.0.1");
        entity.CreatedUserAgent.Should().Be("Agent/1.0");
        entity.CreatedDeviceFingerprint.Should().Be("fp-abc");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRotatedFamily_WhenGenerateRefreshTokenAsync_ThenInheritsFamilySessionBindingAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var loginContext = new ClientContext("10.0.0.1", "Agent/1.0", "fp-abc");
        var refreshContext = new ClientContext("10.0.0.2", "Agent/2.0", "fp-xyz");

        _ = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyId, new List<Claim>(), loginContext, CancellationToken.None);
        var secondToken = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyId, new List<Claim>(), refreshContext, CancellationToken.None);

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secondToken)));
        var entity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == hash);
        entity.CreatedIpAddress.Should().Be("10.0.0.1");
        entity.CreatedUserAgent.Should().Be("Agent/1.0");
        entity.CreatedDeviceFingerprint.Should().Be("fp-abc");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenDeviceBinding_WhenFingerprintMismatchOnRefresh_ThenRevokesFamilyWithDeviceMismatchAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var familyId = Guid.NewGuid();
        var otherFamilyId = Guid.NewGuid();
        var issueContext = new ClientContext("10.0.0.1", "Agent/1.0", "fp-abc");
        var mismatchContext = new ClientContext("10.0.0.1", "Agent/1.0", "fp-stolen");

        var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyId, new List<Claim>(), issueContext, CancellationToken.None);
        var siblingToken = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyId, new List<Claim>(), issueContext, CancellationToken.None);
        var otherFamilyToken = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, otherFamilyId, new List<Claim>(), issueContext, CancellationToken.None);

        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync(refreshToken, mismatchContext, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Session binding validation failed*");

        (await _jwtTokenService.ValidateRefreshTokenAsync(siblingToken, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(otherFamilyToken, CancellationToken.None)).Should().BeTrue();

        var entity = await Context.RefreshTokens.SingleAsync(x =>
            x.TokenHash == Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))));
        entity.RevokedAt.Should().NotBeNull();
        Context.Audits.Should().Contain(a =>
            a.EntityId == entity.Id.ToString()
            && a.RevokedReason == RefreshTokenRevokedReason.DEVICE_MISMATCH);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenIpBinding_WhenIpMismatchOnRefresh_ThenRevokesFamilyWithIpMismatchAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var familyId = Guid.NewGuid();
        var issueContext = new ClientContext("10.0.0.1", "Agent/1.0", "fp-abc");
        var mismatchContext = new ClientContext("10.0.0.9", "Agent/1.0", "fp-abc");

        var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyId, new List<Claim>(), issueContext, CancellationToken.None);

        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync(refreshToken, mismatchContext, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Session binding validation failed*");

        Context.Audits.Should().Contain(a =>
            a.RevokedReason == RefreshTokenRevokedReason.IP_MISMATCH);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUserAgentBinding_WhenUserAgentMismatchOnRefresh_ThenRevokesFamilyWithUserAgentMismatchAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var familyId = Guid.NewGuid();
        var issueContext = new ClientContext("10.0.0.1", "Agent/1.0", "fp-abc");
        var mismatchContext = new ClientContext("10.0.0.1", "Agent/9.0", "fp-abc");

        var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyId, new List<Claim>(), issueContext, CancellationToken.None);

        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync(refreshToken, mismatchContext, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Session binding validation failed*");

        Context.Audits.Should().Contain(a =>
            a.RevokedReason == RefreshTokenRevokedReason.USER_AGENT_MISMATCH);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMultipleBindings_WhenMultipleMismatchOnRefresh_ThenRevokesFamilyWithTokenStolenAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var familyId = Guid.NewGuid();
        var issueContext = new ClientContext("10.0.0.1", "Agent/1.0", "fp-abc");
        var mismatchContext = new ClientContext("10.0.0.9", "Agent/1.0", "fp-stolen");

        var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyId, new List<Claim>(), issueContext, CancellationToken.None);

        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync(refreshToken, mismatchContext, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>();

        Context.Audits.Should().Contain(a =>
            a.RevokedReason == RefreshTokenRevokedReason.TOKEN_STOLEN);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMatchingClientContext_WhenRefresh_ThenDoesNotThrowAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var context = new ClientContext("10.0.0.1", "Agent/1.0", "fp-abc");

        var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, Guid.NewGuid(), new List<Claim>(), context, CancellationToken.None);

        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync(refreshToken, context, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenLegacyTokenWithoutBinding_WhenMismatch_ThenDoesNotThrowAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var familyId = Guid.NewGuid();

        var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        var act = () => _jwtTokenService.EnsureRefreshTokenActiveForRotationAsync(
            refreshToken, new ClientContext(null, null, "fp-any"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRefreshToken_WhenGenerateRefreshTokenAsync_ThenSetsLastActivityAtAsync()
    {
        var userAccountId = Guid.NewGuid();
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, Guid.NewGuid(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var entity = await Context.RefreshTokens.FirstAsync(x => x.TokenHash == hash);
        entity.LastActivityAt.Should().BeCloseTo(entity.CreatedAt, TimeSpan.FromSeconds(2));
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenIdleTimeoutExceeded_WhenEnsureRefreshTokenActiveForRotationAsync_ThenRevokesFamilyWithSessionExpiredAsync()
    {
        var service = CreateJwtTokenServiceWithIdleTimeout(TimeSpan.FromDays(7));
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var familyId = Guid.NewGuid();
        var otherFamilyId = Guid.NewGuid();

        var refreshToken = await service.GenerateRefreshTokenAsync(
            userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var siblingToken = await service.GenerateRefreshTokenAsync(
            userAccountId, familyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var otherFamilyToken = await service.GenerateRefreshTokenAsync(
            userAccountId, otherFamilyId, new List<Claim>(), ClientContext.Empty, CancellationToken.None);

        var entity = await Context.RefreshTokens.SingleAsync(x =>
            x.TokenHash == Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))));
        entity.LastActivityAt = DateTime.UtcNow.AddDays(-8);
        await Context.SaveChangesAsync();

        var act = () => service.EnsureRefreshTokenActiveForRotationAsync(refreshToken, ClientContext.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*idle timeout*");

        (await service.ValidateRefreshTokenAsync(siblingToken, CancellationToken.None)).Should().BeFalse();
        (await service.ValidateRefreshTokenAsync(otherFamilyToken, CancellationToken.None)).Should().BeTrue();

        Context.Audits.Should().Contain(a =>
            a.EntityId == entity.Id.ToString()
            && a.RevokedReason == RefreshTokenRevokedReason.SESSION_EXPIRED);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenIdleTimeoutExceeded_WhenValidateRefreshTokenAsync_ThenReturnsFalseAsync()
    {
        var service = CreateJwtTokenServiceWithIdleTimeout(TimeSpan.FromHours(1));
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var token = await service.GenerateRefreshTokenAsync(
            userAccountId, Guid.NewGuid(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var entity = await Context.RefreshTokens.FirstAsync(x => x.UserAccountId == userAccountId);
        entity.LastActivityAt = DateTime.UtcNow.AddHours(-2);
        await Context.SaveChangesAsync();

        (await service.ValidateRefreshTokenAsync(token, CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenIdleTimeoutDisabled_WhenLastActivityStale_ThenValidateRefreshTokenAsyncReturnsTrueAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedUser(userAccountId);
        var token = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, Guid.NewGuid(), new List<Claim>(), ClientContext.Empty, CancellationToken.None);
        var entity = await Context.RefreshTokens.FirstAsync(x => x.UserAccountId == userAccountId);
        entity.LastActivityAt = DateTime.UtcNow.AddDays(-30);
        await Context.SaveChangesAsync();

        (await _jwtTokenService.ValidateRefreshTokenAsync(token, CancellationToken.None)).Should().BeTrue();
    }

    private JwtTokenService CreateJwtTokenServiceWithIdleTimeout(TimeSpan idleTimeout)
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
                UseEncryption = false,
                AccessTokenExpires = TimeSpan.FromMinutes(15),
                RefreshTokenExpires = TimeSpan.FromDays(30),
                RefreshTokenAbsoluteExpires = TimeSpan.FromDays(90),
                RefreshTokenIdleTimeout = idleTimeout,
            },
        });

        return new JwtTokenService(Context, new AuditService(Context), options.Object);
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
