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

    /// <summary>Verification channel: "email" or "phone".</summary>
    public required string Channel { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> for the identifier (email/phone/username).
    /// May be relative (qualified as <c>"{Kind}.IdentityKey"</c>) or absolute.
    /// </summary>
    public required string IdentityKey { get; init; }

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
        var identity = ctx.Get<string>(BagKey.Qualify(Kind, IdentityKey));
        var code     = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));

        var ok = await CodeService.VerifyAsync(Channel, identity, code, cancellationToken).ConfigureAwait(false);

        return ok
            ? StepResult.Ok(Next)
            : StepResult.Fail(new NotAuthorizedException("Invalid or expired verification code."));
    }
}
