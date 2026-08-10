namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for changing a user password by selector.
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

    public ILogger Logger { get; set; }
    public IUserService UserService { get; set; }
    public IEmailSenderService EmailSenderService { get; set; }
    public ISmsSenderService SmsSenderService { get; set; }
    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }
    public required ChannelEnum Channel { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);

        var passwordValue = ctx.Get<string>(BagKey.Qualify(Kind, PasswordKey));
        var clientContext = ClientContext.Read(ctx);

        await UserService.SetPasswordAsync(selector.Field, selector.Value, passwordValue, clientContext, cancellationToken).ConfigureAwait(false);

        var userIdRaw = await UserService.GetUserIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        if (!Guid.TryParse(userIdRaw, out var userId) || userId == Guid.Empty)
            return StepResult.Ok(Next);

        var preferred = await CommunicationEndpoints.GetPreferredAsync(userId, cancellationToken).ConfigureAwait(false);
        var channel = preferred?.Channel
                      ?? await CommunicationEndpoints
                          .ResolveDeliveryChannelAsync(userId, selector.Field, selector.Value, Channel, cancellationToken)
                          .ConfigureAwait(false);

        channel = channel.ToEmailOrSms();
        if (!channel.SupportsOtp())
            return StepResult.Ok(Next);

        var notifyAddress = preferred?.Address ?? selector.Value;

        var ip = string.IsNullOrWhiteSpace(clientContext.IpAddress) ? "unknown" : clientContext.IpAddress;
        var changedAt = DateTime.UtcNow.ToString("u");
        var subject = "Password changed";
        var textBody = $"Your password was changed at {changedAt} from IP {ip}. If this wasn't you, contact support immediately.";
        var htmlBody = $"<p>Your password was changed at <strong>{changedAt}</strong> from IP <strong>{ip}</strong>.</p><p>If this wasn't you, contact support immediately.</p>";

        try
        {
            switch (channel)
            {
                case ChannelEnum.Email:
                    await EmailSenderService.SendAsync("", notifyAddress, subject, textBody, htmlBody, cancellationToken).ConfigureAwait(false);
                    break;
                case ChannelEnum.Sms:
                    await SmsSenderService.SendAsync(notifyAddress, textBody, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Kind} notification failed: {Message}", Kind, ex.Message);
        }

        return StepResult.Ok(Next);
    }
}
