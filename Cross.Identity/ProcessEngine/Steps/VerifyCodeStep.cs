namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for verifying a confirmation code (email/phone).
/// Uses <see cref="ICodeService"/> to validate the code.
/// <para>
/// Key rules:
/// <list type="bullet">
///   <item><description><see cref="IdentityKey"/> and <see cref="CodeKey"/>:
///     if relative (no dot), are read as <c>"{Name}.{Key}"</c>;
///     to read data from another step, specify absolute keys such as <c>"other-step.Field"</c>.</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class VerifyCodeStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Verification channel (<see cref="ChannelEnum.Email"/> / <see cref="ChannelEnum.Sms"/>, …).</summary>
    public required ChannelEnum Channel { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> for the identifier (email/phone/username).
    /// May be relative (qualified as <c>"{Kind}.IdentityKey"</c>) or absolute.
    /// </summary>
    public required string IdentityKey { get; init; }

    /// <summary>
    /// Optional key for phone (E.164). When set with <see cref="UserNameKey"/>, identity is resolved as email / phone / user name.
    /// </summary>
    public string? PhoneNumberKey { get; init; }

    /// <summary>
    /// Optional key for user name. Code verification requires email or phone channel (not user name alone).
    /// </summary>
    public string? UserNameKey { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> for the verification code.
    /// May be relative (qualified as <c>"{Kind}.CodeKey"</c>) or absolute.
    /// </summary>
    public required string CodeKey { get; init; }

    /// <summary>Code service.</summary>
    public required ICodeService CodeService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // relative keys → "{Kind}.{Key}"
        ChannelEnum channel;
        string identity;
        if (EmailOrPhoneBag.IsMultiSelector(PhoneNumberKey, UserNameKey))
        {
            ChannelEnum? resolvedChannel;
            (_, identity, resolvedChannel) = EmailOrPhoneBag.Resolve(ctx, Kind, IdentityKey, PhoneNumberKey, UserNameKey);
            if (resolvedChannel is null)
                throw new ValidationException("Provide an email or a phone number to verify a code.");
            channel = resolvedChannel.Value;
        }
        else
        {
            channel = Channel;
            identity = ctx.Get<string>(BagKey.Qualify(Kind, IdentityKey));
        }

        var code = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));

        var ok = await CodeService.VerifyAsync(channel, identity, code, cancellationToken).ConfigureAwait(false);

        return ok
            ? StepResult.Ok(Next)
            : StepResult.Fail(new NotAuthorizedException("Invalid or expired verification code."));
    }
}
