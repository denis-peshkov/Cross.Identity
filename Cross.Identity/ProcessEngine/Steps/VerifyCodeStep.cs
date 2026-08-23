namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Verifies an OTP against the same delivery target used for send
/// (<see cref="ICommunicationEndpointService.ResolveOtpTargetAsync"/>) and writes the user id into the bag.
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

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);
        var code = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));

        var userIdRaw = await UserService.GetUserIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(userIdRaw) || !Guid.TryParse(userIdRaw, out var userId) || userId == Guid.Empty)
        {
            return StepResult.Fail(new KeyNotFoundException("User not found."));
        }

        var target = await CommunicationEndpoints
            .ResolveOtpTargetAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (!target.Channel.SupportsOtp())
        {
            throw new ValidationException("Provide an email or a phone number to verify a code.");
        }

        var ok = await CodeService.VerifyAsync(target.Channel, target.Address, code, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return StepResult.Fail(new NotAuthorizedException("Invalid or expired verification code."));
        }

        ctx.Set(BagKey.Qualify(Kind, UserIdKey), userIdRaw);
        return StepResult.Ok(Next);
    }
}
