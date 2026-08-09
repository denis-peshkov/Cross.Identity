namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>Lists communication endpoints for the authenticated user.</summary>
internal sealed class CommunicationEndpointsGetAllStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var endpoints = await CommunicationEndpoints.GetAllForCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        ctx.Set(BagKey.Qualify(Kind, "Endpoints"), endpoints);
        return StepResult.Ok(Next);
    }
}
