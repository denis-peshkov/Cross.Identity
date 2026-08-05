namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
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
    public async Task LogoutAllStep_WhenValid_ShouldRevokeAndSetResult()
    {
        var refreshToken = "refresh-token-value";
        _jwtTokenService
            .Setup(j => j.RevokeAllTokensForLogoutAsync(refreshToken, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new LogoutAllStep
        {
            Kind = "logoutAll",
            RefreshTokenKey = "RefreshToken",
            JwtTokenService = _jwtTokenService.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("logoutAll.RefreshToken", refreshToken);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<bool>("logoutAll.Revoked").Should().BeTrue();
        _jwtTokenService.Verify(
            j => j.RevokeAllTokensForLogoutAsync(refreshToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task LogoutAllStep_WhenServiceThrows_ShouldPropagate()
    {
        _jwtTokenService
            .Setup(j => j.RevokeAllTokensForLogoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotAuthorizedException("Invalid or expired refresh token."));

        var step = new LogoutAllStep
        {
            Kind = "logoutAll",
            RefreshTokenKey = "RefreshToken",
            JwtTokenService = _jwtTokenService.Object,
            Next = null,
        };

        var bag = new Bag();
        bag.Set("logoutAll.RefreshToken", "bad-token");

        var act = async () => await step.ExecuteAsync(bag, CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }
}
