namespace Cross.Identity.Dtos;

/// <summary>
/// Сообщение для отправки пользователю.
/// </summary>
internal sealed class NotificationMessage
{
    public required ChannelEnum Channel { get; init; }
    public required string Destination { get; init; }      // адрес назначения: email address | phone number
    public required string? DestinationName { get; init; } // имя назначения: FirstName LastName | FullName
    public string Subject { get; private set; } = string.Empty;
    public string TextBody { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;

    public static NotificationMessage For(ChannelEnum channel, string destination, string? destinationName = null)
        => new NotificationMessage { Channel = channel, Destination = destination, DestinationName = destinationName};

    public NotificationMessage WithSubject(string subject)
    {
        Subject = subject;
        return this;
    }

    public NotificationMessage WithTextBody(string textBody)
    {
        TextBody = textBody;
        return this;
    }

    public NotificationMessage WithTextHtml(string htmlBody)
    {
        HtmlBody = htmlBody;
        return this;
    }
}
