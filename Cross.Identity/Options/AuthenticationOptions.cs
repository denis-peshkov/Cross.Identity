namespace Cross.Identity.Options;

public sealed class AuthenticationOptions
{
    /// <summary>
    /// Configuration section name for binding email settings.
    /// </summary>
    public const string SectionName = "Authentication";

    public JwtOptions Jwt { get; set; }

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
}
