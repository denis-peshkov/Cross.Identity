namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for authenticating a user (by password or code)
/// and issuing a JWT token pair (access + refresh).
/// <para>
/// Keys:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/>, <see cref="PasswordKey"/>, and <see cref="CodeKey"/>:
///     if a key is relative (no dot), it is read as <c>"{Kind}.{Key}"</c>;
///     to read data from another step, specify an absolute key such as <c>"other-step.Field"</c>.</description></item>
///   <item><description>The result is always written to keys:
///     <c>AccessToken</c>, <c>RefreshToken</c>, <c>TokenType</c>, <c>ExpiresIn</c>, <c>UserId</c>
///     (with the <c>{Kind}.</c> prefix for relative access).</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class TokenStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read e-mail/login from. May be relative or absolute.</summary>
    public required string SelectorKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the password from. May be relative or absolute.</summary>
    public string? PasswordKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the code from. May be relative or absolute.</summary>
    public string? CodeKey { get; init; }

    /// <summary>Step logger.</summary>
    public ILogger Logger { get; set; }

    /// <summary>Token issuance service.</summary>
    public IJwtTokenService JwtTokenService { get; set; }

    /// <summary>Service for validating credentials and reading the user.</summary>
    public IUserService UserService { get; set; }

    /// <summary>User lookup settings: which field to search by (for example, "Email" or "Phone").</summary>
    public required ResolveBy ResolveBy { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // 1) email/login + password or code (absolute keys such as collectForm.Email are not prefixed with the token step Kind)
        var selectorValue = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));
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

        // 2) validation: when PasswordKey is absent from JSON, "" must not be treated as "password provided" — otherwise the code branch does not run (TokenByCode).
        var validated = false;
        if (PasswordKey != null && !string.IsNullOrEmpty(passwordValue))
        {
            validated = await UserService.ValidatePasswordAsync(ResolveBy.Field, selectorValue, passwordValue, cancellationToken).ConfigureAwait(false);
        }
        else if (CodeKey != null && !string.IsNullOrEmpty(codeValue))
        {
            validated = await UserService.ValidateCodeAsync(ResolveBy.Field, selectorValue, codeValue, cancellationToken).ConfigureAwait(false);
        }
        if (!validated)
        {
            ctx.Set(BagKey.Qualify(Kind, "IsInvalidCode"), true);
            return StepResult.Ok(Next);
        }

        // 3) get user data
        var user = (await UserService.GetUserByAsync(ResolveBy.Field, selectorValue, cancellationToken).ConfigureAwait(false)).ToBag();
        ArgumentNullException.ThrowIfNull(user);
        var id     = user.TryGetValue("Id", out var idObj) && Guid.TryParse(idObj?.ToString(), out var guid) ? guid : Guid.Empty;
        var email = user.TryGetValue("Email", out var emailObj) ? emailObj?.ToString() : null;
        var phone = user.TryGetValue("Phone", out var phoneObj) ? phoneObj?.ToString() : null;
        var username    = user.TryGetValue("UserName", out var usernameObj) ? usernameObj?.ToString() : null;

        // 4) generate AccessToken
        var familyId = Guid.NewGuid();
        var accessClaims = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Sub, id.ToString()),
                // new (JwtRegisteredClaimNames.NameId, id),
                // new (ClaimTypes.NameIdentifier, id), // NameId ???
            }
            // .AddIfNotNull(JwtRegisteredClaimNames.Email, email)
            .AddIfNotNull(ClaimTypes.Email, email)
            .AddIfNotNull(ClaimTypes.MobilePhone, phone)
            .AddIfNotNull(ClaimConstants.Username, username);
        var accessToken = await JwtTokenService.GenerateAccessTokenAsync(id, familyId, new List<string>(), accessClaims).ConfigureAwait(false);

        // 5) generate RefreshToken
        var refreshToken = await JwtTokenService.GenerateRefreshTokenAsync(id, familyId, new List<Claim> { new(JwtRegisteredClaimNames.Sub, id.ToString()) }).ConfigureAwait(false);

        // 6) store the token in Bag
        ctx.Set(BagKey.Qualify(Kind, "AccessToken"), accessToken);
        ctx.Set(BagKey.Qualify(Kind, "RefreshToken"), refreshToken);
        ctx.Set(BagKey.Qualify(Kind, "TokenType"), "Bearer");
        ctx.Set(BagKey.Qualify(Kind, "ExpiresIn"), JwtTokenService.AccessTokenExpiresInSeconds);
        ctx.Set(BagKey.Qualify(Kind, "UserId"), id);
        ctx.Set(BagKey.Qualify(Kind, "IsInvalidCode"), false);

        return StepResult.Ok(Next);
    }
}
