namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class VerifyToken_StepTests
{
    private Mock<IJwtTokenService> _jwtTokenService = null!;

    [SetUp]
    public void SetUp()
    {
        _jwtTokenService = new Mock<IJwtTokenService>();
    }

    [Test]
    public async Task GivenValidAccessToken_WhenExecuteAsync_ThenSetsValidAndClaimsAsync()
    {
        var accessToken = "access-token-value";
        var userId = Guid.NewGuid();
        var jti = Guid.NewGuid().ToString();

        _jwtTokenService
            .Setup(j => j.ValidateAccessTokenAsync(accessToken))
            .ReturnsAsync(true);
        _jwtTokenService
            .Setup(j => j.GetClaimValueAsync(accessToken, JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier))
            .ReturnsAsync(userId.ToString());
        _jwtTokenService
            .Setup(j => j.GetClaimValueAsync(accessToken, JwtRegisteredClaimNames.Jti))
            .ReturnsAsync(jti);

        var step = new VerifyTokenStep
        {
            Kind = "verifyToken",
            AccessTokenKey = "AccessToken",
            JwtTokenService = _jwtTokenService.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("verifyToken.AccessToken", accessToken);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<bool>("verifyToken.Valid").Should().BeTrue();
        bag.Get<Guid>("verifyToken.UserId").Should().Be(userId);
        bag.Get<string>("verifyToken.Jti").Should().Be(jti);
    }

    [Test]
    public async Task GivenInvalidAccessToken_WhenExecuteAsync_ThenSetsValidFalseAsync()
    {
        var accessToken = "access-token-value";
        _jwtTokenService
            .Setup(j => j.ValidateAccessTokenAsync(accessToken))
            .ReturnsAsync(false);

        var step = new VerifyTokenStep
        {
            Kind = "verifyToken",
            AccessTokenKey = "AccessToken",
            JwtTokenService = _jwtTokenService.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("verifyToken.AccessToken", accessToken);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<bool>("verifyToken.Valid").Should().BeFalse();
        bag.TryGet<Guid>("verifyToken.UserId", out _).Should().BeFalse();
        _jwtTokenService.Verify(
            j => j.GetClaimValueAsync(It.IsAny<string>(), It.IsAny<string[]>()),
            Times.Never);
    }

    [Test]
    public async Task GivenMalformedAccessToken_WhenExecuteAsync_ThenSetsValidFalseAsync()
    {
        var accessToken = "not-a-jwt";
        _jwtTokenService
            .Setup(j => j.ValidateAccessTokenAsync(accessToken))
            .ThrowsAsync(new ArgumentException("Not a JWT token."));

        var step = new VerifyTokenStep
        {
            Kind = "verifyToken",
            AccessTokenKey = "AccessToken",
            JwtTokenService = _jwtTokenService.Object,
            Next = null,
        };

        var bag = new Bag();
        bag.Set("verifyToken.AccessToken", accessToken);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<bool>("verifyToken.Valid").Should().BeFalse();
    }
}
