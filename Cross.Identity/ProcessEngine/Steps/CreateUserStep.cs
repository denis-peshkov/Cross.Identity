namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for creating a new user in the system.
/// </summary>
internal sealed class CreateUserStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>User service.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>
    /// Dictionary mapping "user field → key in <see cref="Bag"/>".
    /// </summary>
    public required IDictionary<string, string> Map { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> where the created user's Id is stored.
    /// </summary>
    public string UserIdKey { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var userFields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (userField, bagKeyRaw) in Map)
        {
            var bagKey = BagKey.Qualify(Kind, bagKeyRaw);

            if (ctx.TryGet<object?>(bagKey, out var value))
                userFields[userField] = value;
        }

        var userId = await UserService.CreateUserAsync(userFields, cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, UserIdKey), userId);

        return StepResult.Ok(Next);
    }
}
