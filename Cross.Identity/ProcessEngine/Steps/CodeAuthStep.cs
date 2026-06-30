namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for authenticating a user with a one-time code (OTP).
/// Uses <see cref="ICodeService"/> to verify the code and <see cref="IUserService"/> to look up the user.
/// <para>
/// Scenario:
/// <list type="number">
///   <item><c>collectForm</c> collects Email/Phone and a one-time code and writes them to Bag with its step name prefix (for example, <c>auth-form.Email</c>).</item>
///   <item><c>CodeAuthStep</c> reads identity and code via <see cref="IdentityKey"/> and <see cref="CodeKey"/>:
///       if the key is relative (no dot), it is automatically qualified as <c>"{Name}.{key}"</c>;
///       if absolute (with a dot), it is used as-is.</item>
///   <item>If the code is valid, looks up the user by <see cref="ResolveBy.Field"/> and stores the Id in Bag under <see cref="UserIdKey"/>
///       (relative key → <c>"{Name}.{UserIdKey}"</c>).</item>
///   <item>If the code is invalid or the user is not found, returns an error.</item>
/// </list>
/// </para>
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

    /// <summary>Verification channel (for example, "email" or "phone").</summary>
    public required string Channel { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> to read identity from (for example, <c>"auth-form.Email"</c>).
    /// May be relative (qualified as <c>"{Kind}.IdentityKey"</c>).
    /// </summary>
    public required string IdentityKey { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> to read the submitted code from.
    /// May be relative (qualified as <c>"{Kind}.CodeKey"</c>).
    /// </summary>
    public required string CodeKey { get; init; }

    /// <summary>User lookup settings: which field to search by (for example, "Email" or "Phone").</summary>
    public required ResolveBy ResolveBy { get; init; }

    /// <summary>
    /// Key for storing the user identifier.
    /// If the key is relative (no dot), it is saved as <c>"{Kind}.UserIdKey"</c>.
    /// Defaults to <c>"UserId"</c>.
    /// </summary>
    public string UserIdKey { get; init; } = "Id";

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // Read: qualify relative keys with the step name prefix
        var identity = ctx.Get<string>(BagKey.Qualify(Kind, IdentityKey));
        var code     = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));

        // 1) Verify the code
        var ok = await CodeService.VerifyAsync(Channel, identity, code, cancellationToken).ConfigureAwait(false);
        if (!ok)
            return StepResult.Fail(new NotAuthorizedException("Invalid or expired code."));

        // 2) Resolve the user
        var userId = await UserService.GetUserIdByAsync(ResolveBy.Field, identity, cancellationToken).ConfigureAwait(false);
        if (userId is null)
        {
            return StepResult.Fail(new KeyNotFoundException("User not found."));
        }

        // Write: relative key → "{Kind}.UserIdKey"
        ctx.Set(BagKey.Qualify(Kind, UserIdKey), userId);

        return StepResult.Ok(Next);
    }
}
