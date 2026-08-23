namespace Cross.Identity.Enums;

/// <summary>Audited identity / security operations.</summary>
public enum AuditOperation : short
{
    /// <summary>New user account created.</summary>
    UserCreated = 1,

    /// <summary>Password set or changed by the authenticated user.</summary>
    PasswordChanged = 2,

    /// <summary>Password reset via recovery flow.</summary>
    PasswordReset = 3,

    /// <summary>Successful authentication (password / code / external).</summary>
    LoginSucceeded = 4,

    /// <summary>Failed authentication attempt.</summary>
    LoginFailed = 5,

    /// <summary>One-time code sent.</summary>
    CodeSent = 6,

    /// <summary>One-time code verified.</summary>
    CodeVerified = 7,

    /// <summary>Access / refresh token pair issued.</summary>
    TokenIssued = 8,

    /// <summary>Refresh token rotated.</summary>
    TokenRefreshed = 9,

    /// <summary>Access and/or refresh token revoked.</summary>
    TokenRevoked = 10,

    /// <summary>User logged out (current session).</summary>
    Logout = 11,

    /// <summary>User logged out from all devices.</summary>
    LogoutAll = 12,

    /// <summary>External OAuth login linked.</summary>
    ExternalLoginLinked = 13,

    /// <summary>External OAuth login unlinked.</summary>
    ExternalLoginUnlinked = 14,

    /// <summary>Communication endpoint added or updated.</summary>
    CommunicationEndpointChanged = 15,

    /// <summary>Catch-all for operations not covered by a dedicated value.</summary>
    Other = 99,
}
