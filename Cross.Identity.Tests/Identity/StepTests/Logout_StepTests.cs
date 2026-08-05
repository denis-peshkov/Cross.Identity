namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class Logout_StepTests
{
    private Mock<IJwtTokenService> _jwtTokenService = null!;

    [SetUp]
    public void SetUp()
    {
        _jwtTokenService = new Mock<IJwtTokenService>();
    }

    [Test]
    public async Task LogoutStep_WhenValid_ShouldRevokeAndSetResult()
    {
        var refreshToken = "refresh-token-value";
        _jwtTokenService
            .Setup(j => j.RevokeRefreshTokenForLogoutAsync(refreshToken, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new LogoutStep
        {
            Kind = "logout",
            RefreshTokenKey = "RefreshToken",
            JwtTokenService = _jwtTokenService.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("logout.RefreshToken", refreshToken);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<bool>("logout.Revoked").Should().BeTrue();
        _jwtTokenService.Verify(
            j => j.RevokeRefreshTokenForLogoutAsync(refreshToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
