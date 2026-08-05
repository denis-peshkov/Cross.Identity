namespace Cross.Identity.Enums;

public enum RefreshTokenRevokeReason : short
{
    #region 1. Security reasons (critical)

    /// <summary>
    /// Presenting an old token after rotation → attack attempt → invalidate the entire family.
    /// </summary>
    REPLAY_DETECTED,

    /// <summary>
    /// Use from another device / IP combination detected → theft indicator.
    /// </summary>
    /// <remarks>
    /// Typically used with analytics:
    /// • too many attempts from different IPs
    /// • fingerprint mismatch
    /// • suspicious geo-location
    /// </remarks>
    TOKEN_STOLEN,

    /// <summary>
    /// Device hash (DeviceFingerprint) changed → token stolen or forged.
    /// </summary>
    DEVICE_MISMATCH,

    IP_MISMATCH,

    /// <summary>
    /// Some systems strictly enforce region for region-lock.
    /// </summary>
    LOCATION_MISMATCH,

    /// <summary>
    /// User-Agent differs significantly → possible thief.
    /// </summary>
    USER_AGENT_MISMATCH,

    #endregion

    #region 2. Business-security reasons (user behavior). These reasons relate to operating conditions or restrictions.

    /// <summary>
    /// User changed password → ALL refresh tokens are revoked.
    /// </summary>
    PASSWORD_CHANGED,

    /// <summary>
    /// User changed/unlinked MFA → all tokens become invalid.
    /// </summary>
    MFA_RESET,

    /// <summary>
    /// Anomaly: many logins, many errors, unusual activity.
    /// </summary>
    SUSPICIOUS_ACTIVITY,

    /// <summary>
    /// Session was valid for X days → automatically revoke FamilyId. E.g. max 30 days regardless of activity.
    /// </summary>
    SESSION_EXPIRED,

    #endregion

    #region 3. User-initiated (user action)

    /// <summary>
    /// User clicked Logout → token/family revoked.
    /// </summary>
    USER_LOGOUT,

    /// <summary>
    /// User clicked "Logout from all devices".
    /// </summary>
    USER_LOGOUT_ALL,

    /// <summary>
    /// User detached a device in "My devices".
    /// </summary>
    DEVICE_REMOVED_BY_USER,

    /// <summary>
    /// User unlinked an external login provider → sessions revoked via SecurityStamp rotation.
    /// </summary>
    EXTERNAL_LOGIN_REMOVED,

    #endregion

    #region 4. Admin / backend-initiated reasons

    /// <summary>
    /// Administrator manually disabled user / device / tokens.
    /// </summary>
    ADMIN_REVOKE,

    /// <summary>
    /// Account locked — revoke all tokens.
    /// </summary>
    ACCOUNT_DISABLED,

    /// <summary>
    /// Account deleted.
    /// </summary>
    ACCOUNT_DELETED,

    #endregion

    #region 5. Technical reasons

    /// <summary>
    /// Security detector considers the token compromised (AI/ML, anti-fraud).
    /// </summary>
    TOKEN_COMPROMISED,

    /// <summary>
    /// Token tampered, invalid signature, expired, wrong audience.
    /// </summary>
    TOKEN_FORMAT_INVALID,

    /// <summary>
    /// Token scheme / algorithm / version changed → old tokens invalid.
    /// </summary>
    /// <remarks>
    /// For example:
    /// • migration from HS256 → RS256
    /// • pepper rotation
    /// • payload structure change
    /// </remarks>
    TOKEN_UPGRADE_REQUIRED,

    /// <summary>
    /// Forcing rotation (e.g. via a DB flag) — sometimes used during migrations.
    /// </summary>
    ROTATION_REQUIRED,

    #endregion
}
