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

    /// <summary>Key in <see cref="Bag"/> to read the client IP from. May be relative or absolute.</summary>
    public required string IpAddressKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the User-Agent from. May be relative or absolute.</summary>
    public required string UserAgentKey { get; init; }

    public required IExternalLoginService ExternalLoginService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var provider = ctx.Get<string>(BagKey.Qualify(Kind, ProviderKey));
        var userId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserIdKey));
        ctx.TryGet<string?>(BagKey.Qualify(Kind, IpAddressKey), out var ipAddress);
        ctx.TryGet<string?>(BagKey.Qualify(Kind, UserAgentKey), out var userAgent);

        await ExternalLoginService.UnlinkAsync(provider, userId, ipAddress, userAgent, cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "Unlinked"), true);

        return StepResult.Ok(Next);
    }
}
