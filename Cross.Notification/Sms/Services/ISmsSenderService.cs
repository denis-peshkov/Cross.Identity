namespace Cross.Notification.Sms.Services;

public interface ISmsSenderService
{
    Task<string> SendAsync(string destination, string body, CancellationToken cancellationToken);
}
