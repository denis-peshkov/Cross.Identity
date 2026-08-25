namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that lists OAuth providers and link status for the user identified by <see cref="UserAccountIdKey"/>.
/// </summary>
internal sealed class ExternalLoginGetAllStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string UserAccountIdKey { get; init; }

    public required IExternalLoginService ExternalLoginService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var userAccountId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserAccountIdKey));
        var overview = await ExternalLoginService.GetAllAsync(userAccountId, cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "AccountEmail"), overview.AccountEmail);
        ctx.Set(BagKey.Qualify(Kind, "Providers"), overview.Providers);

        return StepResult.Ok(Next);
    }
}
