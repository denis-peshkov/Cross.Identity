namespace Cross.Identity.Tests.Services;

[Category(TestCategory.UNIT)]
[TestFixture]
public class ExpiredRefreshTokenCleanupHostedServiceTests
{
    [Test]
    public async Task GivenHostedService_WhenExecuteAsync_ThenInvokesCleanupAndStopsOnCancellationAsync()
    {
        var jwtTokenService = new Mock<IJwtTokenService>();
        jwtTokenService
            .Setup(j => j.CleanupExpiredRefreshTokensAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var provider = BuildProvider(jwtTokenService.Object);
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var options = new Mock<IOptionsMonitor<AuthenticationOptions>>();
        options.Setup(o => o.CurrentValue).Returns(new AuthenticationOptions
        {
            TokenCleanupInterval = TimeSpan.FromMilliseconds(30),
        });

        var sut = new ExpiredRefreshTokenCleanupHostedService(
            scopeFactory,
            options.Object,
            Mock.Of<ILogger<ExpiredRefreshTokenCleanupHostedService>>());

        using var cts = new CancellationTokenSource();
        await sut.StartAsync(cts.Token);
        await Task.Delay(120, CancellationToken.None);
        await cts.CancelAsync();
        await sut.StopAsync(CancellationToken.None);

        jwtTokenService.Verify(
            j => j.CleanupExpiredRefreshTokensAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task GivenCleanupFailure_WhenCleanupAsync_ThenLogsErrorAsync()
    {
        var jwtTokenService = new Mock<IJwtTokenService>();
        jwtTokenService
            .Setup(j => j.CleanupExpiredRefreshTokensAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("cleanup failed"));

        using var provider = BuildProvider(jwtTokenService.Object);
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var options = new Mock<IOptionsMonitor<AuthenticationOptions>>();
        options.Setup(o => o.CurrentValue).Returns(new AuthenticationOptions());

        var logger = new Mock<ILogger<ExpiredRefreshTokenCleanupHostedService>>();
        var sut = new ExpiredRefreshTokenCleanupHostedService(scopeFactory, options.Object, logger.Object);

        var cleanupAsync = typeof(ExpiredRefreshTokenCleanupHostedService)
            .GetMethod("CleanupAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var task = (Task)cleanupAsync.Invoke(sut, new object[] { CancellationToken.None })!;
        await task;

        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Failed to cleanup expired refresh tokens")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static ServiceProvider BuildProvider(IJwtTokenService jwtTokenService)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => jwtTokenService);
        return services.BuildServiceProvider();
    }
}
