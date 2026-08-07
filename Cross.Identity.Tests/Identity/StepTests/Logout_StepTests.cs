namespace Cross.Identity.Tests.Identity.StepTests;

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
    [Category(TestCategory.UNIT)]
    public async Task GivenValidRefreshToken_WhenExecuteAsync_ThenRevokesAndSetsResultAsync()
    {
        var refreshToken = "refresh-token-value";
        _jwtTokenService
            .Setup(j => j.RevokeRefreshTokenForLogoutAsync(refreshToken, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new LogoutStep
        {
            Kind = "logout",
            RefreshTokenKey = "RefreshToken",
            IpAddressKey = "IpAddress",
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
            j => j.RevokeRefreshTokenForLogoutAsync(refreshToken, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
