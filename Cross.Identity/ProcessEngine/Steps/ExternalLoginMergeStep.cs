
namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that merges absorbed account B into survivor A using a one-time merge token.
/// </summary>
internal sealed class ExternalLoginMergeStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string MergeTokenKey { get; init; }

    public required string UserAccountIdKey { get; init; }

    public required IExternalLoginService ExternalLoginService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var mergeToken = ctx.Get<string>(BagKey.Qualify(Kind, MergeTokenKey));
        var survivorUserAccountId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserAccountIdKey));
        var hostSuppliedClientContext = HostSuppliedClientContext.Read(ctx);

        var result = await ExternalLoginService
            .MergeAccountsAsync(mergeToken, survivorUserAccountId, hostSuppliedClientContext, cancellationToken)
            .ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "SurvivorUserAccountId"), result.SurvivorUserAccountId);
        ctx.Set(BagKey.Qualify(Kind, "AbsorbedUserAccountId"), result.AbsorbedUserAccountId);
        ctx.Set(BagKey.Qualify(Kind, "MovedProviders"), result.MovedProviders);

        return StepResult.Ok(Next);
    }
}
