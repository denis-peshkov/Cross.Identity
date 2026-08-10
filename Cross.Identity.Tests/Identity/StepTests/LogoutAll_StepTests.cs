namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class LogoutAll_StepTests
{
    private Mock<IJwtTokenService> _jwtTokenService = null!;

    [SetUp]
    public void SetUp()
    {
        _jwtTokenService = new Mock<IJwtTokenService>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenValidRefreshToken_WhenExecuteAsync_ThenRevokesAllAndSetsResultAsync()
    {
        var refreshToken = "refresh-token-value";
        _jwtTokenService
            .Setup(j => j.RevokeAllTokensForLogoutAsync(refreshToken, null, null, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new LogoutAllStep
        {
            Kind = "logoutAll",
            RefreshTokenKey = "RefreshToken",
            IpAddressKey = "IpAddress",
            UserAgentKey = "UserAgent",
            DeviceFingerprintKey = "DeviceFingerprint",
            JwtTokenService = _jwtTokenService.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("logoutAll.RefreshToken", refreshToken);
        bag.Set("logoutAll.IpAddress", null);
        bag.Set("logoutAll.UserAgent", null);
        bag.Set("logoutAll.DeviceFingerprint", null);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<bool>("logoutAll.Revoked").Should().BeTrue();
        _jwtTokenService.Verify(
            j => j.RevokeAllTokensForLogoutAsync(refreshToken, null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenInvalidRefreshToken_WhenExecuteAsync_ThenPropagatesNotAuthorizedExceptionAsync()
    {
        _jwtTokenService
            .Setup(j => j.RevokeAllTokensForLogoutAsync(It.IsAny<string>(), null, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotAuthorizedException("Invalid or expired refresh token."));

        var step = new LogoutAllStep
        {
            Kind = "logoutAll",
            RefreshTokenKey = "RefreshToken",
            IpAddressKey = "IpAddress",
            UserAgentKey = "UserAgent",
            DeviceFingerprintKey = "DeviceFingerprint",
            JwtTokenService = _jwtTokenService.Object,
            Next = null,
        };

        var bag = new Bag();
        bag.Set("logoutAll.RefreshToken", "bad-token");
        bag.Set("logoutAll.IpAddress", null);
        bag.Set("logoutAll.UserAgent", null);
        bag.Set("logoutAll.DeviceFingerprint", null);

        var act = async () => await step.ExecuteAsync(bag, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }
}
