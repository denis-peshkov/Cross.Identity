namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Verifies an OTP and writes the resolved user id into the bag.
/// </summary>
internal sealed class VerifyCodeStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Default verification channel when not inferred from selector field.</summary>
    public required ChannelEnum Channel { get; init; }

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

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);
        // OTP verify uses Email/Sms storage keyed by login field (not preferred messenger).
        var channel = Selector.ChannelForField(selector.Field) ?? Channel;
        if (!channel.SupportsOtp())
            throw new ValidationException("Provide an email or a phone number to verify a code.");

        var code = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));

        bool ok;
        if (Selector.ChannelForField(selector.Field) is null)
            ok = await UserService.ValidateCodeAsync(selector.Field, selector.Value, code, cancellationToken).ConfigureAwait(false);
        else
            ok = await CodeService.VerifyAsync(channel, selector.Value, code, cancellationToken).ConfigureAwait(false);

        if (!ok)
            return StepResult.Fail(new NotAuthorizedException("Invalid or expired verification code."));

        var userId = await UserService.GetUserIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(userId))
            return StepResult.Fail(new KeyNotFoundException("User not found."));

        ctx.Set(BagKey.Qualify(Kind, UserIdKey), userId);
        return StepResult.Ok(Next);
    }
}
