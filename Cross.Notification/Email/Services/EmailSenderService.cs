namespace Cross.Notification.Email.Services;

public class EmailSenderService : IEmailSenderService
{
    private readonly ILogger<EmailSenderService> _logger;
    private readonly NotificationEmailOptions _options;

    public EmailSenderService(
        ILogger<EmailSenderService> logger,
        IOptionsSnapshot<NotificationEmailOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task SendAsync(string toName, string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Recipient email is required.", nameof(toEmail));
        }

        var fromAddress = new MailAddress(_options.FromUserAddress, _options.FromUserName);
        var toAddress = string.IsNullOrEmpty(toName)
            ? new MailAddress(toEmail)
            : new MailAddress(toEmail, toName);

        using var smtp = new SmtpClient
        {
            Host = _options.SmtpHost,
            Port = _options.SmtpPort,
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(_options.SmtpLogin, _options.SmtpPassword),
            UseDefaultCredentials = false,
        };

        using var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            // ReplyToList = { new MailAddress() }
        };

        try
        {
            await smtp.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent to {Recipient}", toEmail);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error when sending email to {Recipient}", toEmail);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when sending email to {Recipient}", toEmail);
            throw;
        }
    }

    public async Task SendAsync(string toName, string toEmail, string subject, string textBody, string htmlBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Recipient email is required.", nameof(toEmail));
        }

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromUserName, _options.FromUserAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        // message.ReplyTo.Add(new MailboxAddress(_options.FromUserName, _options.FromUserAddress));

        var builder = new BodyBuilder
        {
            TextBody = textBody,
            HtmlBody = htmlBody,
        };

        // Важно: почтовые клиенты сами выберут HTML или Text.
        message.Body = builder.ToMessageBody();

        using var smtp = new MailKit.Net.Smtp.SmtpClient();
        await smtp.ConnectAsync(_options.SmtpHost, _options.SmtpPort, _options.SecureSocket, cancellationToken);
        await smtp.AuthenticateAsync(_options.SmtpLogin, _options.SmtpPassword, cancellationToken);

        try
        {
            await smtp.SendAsync(message, cancellationToken);
            _logger.LogInformation("Email sent to {Recipient}", toEmail);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error when sending email to {Recipient}", toEmail);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when sending email to {Recipient}", toEmail);
            throw;
        }
        finally
        {
            await smtp.DisconnectAsync(true, cancellationToken);
        }
    }
}
