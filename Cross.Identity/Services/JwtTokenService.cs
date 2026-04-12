namespace Cross.Identity.Services;

internal class JwtTokenService : IJwtTokenService
{
    private readonly IdentityContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly SymmetricSecurityKey _encryptionKey;
    private readonly AuthenticationOptions _options;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenService(
        IdentityContext context,
        IOptionsSnapshot<AuthenticationOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;

        // A256KW (алгоритм обёртки ключа) требует ровно 32 байта (256 бит) wrap-ключа.
        // A256CBC-HS512 — это алгоритм контент-шифрования; CEK (контент-ключ) генерируется внутри и оборачивается твоим wrap-ключом.
        // Важно только чтобы wrap-ключ соответствовал A256KW (32 байта).
        // Нужно брать Base64:
        var encKeyBytes = Convert.FromBase64String(_options.Jwt.EncryptionKey);
        if (encKeyBytes.Length != 32) // 32 bytes = 256-bit for A256KW
            throw new InvalidOperationException("Jwt.EncryptionKey must be 32 bytes (Base64) for A256KW.");
        _encryptionKey = new SymmetricSecurityKey(encKeyBytes);

        // Подпись HMAC тоже лучше делать из Base64:
        var signKeyBytes = Convert.FromBase64String(_options.Jwt.Key);
        if (signKeyBytes.Length < 32) // минимум 256 бит на HMAC-SHA256
            throw new InvalidOperationException("Jwt.Key should be at least 32 bytes (Base64) for HMAC-SHA256.");
        _signingKey = new SymmetricSecurityKey(signKeyBytes);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateIdTokenAsync(List<Claim> claims)
    {
        var jti = Guid.NewGuid();

        var claimsIdentity = claims
            .AddIfNotNull(JwtRegisteredClaimNames.Jti, jti.ToString())
            .AddIfNotNull(JwtRegisteredClaimNames.Typ, "id_token");

        var createdAt = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claimsIdentity),
            Issuer = _options.Jwt.Issuer,
            Audience = _options.Jwt.Audience,
            IssuedAt = createdAt,
            NotBefore = createdAt.AddSeconds(-1),
            Expires = createdAt.AddMinutes(5),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        var token = _handler.CreateToken(descriptor);
        var tokenString = _handler.WriteToken(token);

        return tokenString;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAccessTokenAsync(Guid userId, Guid familyId, List<string> permissions, List<Claim> claims)
    {
        var jti = Guid.NewGuid();

        var claimsIdentity = claims
            .AddIfNotNull(JwtRegisteredClaimNames.Jti, jti.ToString())
            .AddIfNotNull(JwtRegisteredClaimNames.Typ, "access_token");

        claimsIdentity.AddRange(permissions.Select(p => new Claim(ClaimConstants.Permission, p)));

        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt.Add(_options.Jwt.AccessTokenExpires);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claimsIdentity),
            Issuer = _options.Jwt.Issuer,
            Audience = _options.Jwt.Audience,
            IssuedAt = createdAt,
            NotBefore = createdAt.AddSeconds(-1),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
            Claims = null,
        };

        if (_options.Jwt.UseEncryption)
        {
            descriptor.EncryptingCredentials = new EncryptingCredentials(
                _encryptionKey,
                SecurityAlgorithms.Aes256KW,
                SecurityAlgorithms.Aes256CbcHmacSha512);
        }

        var token = _handler.CreateToken(descriptor);
        var tokenString = _handler.WriteToken(token);
        var tokenHash =  Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(tokenString)));

        var entity = new AccessTokenEntity
        {
            Id = jti,
            FamilyId = familyId,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            DeviceFingerprint = null,
            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        };

        // await _context.AccessTokens.Where(x => x.IsRevoked).DeleteFromQueryAsync();
        // await _context.AccessTokens.Where(x => x.UserId == userId && x.ExpiresAt < DateTime.UtcNow).DeleteFromQueryAsync();

        // Сохранить jti в таблицу access-токенов (для blacklist, аудит, и отзывов)
        _context.AccessTokens.Add(entity);

        await _context.SaveChangesAsync();

        return tokenString;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateRefreshTokenAsync(Guid userId, Guid familyId, List<Claim> claims)
    {
        var jti = Guid.NewGuid();

        var claimsIdentity = claims
            .AddIfNotNull(JwtRegisteredClaimNames.Jti, jti.ToString())
            .AddIfNotNull(JwtRegisteredClaimNames.Typ, "refresh_token");

        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt.Add(_options.Jwt.RefreshTokenExpires);
        var absoluteExpiresAt = createdAt.Add(_options.Jwt.RefreshTokenAbsoluteExpires);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claimsIdentity),
            Issuer = _options.Jwt.Issuer,
            Audience = _options.Jwt.Audience,
            IssuedAt = createdAt,
            NotBefore = createdAt.AddSeconds(-1),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
            Claims = null,
        };

        var token = _handler.CreateToken(descriptor);
        var tokenString = _handler.WriteToken(token);
        var tokenHash =  Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(tokenString)));

        // await _context.RefreshTokens.Where(x => x.IsRevoked).DeleteFromQueryAsync();
        // await _context.RefreshTokens.Where(x => x.UserId == userId && x.ExpiresAt < DateTime.UtcNow).DeleteFromQueryAsync();
        _context.RefreshTokens.Add(
            new RefreshTokenEntity
            {
                Id = jti,
                FamilyId = familyId,
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                AbsoluteExpiresAt = absoluteExpiresAt,
                CreatedAt = createdAt,
                DeviceFingerprint = null,
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
                IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            });

        await _context.SaveChangesAsync();

        return tokenString;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateAccessTokenAsync(string accessToken)
    {
        var jwt = _handler.ReadJwtToken(accessToken);
        var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

        if (!Guid.TryParse(jti, out var jtiGuid))
        {
            return false; // невалидный токен
        }

        var entity = await _context.AccessTokens.FindAsync(jtiGuid);

        return entity is { RevokedAt: null }
               && entity.ExpiresAt >= DateTime.UtcNow
               && entity.CreatedAt <= DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateAccessTokenJtiAsync(Guid jti, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AccessTokens
            .FirstOrDefaultAsync(x => x.Id == jti, cancellationToken);

        return entity is { RevokedAt: null }
               && entity.ExpiresAt >= DateTime.UtcNow
               && entity.CreatedAt <= DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
    {
        var tokenHash =  Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        var entity = await _context.RefreshTokens
            .Where(x => x.TokenHash == tokenHash)
            .FirstOrDefaultAsync();

        return entity is { RevokedAt: null }
               && entity.ExpiresAt >= DateTime.UtcNow
               && entity.AbsoluteExpiresAt >= DateTime.UtcNow
               && entity.CreatedAt <= DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public async Task RevokeAccessTokenAsync(Guid jti)
    {
        var entry = await _context.AccessTokens.FindAsync(jti);
        if (entry != null)
        {
            entry.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task CleanupExpiredAccessTokensAsync()
    {
        var expired = await _context.AccessTokens
            .Where(x => x.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expired.Any())
        {
            _context.AccessTokens.RemoveRange(expired);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public Task<string?> GetClaimValueAsync(string token, params string[] claimTypes)
    {
        ArgumentNullException.ThrowIfNull(token);

        static string DecodeJwtPayload(string jwt)
        {
            var p = jwt.Split('.');
            if (p.Length < 2) throw new ArgumentException("Not a JWT token.");
            var payload = p[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
            return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        }

        var t = DecodeJwtPayload(token);
        using var json = JsonDocument.Parse(t);
        var root = json.RootElement;

        string? result = null;
        foreach (var claimType in claimTypes)
        {
            if (root.TryGetProperty(claimType, out var res))
            {
                result = res.GetString();
            }
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public async Task<RefreshTokenEntity?> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash =  Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        var entity = await _context.RefreshTokens
            .AsNoTracking()
            .Where(x => x.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);

        return entity;
    }

    /// <inheritdoc/>
    public int AccessTokenExpiresInSeconds => (int)_options.Jwt.AccessTokenExpires.TotalSeconds;

    /// <inheritdoc/>
    public async Task InvalidateRefreshTokenAsync(string refreshToken, string newJti, CancellationToken cancellationToken)
    {
        var tokenHash =  Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        var entity = await _context.RefreshTokens
            .Where(x => x.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken)
                     ?? throw new InvalidOperationException("Refresh token not found.");

        var jti = Guid.Parse(newJti);

        entity.ReplacedByTokenId = jti;
        entity.RevokedAt = DateTime.UtcNow;
        entity.RevokeReason = RefreshTokenRevokeReason.ROTATION_REQUIRED;
        entity.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RevokeRefreshTokenForLogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        var entity = await _context.RefreshTokens
            .Where(x => x.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null || entity.RevokedAt is not null)
        {
            return;
        }

        entity.RevokedAt = DateTime.UtcNow;
        entity.RevokeReason = RefreshTokenRevokeReason.USER_LOGOUT;
        entity.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
