namespace Cross.Notification.Email.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection AddEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(nameof(EmailOptions)));
        services.TryAddScoped<IEmailSenderService, EmailSenderService>();

        return services;
    }
}
