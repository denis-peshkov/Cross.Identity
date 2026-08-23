namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Sets the preferred communication endpoint for the user identified by <see cref="UserIdKey"/>.
/// Only verified endpoints are allowed; exactly one preferred endpoint per user.
/// </summary>
internal sealed class CommunicationEndpointSetPreferredStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string UserIdKey { get; init; }

    public required string EndpointIdKey { get; init; }

    public required string RefreshTokenKey { get; init; }

    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var userAccountId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserIdKey));
        var refreshToken = ctx.Get<string>(BagKey.Qualify(Kind, RefreshTokenKey));
        var raw = ctx.Get<object?>(BagKey.Qualify(Kind, EndpointIdKey));
        if (raw is null || !Guid.TryParse(raw.ToString(), out var endpointId) || endpointId == Guid.Empty)
            throw new ValidationException("EndpointId is required.");

        var clientContext = ClientContext.Read(ctx);
        await CommunicationEndpoints
            .SetPreferredAsync(userAccountId, endpointId, refreshToken, clientContext, cancellationToken)
            .ConfigureAwait(false);
        ctx.Set(BagKey.Qualify(Kind, "Preferred"), true);
        return StepResult.Ok(Next);
    }
}
