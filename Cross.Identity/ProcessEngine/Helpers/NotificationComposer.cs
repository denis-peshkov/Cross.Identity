namespace Cross.Identity.ProcessEngine.Helpers;

/// <inheritdoc />
internal sealed class NotificationComposer : INotificationComposer
{
    private readonly IProcessDefinitionProvider _provider;
    private readonly NotificationOptions _notifications;

    public NotificationComposer(
        IProcessDefinitionProvider provider,
        IOptions<NotificationOptions> notifications)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(notifications);

        _provider = provider;
        _notifications = notifications.Value;
    }

    /// <inheritdoc />
    public ComposedNotificationBodies Compose(
        Bag bag,
        string templateName,
        IReadOnlyDictionary<string, string>? placeholders = null)
    {
        ArgumentNullException.ThrowIfNull(bag);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);

        var language = HostSuppliedLanguageContext.Read(bag);
        var text = Apply(language.ResolveTemplate(_provider, templateName, "txt"), placeholders);
        var html = Apply(language.ResolveTemplate(_provider, templateName, "html"), placeholders);
        return new ComposedNotificationBodies(text, html);
    }

    private string Apply(string template, IReadOnlyDictionary<string, string>? placeholders)
    {
        var s = ApplyBrand(template);
        if (placeholders is null || placeholders.Count == 0)
        {
            return s;
        }

        foreach (var pair in placeholders)
        {
            s = s.Replace(pair.Key, pair.Value);
        }

        return s;
    }

    private string ApplyBrand(string template)
    {
        var year = DateTime.UtcNow.Year.ToString();
        return template
            .Replace("{{company}}", _notifications.Company)
            .Replace("{{site}}", _notifications.Site)
            .Replace("{{brand}}", _notifications.Brand)
            .Replace("{{fullName}}", _notifications.FullName)
            .Replace("{{supportEmail}}", _notifications.SupportEmail)
            .Replace("{{year}}", year)
            .Replace("{{logoWidth}}", "34")
            .Replace("{{logoHeight}}", "34")
            .Replace("{{imageWidth}}", "34")
            .Replace("{{imageHeight}}", "34");
    }
}
