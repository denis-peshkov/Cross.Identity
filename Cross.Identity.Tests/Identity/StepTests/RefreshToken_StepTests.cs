namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class RefreshToken_StepTests
{
    private Faker _faker = null!;
    private Mock<IJwtTokenService> _jwtTokenService = null!;
    private Mock<IUserService> _userService = null!;
    private Mock<ILogger> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _userService = new Mock<IUserService>();
        _logger = new Mock<ILogger>();
    }

    [Test]
    public async Task GivenValidRefreshToken_WhenExecuteAsync_ThenSetsAccessAndRefreshTokensAsync()
    {
        var refreshTokenHash = "refresh-token-hash";
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var newAccessToken = "new-access-token";
        var newRefreshToken = "new-refresh-token";
        var userEntity = new UserAccountEntity
        {
            Id = userId,
            Email = _faker.Internet.Email(),
            UserName = "user",
            NormalizedUserName = "user"
        };

        _jwtTokenService.Setup(j => j.EnsureRefreshTokenActiveForRotationAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _jwtTokenService.Setup(j => j.GetRefreshTokenAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenEntity { UserId = userId, FamilyId = familyId, TokenHash = "" });
        _jwtTokenService.Setup(j => j.GetClaimValueAsync(newRefreshToken, JwtRegisteredClaimNames.Jti))
            .ReturnsAsync("new-jti");
        _jwtTokenService.Setup(j => j.GenerateAccessTokenAsync(userId, familyId, It.IsAny<List<string>>(), It.IsAny<List<Claim>>()))
            .ReturnsAsync(newAccessToken);
        _jwtTokenService.Setup(j => j.GenerateRefreshTokenAsync(userId, familyId, It.IsAny<List<Claim>>()))
            .ReturnsAsync(newRefreshToken);
        _jwtTokenService.Setup(j => j.InvalidateRefreshTokenAsync(refreshTokenHash, "new-jti", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _jwtTokenService.Setup(j => j.AccessTokenExpiresInSeconds).Returns(3600);
        _userService.Setup(u => u.GetUserByAsync("Id", userId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userEntity);

        var step = new RefreshTokenStep
        {
            Kind = "refreshToken",
            RefreshTokenKey = "RefreshToken",
            Logger = _logger.Object,
            JwtTokenService = _jwtTokenService.Object,
            UserService = _userService.Object,
            AuthenticationOptions = new AuthenticationOptions(),
            Next = "done"
        };

        var bag = new Bag();
        bag.Set("refreshToken.RefreshToken", refreshTokenHash);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<string>("refreshToken.AccessToken").Should().Be(newAccessToken);
        bag.Get<string>("refreshToken.RefreshToken").Should().Be(newRefreshToken);
        bag.Get<string>("refreshToken.TokenType").Should().Be("Bearer");
    }

    [Test]
    public async Task GivenInvalidRefreshToken_WhenExecuteAsync_ThenThrowsNotAuthorizedExceptionAsync()
    {
        _jwtTokenService.Setup(j => j.EnsureRefreshTokenActiveForRotationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotAuthorizedException("Invalid or expired refresh token."));

        var step = new RefreshTokenStep
        {
            Kind = "refreshToken",
            RefreshTokenKey = "RefreshToken",
            Logger = _logger.Object,
            JwtTokenService = _jwtTokenService.Object,
            UserService = _userService.Object,
            AuthenticationOptions = new AuthenticationOptions(),
            Next = null
        };

        var bag = new Bag();
        bag.Set("refreshToken.RefreshToken", "invalid-hash");

        var act = async () => await step.ExecuteAsync(bag, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }

    [Test]
    public async Task GivenAlreadyUsedRefreshToken_WhenExecuteAsync_ThenThrowsConflictExceptionAsync()
    {
        _jwtTokenService.Setup(j => j.EnsureRefreshTokenActiveForRotationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Refresh token has already been used."));

        var step = new RefreshTokenStep
        {
            Kind = "refreshToken",
            RefreshTokenKey = "RefreshToken",
            Logger = _logger.Object,
            JwtTokenService = _jwtTokenService.Object,
            UserService = _userService.Object,
            AuthenticationOptions = new AuthenticationOptions(),
            Next = null
        };

        var bag = new Bag();
        bag.Set("refreshToken.RefreshToken", "already-used-hash");

        var act = async () => await step.ExecuteAsync(bag, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already been used*");
    }
}
