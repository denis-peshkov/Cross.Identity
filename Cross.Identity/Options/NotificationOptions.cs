namespace Cross.Identity.Options;

/// <summary>
/// Branding placeholders for OTP and security-notification templates
/// (<c>Authentication:Notifications</c>).
/// Used by <see cref="ProcessEngine.Helpers.NotificationComposer"/>.
/// </summary>
public sealed class NotificationOptions
{
    /// <summary>Configuration section path: <c>Authentication:Notifications</c>.</summary>
    public const string SectionName = "Authentication:Notifications";

    /// <summary><c>{{brand}}</c> in templates.</summary>
    public string Brand { get; set; }

    /// <summary><c>{{company}}</c> in templates.</summary>
    public string Company { get; set; }

    /// <summary><c>{{site}}</c> in templates.</summary>
    public string Site { get; set; }

    /// <summary><c>{{fullName}}</c> (signature) in templates.</summary>
    public string FullName { get; set; }

    /// <summary><c>{{supportEmail}}</c> in templates.</summary>
    public string SupportEmail { get; set; }
}
