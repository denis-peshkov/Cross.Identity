namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class ExternalLoginComplete_StepTests
{
    private Mock<IExternalLoginService> _externalLoginService = null!;
    private Mock<IJwtTokenService> _jwtTokenService = null!;
    private Mock<IUserService> _userService = null!;

    [SetUp]
    public void SetUp()
    {
        _externalLoginService = new Mock<IExternalLoginService>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _userService = new Mock<IUserService>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenLinkingCompletion_WhenExecuteAsync_ThenSkipsTokenGenerationAsync()
    {
        var userAccountId = Guid.NewGuid();
        _externalLoginService
            .Setup(s => s.CompleteAsync("code", "state", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalLoginCompletion(userAccountId, true));

        var step = CreateStep();
        var bag = CreateBag();

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<Guid>("externalLoginComplete.UserAccountId").Should().Be(userAccountId);
        bag.Get<bool>("externalLoginComplete.IsLinking").Should().BeTrue();
        bag.ContainsKey("externalLoginComplete.AccessToken").Should().BeFalse();
        _jwtTokenService.Verify(
            j => j.GenerateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<List<string>>(), It.IsAny<List<Claim>>(), ClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenSuccessfulLogin_WhenExecuteAsync_ThenIssuesTokensAsync()
    {
        var userAccountId = Guid.NewGuid();
        var user = new UserAccountEntity
        {
            Id = userAccountId,
            Email = "user@example.com",
            UserName = "user",
            NormalizedUserName = "user",
        };

        _externalLoginService
            .Setup(s => s.CompleteAsync("code", "state", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalLoginCompletion(userAccountId, false));
        _userService
            .Setup(u => u.GetUserByAsync("Id", userAccountId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtTokenService.Setup(j => j.AccessTokenExpiresInSeconds).Returns(3600);
        _jwtTokenService
            .Setup(j => j.GenerateAccessTokenAsync(userAccountId, It.IsAny<Guid>(), It.IsAny<List<string>>(), It.IsAny<List<Claim>>(), ClientContext.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");
        _jwtTokenService
            .Setup(j => j.GenerateRefreshTokenAsync(userAccountId, It.IsAny<Guid>(), It.IsAny<List<Claim>>(), ClientContext.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");

        var step = CreateStep();
        var bag = CreateBag();

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("externalLoginComplete.AccessToken").Should().Be("access-token");
        bag.Get<string>("externalLoginComplete.RefreshToken").Should().Be("refresh-token");
        bag.Get<string>("externalLoginComplete.TokenType").Should().Be("Bearer");
        bag.Get<int>("externalLoginComplete.ExpiresIn").Should().Be(3600);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenOAuthError_WhenExecuteAsync_ThenForwardsValidationExceptionAsync()
    {
        _externalLoginService
            .Setup(s => s.CompleteAsync("code", "state", "access_denied", "Denied", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Denied"));

        var step = new ExternalLoginCompleteStep
        {
            Kind = "externalLoginComplete",
            CodeKey = "Code",
            StateKey = "State",
            ErrorKey = "Error",
            ErrorDescriptionKey = "ErrorDescription",
            ExternalLoginService = _externalLoginService.Object,
            JwtTokenService = _jwtTokenService.Object,
            UserService = _userService.Object,
        };

        var bag = new Bag();
        bag.Set("externalLoginComplete.Code", "code");
        bag.Set("externalLoginComplete.State", "state");
        bag.Set("externalLoginComplete.Error", "access_denied");
        bag.Set("externalLoginComplete.ErrorDescription", "Denied");

        await FluentActions.Invoking(async () => await step.ExecuteAsync(bag, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>().WithMessage("Denied");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenOAuthErrorWithoutCode_WhenExecuteAsync_ThenForwardsValidationExceptionAsync()
    {
        _externalLoginService
            .Setup(s => s.CompleteAsync(string.Empty, "state", "access_denied", "Denied", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Denied"));

        var step = new ExternalLoginCompleteStep
        {
            Kind = "externalLoginComplete",
            CodeKey = "Code",
            StateKey = "State",
            ErrorKey = "Error",
            ErrorDescriptionKey = "ErrorDescription",
            ExternalLoginService = _externalLoginService.Object,
            JwtTokenService = _jwtTokenService.Object,
            UserService = _userService.Object,
        };

        var bag = new Bag();
        bag.Set("externalLoginComplete.Code", null);
        bag.Set("externalLoginComplete.State", "state");
        bag.Set("externalLoginComplete.Error", "access_denied");
        bag.Set("externalLoginComplete.ErrorDescription", "Denied");

        await FluentActions.Invoking(async () => await step.ExecuteAsync(bag, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>().WithMessage("Denied");
    }

    private ExternalLoginCompleteStep CreateStep()
        => new()
        {
            Kind = "externalLoginComplete",
            CodeKey = "Code",
            StateKey = "State",
            ExternalLoginService = _externalLoginService.Object,
            JwtTokenService = _jwtTokenService.Object,
            UserService = _userService.Object,
            Next = "done",
        };

    private static Bag CreateBag()
    {
        var bag = new Bag();
        bag.Set("externalLoginComplete.Code", "code");
        bag.Set("externalLoginComplete.State", "state");
        bag.Set("collectForm.IpAddress", null);
        bag.Set("collectForm.UserAgent", null);
        bag.Set("collectForm.DeviceFingerprint", null);
        return bag;
    }
}
