namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that completes external OAuth login: code exchange, account linking, and JWT issuance.
/// </summary>
internal sealed class ExternalLoginCompleteStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string CodeKey { get; init; }

    public required string StateKey { get; init; }

    public string? ErrorKey { get; init; }

    public string? ErrorDescriptionKey { get; init; }

    public required IExternalLoginService ExternalLoginService { get; init; }

    public required IJwtTokenService JwtTokenService { get; init; }

    public required IUserService UserService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // Code may be absent when the provider returned Error (OAuth error redirect).
        var code = ctx.Get<string?>(BagKey.Qualify(Kind, CodeKey));
        var state = ctx.Get<string>(BagKey.Qualify(Kind, StateKey));
        var error = !string.IsNullOrWhiteSpace(ErrorKey)
            ? ctx.Get<string?>(BagKey.Qualify(Kind, ErrorKey))
            : null;
        var errorDescription = !string.IsNullOrWhiteSpace(ErrorDescriptionKey)
            ? ctx.Get<string?>(BagKey.Qualify(Kind, ErrorDescriptionKey))
            : null;

        var completion = await ExternalLoginService.CompleteAsync(code ?? string.Empty, state, error, errorDescription, cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "UserId"), completion.UserId);
        ctx.Set(BagKey.Qualify(Kind, "IsLinking"), completion.IsLinking);

        if (completion.IsLinking)
        {
            return StepResult.Ok(Next);
        }

        var user = await UserService.GetUserByAsync("Id", completion.UserId.ToString(), cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(user);

        var client = ClientContext.Read(ctx);
        await TokenPairIssuer
            .IssueTokenPairAsync(JwtTokenService, ctx, Kind, user, Guid.NewGuid(), client, cancellationToken)
            .ConfigureAwait(false);

        return StepResult.Ok(Next);
    }
}
