namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Lists communication endpoints for the user identified by <see cref="UserAccountIdKey"/>.
/// The host must authorize the caller for that account; this step does not require a refresh token.
/// </summary>
internal sealed class CommunicationEndpointsGetAllStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string UserAccountIdKey { get; init; }

    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var userAccountId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserAccountIdKey));
        var list = await CommunicationEndpoints
            .GetAllAsync(userAccountId, cancellationToken)
            .ConfigureAwait(false);
        ctx.Set(BagKey.Qualify(Kind, "Endpoints"), list);
        return StepResult.Ok(Next);
    }
}
