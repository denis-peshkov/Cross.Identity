namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for authenticating a user with a one-time code (OTP).
/// </summary>
internal sealed class CodeAuthStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>One-time code service.</summary>
    public required ICodeService CodeService { get; init; }

    /// <summary>User service.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Verification channel (<see cref="ChannelEnum.Email"/> / <see cref="ChannelEnum.Sms"/>, …).</summary>
    public required ChannelEnum Channel { get; init; }

    /// <summary>Identity selector (bag keys for field name + value).</summary>
    public required Selector Selector { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the submitted code from.</summary>
    public required string CodeKey { get; init; }

    /// <summary>Key for storing the user identifier.</summary>
    public string UserIdKey { get; init; } = "Id";

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);
        var code = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));
        // OTP verify uses Email/Sms storage keyed by login field (preferred messenger is send-side only).
        var channel = Selector.ChannelForField(selector.Field) ?? Channel;

        var ok = await CodeService.VerifyAsync(channel, selector.Value, code, cancellationToken).ConfigureAwait(false);
        if (!ok)
            return StepResult.Fail(new NotAuthorizedException("Invalid or expired code."));

        var userId = await UserService.GetUserIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        if (userId is null)
        {
            return StepResult.Fail(new KeyNotFoundException("User not found."));
        }

        ctx.Set(BagKey.Qualify(Kind, UserIdKey), userId);

        return StepResult.Ok(Next);
    }
}
