namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that lists OAuth providers and link status for the authenticated user.
/// </summary>
internal sealed class ExternalLoginGetAllStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required IExternalLoginService ExternalLoginService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var overview = await ExternalLoginService.GetAllAsync(cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "AccountEmail"), overview.AccountEmail);
        ctx.Set(BagKey.Qualify(Kind, "Providers"), overview.Providers);

        return StepResult.Ok(Next);
    }
}
