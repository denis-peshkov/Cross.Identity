namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that looks up a user and publishes their identifier into the process context (<see cref="Bag"/>).
/// Looks up the user via <see cref="IUserService"/> by the specified field (Email / UserName / Phone / ...).
/// <para>
/// Keys:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/> — if relative (no dot), is read as <c>"{Name}.{SelectorKey}"</c>;</description></item>
///   <item><description><see cref="UserIdKey"/> — if relative, is written as <c>"{Name}.{UserIdKey}"</c>.</description></item>
///   <item><description>To read data from another step, specify an absolute key such as <c>"other-step.Field"</c>.</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class GetUserIdStep : IStep
{
    /// <inheritdoc />
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>User service.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Lookup field name: "Email" | "UserName" | "Phone" | ...</summary>
    public required string SelectorField { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> to read the selector value from (for example, <c>"auth-form.Email"</c>).
    /// May be relative (qualified as <c>"{Kind}.SelectorKey"</c>) or absolute.
    /// </summary>
    public required string SelectorKey { get; init; }

    /// <inheritdoc />
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // relative → "{Kind}.{SelectorKey}"
        var selectorValue = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));

        var userId = await UserService.GetUserIdByAsync(SelectorField, selectorValue, cancellationToken).ConfigureAwait(false);
        if (userId is null)
            return StepResult.Fail(new KeyNotFoundException("User not found."));

        // relative → "{Kind}.{UserIdKey}"
        ctx.Set(BagKey.Qualify(Kind, "UserId"), userId);

        return StepResult.Ok(Next);
    }
}
