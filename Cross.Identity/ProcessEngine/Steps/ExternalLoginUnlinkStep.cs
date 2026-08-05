namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that unlinks an external OAuth provider from the authenticated user.
/// </summary>
internal sealed class ExternalLoginUnlinkStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string ProviderKey { get; init; }

    public required IExternalLoginService ExternalLoginService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var provider = ctx.Get<string>(BagKey.Qualify(Kind, ProviderKey));

        await ExternalLoginService.UnlinkAsync(provider, cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "Unlinked"), true);

        return StepResult.Ok(Next);
    }
}
