namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг смены пароля пользователя по селектору.
/// </summary>
internal sealed class ResetPasswordStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Ключ в <see cref="Bag"/>, откуда взять e-mail/логин. Может быть относительным или абсолютным.</summary>
    public required string SelectorKey { get; init; }

    /// <summary>Ключ в <see cref="Bag"/>, откуда взять пароль. Может быть относительным или абсолютным.</summary>
    public required string PasswordKey { get; init; }

    public ILogger Logger { get; set; }
    public IUserService UserService { get; set; }
    public IEmailSenderService EmailSenderService { get; set; }
    public ISmsSenderService SmsSenderService { get; set; }
    public IHttpContextAccessor HttpContextAccessor { get; set; }
    public required ChannelEnum Channel { get; init; }
    public required ResolveBy ResolveBy { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selectorValue = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));
        var passwordValue = ctx.Get<string>(BagKey.Qualify(Kind, PasswordKey));

        await UserService.SetPasswordAsync(ResolveBy.Field, selectorValue, passwordValue, cancellationToken).ConfigureAwait(false);

        var ip = HttpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var changedAt = DateTime.UtcNow.ToString("u");
        var subject = "Password changed";
        var textBody = $"Your password was changed at {changedAt} from IP {ip}. If this wasn't you, contact support immediately.";
        var htmlBody = $"<p>Your password was changed at <strong>{changedAt}</strong> from IP <strong>{ip}</strong>.</p><p>If this wasn't you, contact support immediately.</p>";

        try
        {
            switch (Channel)
            {
                case ChannelEnum.Email:
                    await EmailSenderService.SendAsync("", selectorValue, subject, textBody, htmlBody, cancellationToken).ConfigureAwait(false);
                    break;
                case ChannelEnum.Sms:
                    await SmsSenderService.SendAsync(selectorValue, textBody, cancellationToken).ConfigureAwait(false);
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
