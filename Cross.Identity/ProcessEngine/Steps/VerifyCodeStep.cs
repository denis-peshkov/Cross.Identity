namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Verifies an OTP against the same delivery target used for send
/// (<see cref="ICommunicationEndpointService.ResolveOtpTargetAsync"/>) and writes the user id into the bag.
/// Unknown identity / invalid code surface as <see cref="NotAuthorizedException"/> (<c>Invalid credentials.</c>);
/// the real reason is logged at Information (anti user-enumeration).
/// </summary>
internal sealed class VerifyCodeStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Identity selector (bag keys for field name + value).</summary>
    public required Selector Selector { get; init; }

    /// <summary>Key in <see cref="Bag"/> for the verification code.</summary>
    public required string CodeKey { get; init; }

    /// <summary>Key for storing the resolved user identifier.</summary>
    public string UserIdKey { get; init; } = "UserId";

    /// <summary>Code service.</summary>
    public required ICodeService CodeService { get; init; }

    /// <summary>User service (resolve id after successful verify).</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Resolves OTP delivery target (must match send).</summary>
    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }

    /// <summary>Logger (operational detail for rejected verifies).</summary>
    public required ILogger Logger { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);
        var code = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));

        var userAccountId = await UserService.GetUserIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        if (userAccountId is not { } resolvedUserAccountId || resolvedUserAccountId == Guid.Empty)
        {
            Logger.LogInformation(
                "Verify code rejected for {Field} identity {Identity}: user not found.",
                selector.Field,
                selector.Value);
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        DeliveryTarget target;
        try
        {
            target = await CommunicationEndpoints
                .ResolveOtpTargetAsync(resolvedUserAccountId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            Logger.LogInformation(
                "Verify code rejected for {Field} identity {Identity}: {Reason}",
                selector.Field,
                selector.Value,
                ex.Message);
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        if (!target.Channel.SupportsOtp())
        {
            Logger.LogInformation(
                "Verify code rejected for {Field} identity {Identity}: channel {Channel} does not support OTP.",
                selector.Field,
                selector.Value,
                target.Channel);
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        var ok = await CodeService.VerifyAsync(resolvedUserAccountId, target.Channel, target.Address, code, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            Logger.LogInformation(
                "Verify code rejected for {Field} identity {Identity}: invalid or expired verification code.",
                selector.Field,
                selector.Value);
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        ctx.Set(BagKey.Qualify(Kind, UserIdKey), resolvedUserAccountId.ToString());
        return StepResult.Ok(Next);
    }
}
