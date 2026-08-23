namespace Cross.Identity.Options;

public sealed class AuthenticationOptions
{
    /// <summary>
    /// Configuration section name for binding email settings.
    /// </summary>
    public const string SectionName = "Authentication";

    public JwtOptions Jwt { get; set; }

    /// <summary>Failed password attempt lockout (see <see cref="UserAccountEntity.LockoutEnd"/>).</summary>
    public LockoutOptions Lockout { get; set; } = new();

    /// <summary>
    /// When <c>true</c>, OTP and password-change notifications always use Email
    /// (ignores preferred phone/messenger endpoints). Requires an email on the account
    /// or a verified email endpoint.
    /// </summary>
    public bool LockChannelAsEmail { get; set; }

    /// <summary>Limits how often OTP codes may be sent (see <c>CodeService.SendAsync</c>).</summary>
    public OtpSendRateLimitOptions OtpSendRateLimit { get; set; } = new();

    /// <summary>Background cleanup interval for expired refresh tokens.</summary>
    public TimeSpan TokenCleanupInterval { get; set; } = TimeSpan.FromHours(1);


    /// <summary>JWT issuance options.</summary>
    public sealed class JwtOptions
    {
        /// <summary>Issuer (iss) — token issuer.</summary>
        public string Issuer { get; set; }

        /// <summary>Audience (aud) — token audience.</summary>
        public string Audience { get; set; }

        /// <summary>Secret key for HMAC signing (minimum 32 characters).</summary>
        public string Key { get; set; }

        public bool UseEncryption { get; set; }

        public string EncryptionKey { get; set; }

        public TimeSpan AccessTokenExpires { get; set; }

        public TimeSpan RefreshTokenExpires { get; set; }

        public TimeSpan RefreshTokenAbsoluteExpires { get; set; }

        /// <summary>
        /// Maximum idle time since <c>LastActivityAt</c> before refresh is rejected with
        /// <see cref="RefreshTokenRevokedReason.SESSION_EXPIRED"/>. <c>Zero</c> disables the check.
        /// </summary>
        public TimeSpan RefreshTokenIdleTimeout { get; set; }

        /// <summary>
        /// When <c>true</c>, refresh compares the family anchor IP with the current request IP;
        /// mismatch revokes with <see cref="RefreshTokenRevokedReason.IP_MISMATCH"/>.
        /// Default <c>false</c> (opt-in; avoids false positives on NAT/mobile).
        /// Device fingerprint and User-Agent are always checked when captured at family start.
        /// When <c>true</c> and session binding was captured at login, the host must pass
        /// <see cref="HostSuppliedClientContext"/> on refresh (same trusted pipeline as Token) — not
        /// <see cref="HostSuppliedClientContext.Empty"/> — or refresh fails with <see cref="ValidationException"/>.
        /// </summary>
        public bool SessionBindingCheckIp { get; set; }
    }

    /// <summary>Lockout policy for password-based authentication.</summary>
    public sealed class LockoutOptions
    {
        /// <summary>Initial <see cref="UserAccountEntity.LockoutEnabled"/> when a user is created.</summary>
        public bool LockoutEnabled { get; set; } = true;

        /// <summary>Failed attempts before lockout. <c>0</c> disables counting.</summary>
        public int MaxFailedAccessAttempts { get; set; } = 5;

        /// <summary>How long the account stays locked after the threshold is reached.</summary>
        public TimeSpan LockoutTimeout { get; set; } = TimeSpan.FromMinutes(15);
    }

    /// <summary>Rate limits for OTP send (<c>SendCode</c> / <c>ICodeService.SendAsync</c>).</summary>
    public sealed class OtpSendRateLimitOptions
    {
        /// <summary>
        /// Minimum interval between sends for the same user + destination.
        /// <see cref="TimeSpan.Zero"/> disables the cooldown.
        /// </summary>
        public TimeSpan Cooldown { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Maximum number of sends for the same user + destination within <see cref="Window"/>.
        /// <c>0</c> disables the window cap.
        /// </summary>
        public int MaxSendsPerWindow { get; set; } = 5;

        /// <summary>Window for <see cref="MaxSendsPerWindow"/> (ignored when max is <c>0</c>).</summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromHours(1);
    }
}
