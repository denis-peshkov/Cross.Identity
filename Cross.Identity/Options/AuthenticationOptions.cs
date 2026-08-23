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
}
