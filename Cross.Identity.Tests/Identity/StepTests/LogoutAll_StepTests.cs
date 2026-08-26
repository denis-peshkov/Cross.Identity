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
    public async Task GivenUserAccountId_WhenExecuteAsync_ThenRevokesAllAndSetsResultAsync()
    {
        var userAccountId = Guid.NewGuid();
        _jwtTokenService
            .Setup(j => j.RevokeAllTokensForUserAsync(
                userAccountId,
                RefreshTokenRevokedReason.USER_LOGOUT_ALL,
                HostSuppliedClientContext.Empty,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new LogoutAllStep
        {
            Kind = "logoutAll",
            UserAccountIdKey = "UserAccountId",
            JwtTokenService = _jwtTokenService.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("logoutAll.UserAccountId", userAccountId.ToString());
        bag.Set("collectForm.IpAddress", null);
        bag.Set("collectForm.UserAgent", null);
        bag.Set("collectForm.DeviceFingerprint", null);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<bool>("logoutAll.Revoked").Should().BeTrue();
        _jwtTokenService.Verify(
            j => j.RevokeAllTokensForUserAsync(
                userAccountId,
                RefreshTokenRevokedReason.USER_LOGOUT_ALL,
                HostSuppliedClientContext.Empty,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
