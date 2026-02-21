namespace Cross.Notification.Email.Services;

public interface IEmailSenderService
{
    Task SendAsync(string toName, string toEmail, string subject, string body, CancellationToken cancellationToken);

    Task SendAsync(string toName, string toEmail, string subject, string textBody, string htmlBody, CancellationToken cancellationToken);
}
