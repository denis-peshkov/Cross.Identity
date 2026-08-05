namespace Cross.Identity.Services;

internal class JwtTokenService : IJwtTokenService
{
    private readonly IdentityContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly SymmetricSecurityKey _encryptionKey;
    private readonly AuthenticationOptions _options;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenService(
        IdentityContext context,
        IOptionsSnapshot<AuthenticationOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;

        // A256KW (key-wrap algorithm) requires exactly 32 bytes (256 bits) for the wrap key.
        // A256CBC-HS512 is the content-encryption algorithm; the CEK (content key) is generated internally and wrapped with your wrap key.
        // Only the wrap key must match A256KW (32 bytes).
        // Must use Base64:
        var encKeyBytes = Convert.FromBase64String(_options.Jwt.EncryptionKey);
        if (encKeyBytes.Length != 32) // 32 bytes = 256-bit for A256KW
            throw new InvalidOperationException("Jwt.EncryptionKey must be 32 bytes (Base64) for A256KW.");
        _encryptionKey = new SymmetricSecurityKey(encKeyBytes);

        // HMAC signing should also use Base64:
        var signKeyBytes = Convert.FromBase64String(_options.Jwt.Key);
        if (signKeyBytes.Length < 32) // minimum 256 bits for HMAC-SHA256
            throw new InvalidOperationException("Jwt.Key should be at least 32 bytes (Base64) for HMAC-SHA256.");
        _signingKey = new SymmetricSecurityKey(signKeyBytes);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateIdTokenAsync(List<Claim> claims)
    {
        var jti = Guid.NewGuid();

        var claimsIdentity = claims
            .AddIfNotNull(JwtRegisteredClaimNames.Jti, jti.ToString())
            .AddIfNotNull(JwtRegisteredClaimNames.Typ, IdentityConstants.IdToken);

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

        var tokenString = _handler.CreateToken(descriptor);

        return tokenString;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAccessTokenAsync(Guid userId, Guid familyId, List<string> permissions, List<Claim> claims)
    {
        var jti = Guid.NewGuid();

        var claimsIdentity = claims
            .AddIfNotNull(JwtRegisteredClaimNames.Jti, jti.ToString())
            .AddIfNotNull(JwtRegisteredClaimNames.Typ, IdentityConstants.AccessToken);

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

        var tokenString = _handler.CreateToken(descriptor);
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

        // Persist jti in the access-tokens table (blacklist, audit, and revocation)
        await _context.AccessTokens.AddAsync(entity).ConfigureAwait(false);

        await _context.SaveChangesAsync().ConfigureAwait(false);

        return tokenString;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateRefreshTokenAsync(Guid userId, Guid familyId, List<Claim> claims)
    {
        var jti = Guid.NewGuid();

        var claimsIdentity = claims
            .AddIfNotNull(JwtRegisteredClaimNames.Jti, jti.ToString())
            .AddIfNotNull(JwtRegisteredClaimNames.Typ, IdentityConstants.RefreshToken);

        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt.Add(_options.Jwt.RefreshTokenExpires);
        var absoluteExpiresAt = await ResolveRefreshTokenAbsoluteExpiresAtAsync(familyId, createdAt).ConfigureAwait(false);

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

        var tokenString = _handler.CreateToken(descriptor);
        var tokenHash =  Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(tokenString)));

        // await _context.RefreshTokens.Where(x => x.IsRevoked).DeleteFromQueryAsync();
        // await _context.RefreshTokens.Where(x => x.UserId == userId && x.ExpiresAt < DateTime.UtcNow).DeleteFromQueryAsync();
        await _context.RefreshTokens.AddAsync(
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
            })
            .ConfigureAwait(false);

        await _context.SaveChangesAsync().ConfigureAwait(false);

        return tokenString;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateAccessTokenAsync(string accessToken)
    {
        var jwt = _handler.ReadJsonWebToken(accessToken);
        var jti = jwt.GetClaim(JwtRegisteredClaimNames.Jti)?.Value;

        if (!Guid.TryParse(jti, out var jtiGuid))
        {
            return false; // invalid token
        }

        var entity = await _context.AccessTokens.FindAsync(jtiGuid).ConfigureAwait(false);

        return entity is { RevokedAt: null }
               && entity.ExpiresAt >= DateTime.UtcNow
               && entity.CreatedAt <= DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateAccessTokenJtiAsync(Guid jti, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AccessTokens
            .FirstOrDefaultAsync(x => x.Id == jti, cancellationToken).ConfigureAwait(false);

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
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        return entity is { RevokedAt: null }
               && entity.ExpiresAt >= DateTime.UtcNow
               && entity.AbsoluteExpiresAt >= DateTime.UtcNow
               && entity.CreatedAt <= DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public async Task RevokeAccessTokenAsync(Guid jti)
    {
        var entry = await _context.AccessTokens.FindAsync(jti).ConfigureAwait(false);
        if (entry != null)
        {
            entry.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task CleanupExpiredAccessTokensAsync()
    {
        var expired = await _context.AccessTokens
            .Where(x => x.ExpiresAt < DateTime.UtcNow)
            .ToListAsync()
            .ConfigureAwait(false);

        if (expired.Any())
        {
            _context.AccessTokens.RemoveRange(expired);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task CleanupExpiredRefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredQuery = _context.RefreshTokens.Where(x => x.AbsoluteExpiresAt < now);

        if (_context.Database.IsInMemory())
        {
            var expired = await expiredQuery.ToArrayAsync(cancellationToken).ConfigureAwait(false);
            _context.RefreshTokens.RemoveRange(expired);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await expiredQuery.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
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
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return entity;
    }

    /// <inheritdoc/>
    public int AccessTokenExpiresInSeconds => (int)_options.Jwt.AccessTokenExpires.TotalSeconds;

    private async Task<DateTime> ResolveRefreshTokenAbsoluteExpiresAtAsync(Guid familyId, DateTime createdAt)
    {
        var chainAbsolute = await _context.RefreshTokens
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => (DateTime?)x.AbsoluteExpiresAt)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        return chainAbsolute ?? createdAt.Add(_options.Jwt.RefreshTokenAbsoluteExpires);
    }

    /// <inheritdoc/>
    public async Task InvalidateRefreshTokenAsync(string refreshToken, string newJti, CancellationToken cancellationToken)
    {
        var tokenHash =  Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        var jti = Guid.Parse(newJti);
        var revokedAt = DateTime.UtcNow;
        var revokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        if (_context.Database.IsInMemory())
        {
            var entity = await _context.RefreshTokens
                .Where(x => x.TokenHash == tokenHash)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false)
                         ?? throw new InvalidOperationException("Refresh token not found.");

            if (entity.RevokedAt is not null)
            {
                throw new ConflictException("Refresh token has already been used.");
            }

            entity.ReplacedByTokenId = jti;
            entity.RevokedAt = revokedAt;
            entity.RevokeReason = RefreshTokenRevokeReason.ROTATION_REQUIRED;
            entity.RevokedByIp = revokedByIp;

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var affectedRows = await _context.RefreshTokens
            .Where(x => x.TokenHash == tokenHash && x.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ReplacedByTokenId, jti)
                    .SetProperty(x => x.RevokedAt, revokedAt)
                    .SetProperty(x => x.RevokeReason, RefreshTokenRevokeReason.ROTATION_REQUIRED)
                    .SetProperty(x => x.RevokedByIp, revokedByIp),
                cancellationToken)
            .ConfigureAwait(false);

        if (affectedRows == 0)
        {
            var exists = await _context.RefreshTokens
                .AnyAsync(x => x.TokenHash == tokenHash, cancellationToken)
                .ConfigureAwait(false);

            if (!exists)
            {
                throw new InvalidOperationException("Refresh token not found.");
            }

            throw new ConflictException("Refresh token has already been used.");
        }
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
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity is null || entity.RevokedAt is not null)
        {
            return;
        }

        entity.RevokedAt = DateTime.UtcNow;
        entity.RevokeReason = RefreshTokenRevokeReason.USER_LOGOUT;
        entity.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
