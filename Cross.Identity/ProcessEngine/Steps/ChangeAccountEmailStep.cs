namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Changes the account primary email for the user identified by <see cref="UserAccountIdKey"/>.
/// When the address matches a linked external provider email, it is auto-verified.
/// The host must authorize the caller for that account; this step does not require a refresh token.
/// </summary>
internal sealed class ChangeAccountEmailStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string UserAccountIdKey { get; init; }

    public required string EmailKey { get; init; }

    public required IUserService UserService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var userAccountId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserAccountIdKey));
        var email = ctx.Get<string>(BagKey.Qualify(Kind, EmailKey));
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        var hostSuppliedClientContext = HostSuppliedClientContext.Read(ctx);
        var endpoint = await UserService
            .ChangeAccountEmailAsync(userAccountId, email, hostSuppliedClientContext, cancellationToken)
            .ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "Endpoint"), endpoint);
        ctx.Set(BagKey.Qualify(Kind, "Email"), endpoint.Address);
        ctx.Set(BagKey.Qualify(Kind, "EmailVerified"), endpoint.IsVerified);

        return StepResult.Ok(Next);
    }
}
