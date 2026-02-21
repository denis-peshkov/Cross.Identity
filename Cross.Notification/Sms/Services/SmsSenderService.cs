namespace Cross.Notification.Sms.Services;

public class SmsSenderService : ISmsSenderService
{
    public SmsSenderService()
    {

    }

    public Task<string> SendAsync(string destination, string body, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
