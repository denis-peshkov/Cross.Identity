namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for password-based authentication.
/// </summary>
internal sealed class PasswordAuthStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>User service for password verification.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Identity selector (bag keys for field name + value).</summary>
    public required Selector Selector { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the password from.</summary>
    public required string PasswordKey { get; init; }

    /// <summary>Key for storing the user identifier in <see cref="Bag"/>.</summary>
    public string UserIdKey { get; init; } = "UserId";

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);
        var password = ctx.Get<string>(BagKey.Qualify(Kind, PasswordKey));

        var ok = await UserService.ValidatePasswordAsync(selector.Field, selector.Value, password, cancellationToken).ConfigureAwait(false);
        if (!ok)
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));

        var userId = await UserService.GetUserIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("User not found after password validation.");

        ctx.Set(BagKey.Qualify(Kind, UserIdKey), userId);

        return StepResult.Ok(Next);
    }
}
