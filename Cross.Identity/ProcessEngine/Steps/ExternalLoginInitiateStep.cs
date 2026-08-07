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

        string? returnUrl = null;
        if (!string.IsNullOrWhiteSpace(ReturnUrlKey))
        {
            ctx.TryGet(BagKey.Qualify(Kind, ReturnUrlKey), out returnUrl);
        }

        Guid? linkUserId = null;
        if (!string.IsNullOrWhiteSpace(UserIdKey))
        {
            if (!TryReadUserId(ctx, UserIdKey, out linkUserId)
                && !TryReadUserId(ctx, BagKey.Qualify(Kind, UserIdKey), out linkUserId))
            {
                linkUserId = null;
            }
        }

        var url = await ExternalLoginService.InitiateAsync(provider, returnUrl, linkUserId, cancellationToken).ConfigureAwait(false);
        ctx.Set(BagKey.Qualify(Kind, "Url"), url);

        return StepResult.Ok(Next);
    }

    private static bool TryReadUserId(Bag ctx, string key, out Guid? linkUserId)
    {
        linkUserId = null;
        if (!ctx.TryGet<object?>(key, out var linkUserIdRaw) || linkUserIdRaw is null)
        {
            return false;
        }

        if (linkUserIdRaw is Guid guid)
        {
            linkUserId = guid;
            return true;
        }

        if (Guid.TryParse(linkUserIdRaw.ToString(), out var parsed))
        {
            linkUserId = parsed;
            return true;
        }

        return false;
    }
}
