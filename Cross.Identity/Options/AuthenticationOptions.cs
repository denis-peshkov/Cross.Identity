namespace Cross.Identity.Options;

public sealed class AuthenticationOptions
{
    /// <summary>
    /// Configuration section name for binding email settings.
    /// </summary>
    public const string SectionName = "Authentication";

    public JwtOptions Jwt { get; set; }

    /// <summary>Опции выпуска JWT.</summary>
    public sealed class JwtOptions
    {
        /// <summary>Issuer (iss) — издатель токена.</summary>
        public string Issuer { get; set; }

        /// <summary>Audience (aud) — потребитель токена.</summary>
        public string Audience { get; set; }

        /// <summary>Секретный ключ для HMAC-подписания (минимум 32 символа).</summary>
        public string Key { get; set; }

        public bool UseEncryption { get; set; }

        public string EncryptionKey { get; set; }

        public TimeSpan AccessTokenExpires { get; set; }

        public TimeSpan RefreshTokenExpires { get; set; }

        public TimeSpan RefreshTokenAbsoluteExpires { get; set; }
    }
}
