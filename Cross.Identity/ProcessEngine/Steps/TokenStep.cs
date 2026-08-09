namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for authenticating a user (by password or code)
/// and issuing a JWT token pair (access + refresh).
/// </summary>
internal sealed class TokenStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Identity selector (bag keys for field name + value).</summary>
    public required Selector Selector { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the password from. May be relative or absolute.</summary>
    public string? PasswordKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the code from. May be relative or absolute.</summary>
    public string? CodeKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the client IP from. May be relative or absolute.</summary>
    public required string IpAddressKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the User-Agent from. May be relative or absolute.</summary>
    public required string UserAgentKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the device fingerprint from. May be relative or absolute.</summary>
    public required string DeviceFingerprintKey { get; init; }

    /// <summary>Step logger.</summary>
    public ILogger Logger { get; set; }

    /// <summary>Token issuance service.</summary>
    public IJwtTokenService JwtTokenService { get; set; }

    /// <summary>Service for validating credentials and reading the user.</summary>
    public IUserService UserService { get; set; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);
        var selectorField = selector.Field;
        var selectorValue = selector.Value;

        string? passwordValue = null;
        if (PasswordKey != null)
        {
            ctx.TryGet(BagKey.Qualify(Kind, PasswordKey), out passwordValue);
        }

        string? codeValue = null;
        if (CodeKey != null)
        {
            ctx.TryGet(BagKey.Qualify(Kind, CodeKey), out codeValue);
        }

        // validation: when PasswordKey is absent from JSON, "" must not be treated as "password provided"
        var validated = false;
        if (PasswordKey != null && !string.IsNullOrEmpty(passwordValue))
        {
            validated = await UserService.ValidatePasswordAsync(selectorField, selectorValue, passwordValue, cancellationToken).ConfigureAwait(false);
        }
        else if (CodeKey != null && !string.IsNullOrEmpty(codeValue))
        {
            validated = await UserService.ValidateCodeAsync(selectorField, selectorValue, codeValue, cancellationToken).ConfigureAwait(false);
        }
        if (!validated)
        {
            ctx.Set(BagKey.Qualify(Kind, "IsInvalidCode"), true);
            return StepResult.Ok(Next);
        }

        var userAccount = await UserService.GetUserByAsync(selectorField, selectorValue, cancellationToken).ConfigureAwait(false);
        var user = userAccount.ToBag();
        ArgumentNullException.ThrowIfNull(user);
        var id     = user.TryGetValue("Id", out var idObj) && Guid.TryParse(idObj?.ToString(), out var guid) ? guid : Guid.Empty;
        var email = user.TryGetValue("Email", out var emailObj) ? emailObj?.ToString() : null;
        var phone = user.TryGetValue("PhoneNumber", out var phoneObj) ? phoneObj?.ToString() : null;
        var username    = user.TryGetValue("UserName", out var usernameObj) ? usernameObj?.ToString() : null;

        var familyId = Guid.NewGuid();
        var accessClaims = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Sub, id.ToString()),
            }
            .AddIfNotNull(ClaimTypes.Email, email)
            .AddIfNotNull(ClaimTypes.MobilePhone, phone)
            .AddIfNotNull(ClaimConstants.Username, username);
        ctx.TryGet<string?>(BagKey.Qualify(Kind, IpAddressKey), out var ipAddress);
        ctx.TryGet<string?>(BagKey.Qualify(Kind, UserAgentKey), out var userAgent);
        ctx.TryGet<string?>(BagKey.Qualify(Kind, DeviceFingerprintKey), out var deviceFingerprint);
        var accessToken = await JwtTokenService.GenerateAccessTokenAsync(id, familyId, new List<string>(), accessClaims, ipAddress, userAgent, deviceFingerprint, cancellationToken).ConfigureAwait(false);

        var refreshToken = await JwtTokenService.GenerateRefreshTokenAsync(id, familyId, new List<Claim> { new(JwtRegisteredClaimNames.Sub, id.ToString()) }, ipAddress, userAgent, deviceFingerprint, cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "AccessToken"), accessToken);
        ctx.Set(BagKey.Qualify(Kind, "RefreshToken"), refreshToken);
        ctx.Set(BagKey.Qualify(Kind, "TokenType"), "Bearer");
        ctx.Set(BagKey.Qualify(Kind, "ExpiresIn"), JwtTokenService.AccessTokenExpiresInSeconds);
        ctx.Set(BagKey.Qualify(Kind, "UserId"), id);
        ctx.Set(BagKey.Qualify(Kind, "IsInvalidCode"), false);

        return StepResult.Ok(Next);
    }
}
