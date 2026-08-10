namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Lists communication endpoints for the user identified by <see cref="UserIdKey"/>.
/// </summary>
internal sealed class CommunicationEndpointsGetAllStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string UserIdKey { get; init; }

    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var userId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserIdKey));

        var endpoints = await CommunicationEndpoints.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        ctx.Set(BagKey.Qualify(Kind, "Endpoints"), endpoints);
        return StepResult.Ok(Next);
    }
}
