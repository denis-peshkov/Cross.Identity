namespace Cross.Identity.Enums;

/// <summary>Where a communication endpoint address came from.</summary>
public enum CommunicationEndpointSource : short
{
    /// <summary>Account email/phone on <c>UsersAccounts</c>.</summary>
    Account = 0,

    /// <summary>Email from a linked OAuth provider (<c>UsersExternalLogins.ProviderEmail</c>).</summary>
    ExternalProvider = 1,

    /// <summary>Messenger link (Telegram / Viber / WhatsApp chat id).</summary>
    LinkedMessenger = 2,

    /// <summary>Explicitly added by the user or host.</summary>
    Manual = 3,
}
