namespace Cross.Identity.Services;

/// <summary>
/// Periodically deletes refresh tokens whose <see cref="RefreshTokenEntity.AbsoluteExpiresAt"/> has expired.
/// </summary>
internal sealed class ExpiredRefreshTokenCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<AuthenticationOptions> _options;
    private readonly ILogger<ExpiredRefreshTokenCleanupHostedService> _logger;

    public ExpiredRefreshTokenCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<AuthenticationOptions> options,
        ILogger<ExpiredRefreshTokenCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await CleanupAsync(cancellationToken).ConfigureAwait(false);

            var interval = _options.CurrentValue.TokenCleanupInterval;
            if (interval <= TimeSpan.Zero)
            {
                interval = TimeSpan.FromHours(1);
            }

            try
            {
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            await jwtTokenService.CleanupExpiredRefreshTokensAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired refresh tokens.");
        }
    }
}
