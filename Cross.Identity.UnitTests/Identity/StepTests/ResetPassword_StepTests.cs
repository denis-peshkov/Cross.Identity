namespace Cross.Identity.UnitTests.Identity.StepTests;

[TestFixture]
public class ResetPassword_StepTests
{
    private Faker _faker = null!;
    private Mock<IUserService> _userService = null!;
    private Mock<IJwtTokenService> _jwtTokenService = null!;
    private Mock<ILogger> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _userService = new Mock<IUserService>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _logger = new Mock<ILogger>();
    }

    [Test]
    public async Task ResetPasswordStep_ShouldGetUserAndSetTokenTypeAndExpiresIn()
    {
        var email = _faker.Internet.Email();
        var password = "P@ssw0rd!";
        var userId = Guid.NewGuid();
        var userEntity = new UserAccountEntity
        {
            Id = userId,
            Email = email,
            NormalizedEmail = email.ToLowerInvariant(),
            UserName = "user",
            NormalizedUserName = "user"
        };

        _userService.Setup(u => u.GetUserByAsync("", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userEntity);
        _jwtTokenService.Setup(j => j.AccessTokenExpiresInSeconds).Returns(3600);

        var step = new ResetPasswordStep
        {
            Kind = "resetPassword",
            SelectorKey = "forgotPassword.email",
            PasswordKey = "forgotPassword.password",
            UserService = _userService.Object,
            JwtTokenService = _jwtTokenService.Object,
            Logger = _logger.Object,
            Channel = ChannelEnum.Email,
            TokenTypeKey = "TokenType",
            ExpiresInKey = "ExpiresIn",
            Next = "done"
        };

        var bag = new Bag();
        bag.Set("forgotPassword.email", email);
        bag.Set("forgotPassword.password", password);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<string>("resetPassword.TokenType").Should().Be("Bearer");
        bag.Get<object>("resetPassword.ExpiresIn").Should().NotBeNull();
    }
}
