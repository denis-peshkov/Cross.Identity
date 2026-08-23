namespace Cross.Identity.Dtos;

/// <summary>
/// Message to send to the user.
/// </summary>
internal sealed class NotificationMessage
{
    /// <summary>Delivery channel (e.g. email or SMS).</summary>
    public required ChannelEnum Channel { get; init; }

    /// <summary>Destination address (email, E.164 phone, or messenger id).</summary>
    public required string Destination { get; init; }

    /// <summary>Optional display name for the destination (e.g. user full name).</summary>
    public required string? DestinationName { get; init; }

    /// <summary>Email subject or SMS title when applicable.</summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>Plain-text body.</summary>
    public string TextBody { get; private set; } = string.Empty;

    /// <summary>HTML body (email only).</summary>
    public string HtmlBody { get; private set; } = string.Empty;

    /// <summary>Creates a message for the given channel and destination.</summary>
    /// <param name="channel">Delivery channel.</param>
    /// <param name="destination">Normalized destination address.</param>
    /// <param name="destinationName">Optional recipient display name.</param>
    public static NotificationMessage For(ChannelEnum channel, string destination, string? destinationName = null)
        => new NotificationMessage { Channel = channel, Destination = destination, DestinationName = destinationName };

    /// <summary>Sets <see cref="Subject"/> and returns this instance for chaining.</summary>
    /// <param name="subject">Subject line.</param>
    public NotificationMessage WithSubject(string subject)
    {
        Subject = subject;
        return this;
    }

    /// <summary>Sets <see cref="TextBody"/> and returns this instance for chaining.</summary>
    /// <param name="textBody">Plain-text content.</param>
    public NotificationMessage WithTextBody(string textBody)
    {
        TextBody = textBody;
        return this;
    }

    /// <summary>Sets <see cref="HtmlBody"/> and returns this instance for chaining.</summary>
    /// <param name="htmlBody">HTML content.</param>
    public NotificationMessage WithTextHtml(string htmlBody)
    {
        HtmlBody = htmlBody;
        return this;
    }
}
