namespace Cross.Identity.Enums;

public enum RefreshTokenRevokeReason : short
{
    #region 1. Security reasons (critical)

    /// <summary>
    /// Reuse of an already rotated (revoked) refresh token → revoke the entire <c>FamilyId</c> chain.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rotation alone only rejects the reused token. Family revoke closes the theft race where
    /// the attacker rotated first and still holds the newer active token:
    /// </para>
    /// <list type="number">
    ///   <item><description>Attacker steals <c>R1</c> and refreshes first → gets active <c>R2</c>, <c>R1</c> revoked.</description></item>
    ///   <item><description>Victim presents their copy of <c>R1</c> → reuse of a revoked refresh.</description></item>
    ///   <item><description>Without family revoke: victim gets conflict / must re-login; attacker keeps live <c>R2</c>.</description></item>
    ///   <item><description>With family revoke (<see cref="REPLAY_DETECTED"/>): <c>R2</c> (and access tokens in the family) are revoked too.</description></item>
    /// </list>
    /// <para>
    /// Trade-off: a legitimate retry / concurrent double-refresh can look identical and kill a honest session.
    /// That risk is accepted deliberately for the theft race above.
    /// </para>
    /// </remarks>
    REPLAY_DETECTED = 101,

    /// <summary>
    /// Use from another device / IP combination detected → theft indicator.
    /// </summary>
    /// <remarks>
    /// Typically used with analytics:
    /// • too many attempts from different IPs
    /// • fingerprint mismatch
    /// • suspicious geo-location
    /// </remarks>
    TOKEN_STOLEN = 102,

    /// <summary>
    /// Device hash (DeviceFingerprint) changed → token stolen or forged.
    /// </summary>
    DEVICE_MISMATCH = 103,

    IP_MISMATCH = 104,

    /// <summary>
    /// Some systems strictly enforce region for region-lock.
    /// </summary>
    LOCATION_MISMATCH = 105,

    /// <summary>
    /// User-Agent differs significantly → possible thief.
    /// </summary>
    USER_AGENT_MISMATCH = 106,

    #endregion

    #region 2. Business-security reasons (user behavior). These reasons relate to operating conditions or restrictions.

    /// <summary>
    /// User changed password → ALL refresh tokens are revoked.
    /// </summary>
    PASSWORD_CHANGED = 201,

    /// <summary>
    /// User changed/unlinked MFA → all tokens become invalid.
    /// </summary>
    MFA_RESET = 202,

    /// <summary>
    /// Anomaly: many logins, many errors, unusual activity.
    /// </summary>
    SUSPICIOUS_ACTIVITY = 203,

    /// <summary>
    /// Session was valid for X days → automatically revoke FamilyId. E.g. max 30 days regardless of activity.
    /// </summary>
    SESSION_EXPIRED = 204,

    #endregion

    #region 3. User-initiated (user action)

    /// <summary>
    /// User clicked Logout → token/family revoked.
    /// </summary>
    USER_LOGOUT = 301,

    /// <summary>
    /// User clicked "Logout from all devices".
    /// </summary>
    USER_LOGOUT_ALL = 302,

    /// <summary>
    /// User detached a device in "My devices".
    /// </summary>
    DEVICE_REMOVED_BY_USER = 303,

    /// <summary>
    /// User unlinked an external login provider → sessions revoked via SecurityStamp rotation.
    /// </summary>
    EXTERNAL_LOGIN_REMOVED = 304,

    #endregion

    #region 4. Admin / backend-initiated reasons

    /// <summary>
    /// Administrator manually disabled user / device / tokens.
    /// </summary>
    ADMIN_REVOKE = 401,

    /// <summary>
    /// Account locked — revoke all tokens.
    /// </summary>
    ACCOUNT_DISABLED = 402,

    /// <summary>
    /// Account deleted.
    /// </summary>
    ACCOUNT_DELETED = 403,

    #endregion

    #region 5. Technical reasons

    /// <summary>
    /// Security detector considers the token compromised (AI/ML, anti-fraud).
    /// </summary>
    TOKEN_COMPROMISED = 501,

    /// <summary>
    /// Token tampered, invalid signature, expired, wrong audience.
    /// </summary>
    TOKEN_FORMAT_INVALID = 502,

    /// <summary>
    /// Token scheme / algorithm / version changed → old tokens invalid.
    /// </summary>
    /// <remarks>
    /// For example:
    /// • migration from HS256 → RS256
    /// • pepper rotation
    /// • payload structure change
    /// </remarks>
    TOKEN_UPGRADE_REQUIRED = 503,

    /// <summary>
    /// Forcing rotation (e.g. via a DB flag) — sometimes used during migrations.
    /// </summary>
    ROTATION_REQUIRED = 504,

    #endregion
}
