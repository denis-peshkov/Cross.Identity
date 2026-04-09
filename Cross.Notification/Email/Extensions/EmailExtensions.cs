namespace Cross.Notification.Email.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection AddEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NotificationEmailOptions>(configuration.GetSection(NotificationEmailOptions.SectionName));
        services.TryAddScoped<IEmailSenderService, EmailSenderService>();

        return services;
    }
}
