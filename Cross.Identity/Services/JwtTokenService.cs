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
    public async Task<bool> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        if (_options.Jwt.UseEncryption)
        {
            parameters.TokenDecryptionKey = _encryptionKey;
        }

        var validation = await _handler
            .ValidateTokenAsync(accessToken, parameters)
            .ConfigureAwait(false);
        if (!validation.IsValid || validation.ClaimsIdentity is null)
        {
            return false;
        }

        var jtiClaim = validation.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (!Guid.TryParse(jtiClaim, out var jtiGuid))
        {
            return false;
        }

        var entity = await _context.AccessTokens.FindAsync(new object[] { jtiGuid }, cancellationToken)
            .ConfigureAwait(false);

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
    public async Task EnsureRefreshTokenActiveForRotationAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        var entity = await _context.RefreshTokens
            .Where(x => x.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new NotAuthorizedException("Invalid or expired refresh token.");
        }

        if (entity.RevokedAt is not null)
        {
            await HandleRefreshTokenReplayAsync(entity, cancellationToken).ConfigureAwait(false);
            throw new ConflictException("Refresh token has already been used.");
        }

        if (entity.ExpiresAt < DateTime.UtcNow
            || entity.AbsoluteExpiresAt < DateTime.UtcNow
            || entity.CreatedAt > DateTime.UtcNow)
        {
            throw new NotAuthorizedException("Invalid or expired refresh token.");
        }
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
    public string? GetClaimValue(string token, params string[] claimTypes)
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

        return result;
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

        var entity = await _context.RefreshTokens
            .Where(x => x.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
                     ?? throw new InvalidOperationException("Refresh token not found.");

        if (entity.RevokedAt is not null)
        {
            // Concurrent refresh or replay of an already rotated token — see REPLAY_DETECTED.
            await HandleRefreshTokenReplayAsync(entity, cancellationToken).ConfigureAwait(false);
            throw new ConflictException("Refresh token has already been used.");
        }

        entity.ReplacedByTokenId = jti;
        entity.RevokedAt = DateTime.UtcNow;
        entity.RevokeReason = RefreshTokenRevokeReason.ROTATION_REQUIRED;
        entity.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request won the rotation race; treat as reuse and kill the family
            // so a possible attacker-held successor token cannot survive.
            await _context.Entry(entity).ReloadAsync(cancellationToken).ConfigureAwait(false);
            await HandleRefreshTokenReplayAsync(entity, cancellationToken).ConfigureAwait(false);
            throw new ConflictException("Refresh token has already been used.");
        }
    }

    /// <inheritdoc/>
    public async Task RevokeRefreshTokenFamilyAsync(
        Guid familyId,
        RefreshTokenRevokeReason reason,
        CancellationToken cancellationToken = default)
    {
        await RevokeRefreshTokenFamilyCoreAsync(familyId, reason, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Refresh-token reuse after rotation: mark the presented token and revoke the whole family.
    /// </summary>
    /// <remarks>
    /// Threat model (why family revoke, not only Conflict on this token):
    /// <list type="number">
    ///   <item><description>Attacker steals <c>R1</c> and refreshes first → active <c>R2</c>, <c>R1</c> revoked.</description></item>
    ///   <item><description>Victim sends <c>R1</c> → reuse of revoked refresh.</description></item>
    ///   <item><description>Without family revoke: victim is rejected; attacker keeps live <c>R2</c>.</description></item>
    ///   <item><description>With family revoke: <c>R2</c> and access tokens in the family are revoked too.</description></item>
    /// </list>
    /// Legitimate retry / double-refresh can look the same — accepted trade-off.
    /// </remarks>
    private async Task HandleRefreshTokenReplayAsync(
        RefreshTokenEntity reusedToken,
        CancellationToken cancellationToken)
    {
        var revokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        // Audit: the presented token was already revoked (usually ROTATION_REQUIRED); record that reuse was detected.
        reusedToken.RevokeReason = RefreshTokenRevokeReason.REPLAY_DETECTED;
        reusedToken.RevokedByIp = revokedByIp;

        await RevokeRefreshTokenFamilyCoreAsync(
                reusedToken.FamilyId,
                RefreshTokenRevokeReason.REPLAY_DETECTED,
                cancellationToken)
            .ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RevokeRefreshTokenFamilyCoreAsync(
        Guid familyId,
        RefreshTokenRevokeReason reason,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var revokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var refreshTokens = await _context.RefreshTokens
            .Where(x => x.FamilyId == familyId && x.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in refreshTokens)
        {
            token.RevokedAt = now;
            token.RevokeReason = reason;
            token.RevokedByIp = revokedByIp;
        }

        var accessTokens = await _context.AccessTokens
            .Where(x => x.FamilyId == familyId && x.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in accessTokens)
        {
            token.RevokedAt = now;
            token.RevokeReason = reason;
            token.RevokedByIp = revokedByIp;
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

    /// <inheritdoc/>
    public async Task RevokeAllTokensForLogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        var entity = await _context.RefreshTokens
            .AsNoTracking()
            .Where(x => x.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity is null
            || entity.RevokedAt is not null
            || entity.ExpiresAt < DateTime.UtcNow
            || entity.AbsoluteExpiresAt < DateTime.UtcNow
            || entity.CreatedAt > DateTime.UtcNow)
        {
            throw new NotAuthorizedException("Invalid or expired refresh token.");
        }

        await RevokeAllTokensForUserAsync(entity.UserId, RefreshTokenRevokeReason.USER_LOGOUT_ALL, cancellationToken)
            .ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RevokeAllTokensForUserAsync(
        Guid userId,
        RefreshTokenRevokeReason reason,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var revokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var refreshTokens = await _context.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in refreshTokens)
        {
            token.RevokedAt = now;
            token.RevokeReason = reason;
            token.RevokedByIp = revokedByIp;
        }

        var accessTokens = await _context.AccessTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in accessTokens)
        {
            token.RevokedAt = now;
            token.RevokeReason = reason;
            token.RevokedByIp = revokedByIp;
        }
    }
}
