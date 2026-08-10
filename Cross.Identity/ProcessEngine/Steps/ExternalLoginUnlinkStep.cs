namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that unlinks an external OAuth provider from the user identified by <see cref="UserIdKey"/>.
/// </summary>
internal sealed class ExternalLoginUnlinkStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string ProviderKey { get; init; }

    public required string UserIdKey { get; init; }

    public required IExternalLoginService ExternalLoginService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var provider = ctx.Get<string>(BagKey.Qualify(Kind, ProviderKey));
        var userId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserIdKey));
        var client = ClientContext.Read(ctx);

        await ExternalLoginService.UnlinkAsync(provider, userId, client.IpAddress, client.UserAgent, client.DeviceFingerprint, cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "Unlinked"), true);

        return StepResult.Ok(Next);
    }
}
