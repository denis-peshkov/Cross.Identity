namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Lists communication endpoints for the user identified by <see cref="UserAccountIdKey"/>.
/// </summary>
internal sealed class CommunicationEndpointsGetAllStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string UserAccountIdKey { get; init; }

    public required string RefreshTokenKey { get; init; }

    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var userAccountId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserAccountIdKey));
        var refreshToken = ctx.Get<string>(BagKey.Qualify(Kind, RefreshTokenKey));

        var endpoints = await CommunicationEndpoints.GetAllAsync(userAccountId, refreshToken, cancellationToken).ConfigureAwait(false);
        ctx.Set(BagKey.Qualify(Kind, "Endpoints"), endpoints);
        return StepResult.Ok(Next);
    }
}
