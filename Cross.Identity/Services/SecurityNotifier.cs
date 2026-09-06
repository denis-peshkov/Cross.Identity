namespace Cross.Identity.Services;

/// <inheritdoc />
internal sealed class SecurityNotifier : ISecurityNotifier
{
    private readonly IEmailSenderService _emailSenderService;
    private readonly ISmsSenderService _smsSenderService;

    public SecurityNotifier(
        IEmailSenderService emailSenderService,
        ISmsSenderService smsSenderService)
    {
        ArgumentNullException.ThrowIfNull(emailSenderService);
        ArgumentNullException.ThrowIfNull(smsSenderService);

        _emailSenderService = emailSenderService;
        _smsSenderService = smsSenderService;
    }

    /// <inheritdoc />
    public async Task SendAsync(
        ChannelEnum channel,
        string address,
        string subject,
        string textBody,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentNullException.ThrowIfNull(textBody);

        switch (channel)
        {
            case ChannelEnum.Email:
                await _emailSenderService
                    .SendAsync("", address, subject ?? string.Empty, textBody, htmlBody ?? string.Empty, cancellationToken)
                    .ConfigureAwait(false);
                break;
            case ChannelEnum.Sms:
                await _smsSenderService
                    .SendAsync(address, textBody, cancellationToken)
                    .ConfigureAwait(false);
                break;
            default:
                throw new ValidationException($"Channel '{channel}' is not supported for security notifications.");
        }
    }
}
