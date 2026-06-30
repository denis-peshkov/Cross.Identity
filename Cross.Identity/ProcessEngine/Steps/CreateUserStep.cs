namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for creating a new user in the system.
/// Uses <see cref="IUserService"/> to persist the user
/// and stores its identifier in <see cref="Bag"/>.
/// <para>
/// Scenario:
/// <list type="number">
///   <item><c>collectForm</c> collects registration data (email, userName, phone, etc.) and writes them to Bag with the step name prefix (for example, <c>reg-form.Email</c>).</item>
///   <item><c>CreateUserStep</c> maps this data into <see cref="Map"/>: values are read from Bag by key; the key may be
///       absolute (for example, <c>"reg-form.Email"</c>) or relative (then <c>"{Name}.Email"</c> is used).</item>
///   <item><see cref="IUserService.CreateUserAsync"/> creates the user and returns the Id.</item>
///   <item>The Id is stored in <see cref="Bag"/> under <see cref="UserIdKey"/> (a relative key is saved as <c>"{Name}.UserIdKey"</c>).</item>
/// </list>
/// </para>
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
    /// Values may be absolute (for example, <c>"reg-form.Email"</c>) or relative
    /// (in which case <c>"{Kind}.Email"</c> is used).
    /// <para>
    /// Example:
    /// <code language="json">
    /// "map": {
    ///   "Email": "reg-form.Email",
    ///   "UserName": "reg-form.UserName",
    ///   "Phone": "Phone" // relative → "{Kind}.Phone"
    /// }
    /// </code>
    /// </para>
    /// </summary>
    public required IDictionary<string, string> Map { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> where the created user's Id is stored.
    /// If the key is relative (no dot), it is saved as <c>"{Kind}.UserIdKey"</c>.
    /// Defaults to <c>"UserId"</c>.
    /// </summary>
    public string UserIdKey { get; init; }

    public string SelectorKey { get; set; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // Build the user value map from Bag
        var userFields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (userField, bagKeyRaw) in Map)
        {
            // relative key → "{Kind}.{bagKeyRaw}"
            var bagKey = BagKey.Qualify(Kind, bagKeyRaw);

            if (ctx.TryGet<object?>(bagKey, out var value))
                userFields[userField] = value;
        }

        // Create the user
        var userId = await UserService.CreateUserAsync(userFields, cancellationToken).ConfigureAwait(false);

        // Save the Id in Bag (relative → "{Kind}.UserIdKey")
        ctx.Set(BagKey.Qualify(Kind, UserIdKey), userId);
        ctx.TryGet<string>(SelectorKey, out var selectorKey);
        ctx.Set(BagKey.Qualify(Kind, "selectorKey"), selectorKey);

        return StepResult.Ok(Next);
    }
}
