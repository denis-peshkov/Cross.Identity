namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that looks up a user and publishes their identifier into the process context (<see cref="Bag"/>).
/// </summary>
internal sealed class GetUserIdStep : IStep
{
    /// <inheritdoc />
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>User service.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Identity selector (bag keys for field name + value).</summary>
    public required Selector Selector { get; init; }

    /// <inheritdoc />
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);

        var userId = await UserService.GetUserIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        if (userId is null)
            return StepResult.Fail(new KeyNotFoundException("User not found."));

        ctx.Set(BagKey.Qualify(Kind, "UserId"), userId);

        return StepResult.Ok(Next);
    }
}
