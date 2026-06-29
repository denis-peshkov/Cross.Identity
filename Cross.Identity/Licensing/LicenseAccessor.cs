namespace Cross.Identity.Licensing;

internal sealed class LicenseAccessor
{
    private readonly IdentityServiceConfiguration _serviceConfiguration;
    private readonly ILogger _logger;

    public LicenseAccessor(IdentityServiceConfiguration serviceConfiguration, ILoggerFactory loggerFactory)
    {
        _serviceConfiguration = serviceConfiguration;
        _logger = loggerFactory.CreateLogger("Peshkov.Cross.Identity.License");
    }

    private License? _license;
    private readonly object _lock = new();

    public License Current => _license ??= Initialize();

    private License Initialize()
    {
        lock (_lock)
        {
            if (_license != null)
            {
                return _license;
            }

            var key = _serviceConfiguration.LicenseKey;
            if (key == null)
            {
                return new License();
            }

            var licenseClaims = ValidateKey(key);
            return licenseClaims.Length > 0
                ? new License(new ClaimsPrincipal(new ClaimsIdentity(licenseClaims)))
                : new License();
        }
    }

    private Claim[] ValidateKey(string licenseKey)
    {
        if (!IsValidJwtFormat(licenseKey))
        {
            _logger.LogError(
                "Invalid Peshkov software license key. The token needs to be in JWS or JWE Compact Serialization Format. " +
                "(JWS): 'EncodedHeader.EncodedPayload.EncodedSignature'. " +
                "(JWE): 'EncodedProtectedHeader.EncodedEncryptedKey.EncodedInitializationVector.EncodedCiphertext.EncodedAuthenticationTag'. " +
                "Please visit https://peshkov.biz to obtain a valid license.");
            return Array.Empty<Claim>();
        }

        var handler = new JsonWebTokenHandler();

        var rsa = new RSAParameters
        {
            Exponent = Convert.FromBase64String("AQAB"),
            Modulus = Convert.FromBase64String(
                "wLWWXccoyaqk6RVn1kDNSX6WNJDtuOB2Lpu5Kh1q3ENDzkieia2xDlffpvo14XoI1JJOunY1k11XDg0HfRxVC2FwdcrouCDZKDQp87jvnY2vsxIZVAIYQ5wUetNOD4GVAoLAGYUhc647nyRgasC4ATIxCbH0XKjJZdWwb9BIKK9OCbqcDwHHX3IKK7v0sbiw/OOQQHhUZ7EeiPzZavnu8ZWwA1M4bsk9s/2qc5t+fFC0EWVhuGlV7U3dtwRKJ3/rvqbpo9MHUT4HzsZPMA6+/uNcZhjZLADjKsNrGs7vIDoaizneg1TUyiIiy+0K50C2vs/vbSNiz49JOTcr81RjFw=="),
        };

        var key = new RsaSecurityKey(rsa)
        {
            KeyId = "PeshkovSoftwareLicenseKey/bbb13acb59904d89b4cb1c85f088ccf9",
        };

        var parms = new TokenValidationParameters
        {
            ValidIssuer = "https://peshkov.biz",
            ValidAudience = "Peshkov software",
            IssuerSigningKey = key,
            ValidateLifetime = false,
        };

        var validateResult = handler.ValidateTokenAsync(licenseKey, parms).GetAwaiter().GetResult();
        if (!validateResult.IsValid)
        {
            _logger.LogError(
                validateResult.Exception,
                "Invalid Peshkov software license key. Please visit https://peshkov.biz to obtain a valid license.");
        }

        return validateResult.ClaimsIdentity?.Claims.ToArray() ?? Array.Empty<Claim>();
    }

    /// <summary>
    /// Validates that the token is in JWS (3 parts) or JWE (5 parts) Compact Serialization Format.
    /// </summary>
    private static bool IsValidJwtFormat(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.');
        return parts.Length is 3 or 5;
    }
}
