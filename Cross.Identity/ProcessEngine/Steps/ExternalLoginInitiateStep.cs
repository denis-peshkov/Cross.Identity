namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that initiates external OAuth login: builds the provider authorization URL.
/// </summary>
internal sealed class ExternalLoginInitiateStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string ProviderKey { get; init; }

    public string? ReturnUrlKey { get; init; }

    public string? UserIdKey { get; init; }

    public required IExternalLoginService ExternalLoginService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var provider = ctx.Get<string>(BagKey.Qualify(Kind, ProviderKey));

        var returnUrl = string.IsNullOrWhiteSpace(ReturnUrlKey)
            ? null
            : ctx.Get<string?>(BagKey.Qualify(Kind, ReturnUrlKey));

        var linkUserId = string.IsNullOrWhiteSpace(UserIdKey)
            ? null
            : ctx.Get<Guid?>(BagKey.Qualify(Kind, UserIdKey));

        var url = await ExternalLoginService.InitiateAsync(provider, returnUrl, linkUserId, cancellationToken).ConfigureAwait(false);
        ctx.Set(BagKey.Qualify(Kind, "Url"), url);

        return StepResult.Ok(Next);
    }
}
