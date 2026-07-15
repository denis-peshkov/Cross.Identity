namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for password-based authentication.
/// Uses <see cref="IUserService"/> to look up the user and validate the password.
/// <para>
/// Key rules:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/> and <see cref="PasswordKey"/>:
///     if relative (no dot), are read as <c>"{Name}.{Key}"</c>;
///     to read data from another step, specify absolute keys such as <c>"other-step.Field"</c>.</description></item>
///   <item><description><see cref="UserIdKey"/> — if relative, the result is written as <c>"{Name}.{UserIdKey}"</c>.</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class PasswordAuthStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>User service for password verification.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Field used to look up the user (for example, "Email" or "UserName").</summary>
    public required string SelectorField { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> to read the selector value from (for example, "auth-form.Email").
    /// May be relative (qualified as <c>"{Kind}.SelectorKey"</c>) or absolute.
    /// </summary>
    public required string SelectorKey { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> to read the password from (for example, "auth-form.Password").
    /// May be relative (qualified as <c>"{Kind}.PasswordKey"</c>) or absolute.
    /// </summary>
    public required string PasswordKey { get; init; }

    /// <summary>
    /// Key for storing the user identifier in <see cref="Bag"/>.
    /// If the key is relative (no dot), it is saved as <c>"{Kind}.UserIdKey"</c>.
    /// Defaults to <c>"UserId"</c>.
    /// </summary>
    public string UserIdKey { get; init; } = "UserId";

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // read: qualify relative keys with the step name prefix
        var selectorValue = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));
        var password      = ctx.Get<string>(BagKey.Qualify(Kind, PasswordKey));

        // password validation
        var ok = await UserService.ValidatePasswordAsync(SelectorField, selectorValue, password, cancellationToken).ConfigureAwait(false);
        if (!ok)
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));

        // resolve the identifier
        var userId = await UserService.GetUserIdByAsync(SelectorField, selectorValue, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("User not found after password validation.");

        // write: relative key → "{Kind}.UserIdKey"
        ctx.Set(BagKey.Qualify(Kind, UserIdKey), userId);

        return StepResult.Ok(Next);
    }
}
