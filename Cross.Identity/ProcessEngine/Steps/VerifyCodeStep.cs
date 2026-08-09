namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for verifying a confirmation code (email/phone).
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

    /// <summary>Code service.</summary>
    public required ICodeService CodeService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);
        var channel = Selector.ChannelForField(selector.Field) ?? Channel;
        if (channel is not (ChannelEnum.Email or ChannelEnum.Sms))
            throw new ValidationException("Provide an email or a phone number to verify a code.");

        var code = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));

        var ok = await CodeService.VerifyAsync(channel, selector.Value, code, cancellationToken).ConfigureAwait(false);

        return ok
            ? StepResult.Ok(Next)
            : StepResult.Fail(new NotAuthorizedException("Invalid or expired verification code."));
    }
}
