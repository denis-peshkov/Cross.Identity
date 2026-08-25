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
    public async Task GivenJti_WhenExecuteAsync_ThenRevokesAndSetsResultAsync()
    {
        var jti = Guid.NewGuid();
        _jwtTokenService
            .Setup(j => j.RevokeSessionForLogoutAsync(jti, HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new LogoutStep
        {
            Kind = "logout",
            JtiKey = "Jti",
            JwtTokenService = _jwtTokenService.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("logout.Jti", jti.ToString());
        bag.Set("collectForm.IpAddress", null);
        bag.Set("collectForm.UserAgent", null);
        bag.Set("collectForm.DeviceFingerprint", null);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<bool>("logout.Revoked").Should().BeTrue();
        _jwtTokenService.Verify(
            j => j.RevokeSessionForLogoutAsync(jti, HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
