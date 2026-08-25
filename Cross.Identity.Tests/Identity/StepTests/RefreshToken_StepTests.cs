namespace Cross.Identity.Tests.Identity.StepTests;

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
    [Category(TestCategory.UNIT)]
    public async Task GivenValidRefreshTokenJti_WhenExecuteAsync_ThenSetsAccessAndRefreshTokensAsync()
    {
        var refreshTokenJti = Guid.NewGuid();
        var newRefreshTokenJti = Guid.NewGuid();
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var newAccessToken = "new-access-token";
        var newRefreshToken = "new-refresh-token";
        var userEntity = new UserAccountEntity
        {
            Id = userAccountId,
            Email = _faker.Internet.Email(),
            UserName = "user",
            NormalizedUserName = "user"
        };

        _jwtTokenService.Setup(j => j.EnsureRefreshTokenActiveForRotationAsync(refreshTokenJti, HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _jwtTokenService.Setup(j => j.GetRefreshTokenByIdAsync(refreshTokenJti, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenEntity { Id = refreshTokenJti, UserAccountId = userAccountId, UserAccount = null!, FamilyId = familyId, TokenHash = "" });
        _jwtTokenService.Setup(j => j.GetClaimValue(newRefreshToken, JwtRegisteredClaimNames.Jti))
            .Returns(newRefreshTokenJti.ToString());
        _jwtTokenService.Setup(j => j.GenerateAccessTokenAsync(userAccountId, familyId, It.IsAny<List<string>>(), It.IsAny<List<Claim>>(), HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newAccessToken);
        _jwtTokenService.Setup(j => j.GenerateRefreshTokenAsync(userAccountId, familyId, It.IsAny<List<Claim>>(), HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newRefreshToken);
        _jwtTokenService.Setup(j => j.InvalidateRefreshTokenAsync(refreshTokenJti, newRefreshTokenJti, HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _jwtTokenService.Setup(j => j.AccessTokenExpiresInSeconds).Returns(3600);
        _userService.Setup(u => u.GetUserByAsync("Id", userAccountId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userEntity);

        var step = new RefreshTokenStep
        {
            Kind = "refreshToken",
            JtiKey = "Jti",
            JwtTokenService = _jwtTokenService.Object,
            UserService = _userService.Object,
            AuthenticationOptions = new AuthenticationOptions(),
            Next = "done"
        };

        var bag = new Bag();
        bag.Set("refreshToken.Jti", refreshTokenJti);
        bag.Set("collectForm.IpAddress", null);
        bag.Set("collectForm.UserAgent", null);
        bag.Set("collectForm.DeviceFingerprint", null);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<string>("refreshToken.AccessToken").Should().Be(newAccessToken);
        bag.Get<string>("refreshToken.RefreshToken").Should().Be(newRefreshToken);
        bag.Get<string>("refreshToken.TokenType").Should().Be("Bearer");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenInvalidRefreshTokenJti_WhenExecuteAsync_ThenThrowsNotAuthorizedExceptionAsync()
    {
        _jwtTokenService.Setup(j => j.EnsureRefreshTokenActiveForRotationAsync(It.IsAny<Guid>(), HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotAuthorizedException("Invalid or expired refresh token."));

        var step = new RefreshTokenStep
        {
            Kind = "refreshToken",
            JtiKey = "Jti",
            JwtTokenService = _jwtTokenService.Object,
            UserService = _userService.Object,
            AuthenticationOptions = new AuthenticationOptions(),
            Next = null
        };

        var bag = new Bag();
        bag.Set("refreshToken.Jti", Guid.NewGuid());
        bag.Set("collectForm.IpAddress", null);
        bag.Set("collectForm.UserAgent", null);
        bag.Set("collectForm.DeviceFingerprint", null);

        var act = async () => await step.ExecuteAsync(bag, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenAlreadyUsedRefreshTokenJti_WhenExecuteAsync_ThenThrowsConflictExceptionAsync()
    {
        _jwtTokenService.Setup(j => j.EnsureRefreshTokenActiveForRotationAsync(It.IsAny<Guid>(), HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Refresh token has already been used."));

        var step = new RefreshTokenStep
        {
            Kind = "refreshToken",
            JtiKey = "Jti",
            JwtTokenService = _jwtTokenService.Object,
            UserService = _userService.Object,
            AuthenticationOptions = new AuthenticationOptions(),
            Next = null
        };

        var bag = new Bag();
        bag.Set("refreshToken.Jti", Guid.NewGuid());
        bag.Set("collectForm.IpAddress", null);
        bag.Set("collectForm.UserAgent", null);
        bag.Set("collectForm.DeviceFingerprint", null);

        var act = async () => await step.ExecuteAsync(bag, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already been used*");
    }
}
