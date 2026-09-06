namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for changing a user password by selector.
/// On success, notifies via <see cref="ICommunicationEndpointService.ResolveDeliveryTargetAsync"/>
/// using <see cref="INotificationComposer"/> and <see cref="ISecurityNotifier"/>.
/// Template language is selected via <see cref="HostSuppliedLanguageContext"/> (<c>collectForm.LanguageCode</c>).
/// </summary>
internal sealed class ResetPasswordStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Identity selector (bag keys for field name + value).</summary>
    public required Selector Selector { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the password from. May be relative or absolute.</summary>
    public required string PasswordKey { get; init; }

    /// <summary>Template name under Definitions/Templates (e.g. <c>password-changed</c>).</summary>
    public required string Template { get; init; }

    /// <summary>Notification subject line.</summary>
    public required string Subject { get; init; }

    public required ILogger Logger { get; init; }
    public required IUserService UserService { get; init; }
    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }
    public required INotificationComposer NotificationComposer { get; init; }
    public required ISecurityNotifier SecurityNotifier { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);

        var passwordValue = ctx.Get<string>(BagKey.Qualify(Kind, PasswordKey));
        var hostSuppliedClientContext = HostSuppliedClientContext.Read(ctx);

        await UserService.SetPasswordAsync(selector.Field, selector.Value, passwordValue, hostSuppliedClientContext, cancellationToken).ConfigureAwait(false);

        var userAccountId = await UserService.GetUserAccountIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        if (userAccountId is not { } resolvedUserAccountId || resolvedUserAccountId == Guid.Empty)
        {
            return StepResult.Ok(Next);
        }

        DeliveryTarget target;
        try
        {
            target = await CommunicationEndpoints
                .ResolveDeliveryTargetAsync(resolvedUserAccountId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ValidationException)
        {
            return StepResult.Ok(Next);
        }

        var channel = target.Channel.ToEmailOrSms();
        if (!channel.SupportsOtp())
        {
            return StepResult.Ok(Next);
        }

        var ip = string.IsNullOrWhiteSpace(hostSuppliedClientContext.IpAddress) ? "unknown" : hostSuppliedClientContext.IpAddress;
        var bodies = NotificationComposer.Compose(
            ctx,
            Template,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["{{changedAt}}"] = DateTime.UtcNow.ToString("u"),
                ["{{ip}}"] = ip,
            });

        try
        {
            await SecurityNotifier
                .SendAsync(channel, target.Address, Subject, bodies.TextBody, bodies.HtmlBody, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Kind} notification failed: {Message}", Kind, ex.Message);
        }

        return StepResult.Ok(Next);
    }
}
