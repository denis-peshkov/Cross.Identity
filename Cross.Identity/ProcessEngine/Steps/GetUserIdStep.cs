namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that looks up a user and publishes their identifier into the process context (<see cref="Bag"/>).
/// Unknown identity surfaces as <see cref="NotAuthorizedException"/> (<c>Invalid credentials.</c>);
/// the real reason is logged at Information (anti user-enumeration).
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

    /// <summary>Logger (operational detail for rejected lookups).</summary>
    public required ILogger Logger { get; init; }

    /// <inheritdoc />
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);

        var userId = await UserService.GetUserIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        if (userId is not { } resolvedUserId || resolvedUserId == Guid.Empty)
        {
            Logger.LogInformation(
                "Get user id rejected for {Field} identity {Identity}: user not found.",
                selector.Field,
                selector.Value);
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        ctx.Set(BagKey.Qualify(Kind, "UserId"), resolvedUserId.ToString());

        return StepResult.Ok(Next);
    }
}
