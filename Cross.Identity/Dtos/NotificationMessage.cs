namespace Cross.Identity.Dtos;

/// <summary>
/// Message to send to the user.
/// </summary>
internal sealed class NotificationMessage
{
    /// <param name="channel">Delivery channel (e.g. <c>"email"</c> or <c>"phone"</c>).</param>
    public required ChannelEnum Channel { get; init; }
    /// <param name="destination">Destination (e.g. email address or phone number).</param>
    public required string Destination { get; init; }      // destination: email address | phone number
    public required string? DestinationName { get; init; } // destination name: FirstName LastName | FullName
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
