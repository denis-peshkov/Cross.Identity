namespace Cross.Identity.Services.ExternalOAuth;

/// <summary>
/// External OAuth: initiate/callback, exchange code for provider token, user provisioning.
/// OAuth state (see <see cref="ExternalLoginStatePayload"/>) is stored in
/// <c>auth.ExternalLoginStates</c> — see <see cref="InitiateAsync"/>, <see cref="ResolveStateAsync"/>.
/// </summary>
internal sealed class ExternalLoginService : IExternalLoginService
{
    private readonly IdentityContext _identityContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalLoginOptions _options;
    private readonly ILogger<ExternalLoginService> _logger;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICommunicationEndpointUpsertService _communicationEndpoints;
    private readonly IExternalLoginUserProvisioner? _userProvisioner;

    public ExternalLoginService(
        IdentityContext identityContext,
        IHttpClientFactory httpClientFactory,
        IOptionsSnapshot<ExternalLoginOptions> options,
        ILogger<ExternalLoginService> logger,
        IJwtTokenService jwtTokenService,
        ICommunicationEndpointUpsertService communicationEndpoints,
        IExternalLoginUserProvisioner? userProvisioner = null)
    {
        _identityContext = identityContext;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _jwtTokenService = jwtTokenService;
        _communicationEndpoints = communicationEndpoints;
        _userProvisioner = userProvisioner;
    }

    /// <inheritdoc/>
    public async Task<string> InitiateAsync(
        string provider,
        string? returnUrl,
        Guid? userAccountId,
        CancellationToken cancellationToken)
    {
        if (!ExternalOAuthProviders.TryGet(provider, out var definition))
        {
            throw new NotFoundException($"Provider '{provider}' is not supported.");
        }

        if (!_options.Providers.TryGetValue(provider, out var providerOptions) || !providerOptions.IsConfigured)
        {
            throw new ValidationException($"Provider '{provider}' is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.CallbackUrl))
        {
            throw new InvalidOperationException("Authentication:ExternalLogin:CallbackUrl is not configured.");
        }

        var providerEntity = await _identityContext.Providers
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.Name.ToLower() == provider.ToLower())
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (providerEntity is null)
        {
            throw new NotFoundException($"Provider '{provider}' is not enabled.");
        }

        if (userAccountId.HasValue)
        {
            if (userAccountId.Value == Guid.Empty)
            {
                throw new ValidationException("UserAccountId is required for account linking.");
            }

            var accountExists = await _identityContext.UsersAccounts
                .AsNoTracking()
                .AnyAsync(x => x.Id == userAccountId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!accountExists)
            {
                throw new NotFoundException("Current user account was not found.");
            }

            var alreadyLinked = await _identityContext.UsersExternalLogins
                .AsNoTracking()
                .AnyAsync(x => x.UserAccountId == userAccountId.Value && x.ProviderId == providerEntity.Id, cancellationToken)
                .ConfigureAwait(false);

            if (alreadyLinked)
            {
                throw new ValidationException($"Provider '{provider}' is already linked to the current user.");
            }
        }

        var now = DateTime.UtcNow;
        var payload = new ExternalLoginStatePayload
        {
            Nonce = Guid.NewGuid().ToString("N"),
            Provider = providerEntity.Name,
            ReturnUrl = returnUrl,
            UserAccountId = userAccountId,
        };

        await _identityContext.ExternalLoginStates.AddAsync(new ExternalLoginStateEntity
        {
            Id = Guid.NewGuid(),
            Nonce = payload.Nonce,
            Provider = payload.Provider,
            ReturnUrl = payload.ReturnUrl,
            UserAccountId = payload.UserAccountId,
            CreatedAt = now,
            ExpiresAt = now.Add(_options.StateLifetime),
        }, cancellationToken).ConfigureAwait(false);
        await _identityContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var state = EncodeState(payload);
        return BuildAuthorizationUrl(definition, providerOptions, state);
    }

    /// <inheritdoc/>
    public async Task<ExternalLoginCompletion> CompleteAsync(
        string code,
        string state,
        string? error,
        string? errorDescription,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            throw new ValidationException(errorDescription ?? error);
        }

        var payload = await ResolveStateAsync(state, cancellationToken).ConfigureAwait(false);
        var isLinking = payload.UserAccountId.HasValue
            || IsExternalLoginLinkReturnUrl(payload.ReturnUrl);

        if (isLinking && !payload.UserAccountId.HasValue)
        {
            throw new NotAuthorizedException("Authentication is required to link an external login.");
        }

        if (!ExternalOAuthProviders.TryGet(payload.Provider, out var definition))
        {
            throw new NotFoundException($"Provider '{payload.Provider}' is not supported.");
        }

        if (!_options.Providers.TryGetValue(payload.Provider, out var providerOptions) || !providerOptions.IsConfigured)
        {
            throw new ValidationException($"Provider '{payload.Provider}' is not configured.");
        }

        var providerEntity = await _identityContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsEnabled && x.Name.ToLower() == payload.Provider.ToLower(), cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Provider '{payload.Provider}' is not enabled.");

        var httpClient = _httpClientFactory.CreateClient(nameof(ExternalLoginService));
        var accessToken = await ExchangeCodeAsync(httpClient, definition, providerOptions, code, cancellationToken).ConfigureAwait(false);
        var profile = await definition.FetchProfileAsync(httpClient, accessToken, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(profile.ProviderUserId))
        {
            throw new InvalidOperationException("External provider user id was not returned.");
        }

        var userAccountId = await ResolveOrCreateUserAsync(
            providerEntity,
            profile,
            payload.UserAccountId,
            cancellationToken).ConfigureAwait(false);

        await UserAccountGuard.EnsureIsActiveAsync(_identityContext, userAccountId, cancellationToken).ConfigureAwait(false);

        if (_userProvisioner is not null)
        {
            await _userProvisioner.ProvisionAsync(userAccountId, profile, cancellationToken).ConfigureAwait(false);
        }

        await UpsertExternalLoginAsync(providerEntity, userAccountId, profile, cancellationToken).ConfigureAwait(false);

        return new ExternalLoginCompletion(userAccountId, isLinking);
    }

    /// <inheritdoc/>
    public async Task UnlinkAsync(
        string provider,
        Guid userAccountId,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        if (userAccountId == Guid.Empty)
            throw new ValidationException("UserAccountId is required to unlink an external login.");

        var providerEntity = await _identityContext.Providers
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.Name.ToLower() == provider.ToLower())
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Provider '{provider}' is not enabled.");

        var login = await _identityContext.UsersExternalLogins
            .FirstOrDefaultAsync(
                x => x.UserAccountId == userAccountId && x.ProviderId == providerEntity.Id,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Provider '{provider}' is not linked to the current user.");

        var account = await _identityContext.UsersAccounts
            .FirstOrDefaultAsync(x => x.Id == userAccountId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Current user account was not found.");

        var remainingExternalLogins = await _identityContext.UsersExternalLogins
            .CountAsync(x => x.UserAccountId == userAccountId && x.Id != login.Id, cancellationToken)
            .ConfigureAwait(false);

        var hasPassword = !string.IsNullOrWhiteSpace(account.PasswordPhc);
        if (!hasPassword && remainingExternalLogins == 0)
        {
            throw new ValidationException(
                "Cannot unlink the last login method. Set a password or link another provider first.");
        }

        _identityContext.UsersExternalLogins.Remove(login);
        account.SecurityStamp = Guid.NewGuid();

        await _jwtTokenService
            .RevokeAllTokensForUserAsync(userAccountId, RefreshTokenRevokedReason.EXTERNAL_LOGIN_REMOVED, hostSuppliedClientContext, cancellationToken)
            .ConfigureAwait(false);

        await _identityContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Unlinked external login. UserAccountId={UserAccountId} Provider={Provider}",
            userAccountId,
            providerEntity.Name);
    }

    /// <inheritdoc/>
    public async Task<ExternalLoginOverviewDto> GetAllAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        if (userAccountId == Guid.Empty)
            throw new ValidationException("UserAccountId is required to list external logins.");

        var account = await _identityContext.UsersAccounts
            .AsNoTracking()
            .Where(x => x.Id == userAccountId)
            .Select(x => new { x.Email })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Current user account was not found.");

        var providers = await _identityContext.Providers
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var connectedLogins = await _identityContext.UsersExternalLogins
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId)
            .Select(x => new { x.ProviderId, x.ProviderEmail, x.AvatarUrl, x.ProfileUrl })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var connectedByProviderId = connectedLogins.ToDictionary(
            x => x.ProviderId,
            x => new { x.ProviderEmail, x.AvatarUrl, x.ProfileUrl });

        var result = new List<ExternalLoginProviderItemDto>();
        foreach (var provider in providers)
        {
            var isConnected = connectedByProviderId.ContainsKey(provider.Id);
            if (!isConnected && !IsProviderConfigured(provider.Name))
                continue;

            connectedByProviderId.TryGetValue(provider.Id, out var connectedLogin);
            result.Add(new ExternalLoginProviderItemDto
            {
                Provider = provider.Name,
                DisplayName = provider.Name,
                IsConnected = isConnected,
                ProviderEmail = connectedLogin?.ProviderEmail,
                AvatarUrl = connectedLogin?.AvatarUrl ?? connectedLogin?.ProfileUrl,
            });
        }

        return new ExternalLoginOverviewDto
        {
            AccountEmail = account.Email,
            Providers = result,
        };
    }

    private bool IsProviderConfigured(string providerName)
    {
        if (!_options.Providers.TryGetValue(providerName, out var providerOptions))
            return false;

        return providerOptions.IsConfigured;
    }

    private string BuildAuthorizationUrl(
        ExternalOAuthProviderDefinition definition,
        ExternalLoginProviderOptions providerOptions,
        string state)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = providerOptions.ClientId;
        query["redirect_uri"] = _options.CallbackUrl;
        query["response_type"] = "code";
        query["scope"] = definition.Scope;
        query["state"] = state;

        if (string.Equals(definition.Scheme, "microsoft", StringComparison.OrdinalIgnoreCase))
        {
            query["response_mode"] = "query";
        }

        return $"{definition.AuthorizationEndpoint}?{query}";
    }

    private async Task<string> ExchangeCodeAsync(
        HttpClient httpClient,
        ExternalOAuthProviderDefinition definition,
        ExternalLoginProviderOptions providerOptions,
        string code,
        CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _options.CallbackUrl,
            ["client_id"] = providerOptions.ClientId,
            ["client_secret"] = providerOptions.ClientSecret,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, definition.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form),
        };

        if (string.Equals(definition.Scheme, "github", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd("peshkov.biz");
        }

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "External OAuth token exchange failed. Provider={Provider} Status={Status} Body={Body}",
                definition.Scheme,
                (int)response.StatusCode,
                body);
            throw new ValidationException("External login failed during token exchange.");
        }

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("access_token", out var accessTokenNode))
        {
            throw new InvalidOperationException("External provider did not return access_token.");
        }

        return accessTokenNode.GetString()
            ?? throw new InvalidOperationException("External provider returned an empty access_token.");
    }

    private async Task<ExternalLoginStatePayload> ResolveStateAsync(
        string state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ValidationException("State is required.");
        }

        ExternalLoginStatePayload payload;
        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(state));
            payload = JsonSerializer.Deserialize<ExternalLoginStatePayload>(json)
                ?? throw new InvalidOperationException("OAuth state payload is empty.");
        }
        catch (JsonException)
        {
            throw new ValidationException("OAuth state is invalid.");
        }
        catch (FormatException)
        {
            throw new ValidationException("OAuth state is invalid.");
        }
        catch (InvalidOperationException)
        {
            throw new ValidationException("OAuth state is invalid.");
        }

        var now = DateTime.UtcNow;
        var entity = await _identityContext.ExternalLoginStates
            .FirstOrDefaultAsync(
                x => x.Nonce == payload.Nonce && x.ExpiresAt > now,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is null
            || !string.Equals(entity.Provider, payload.Provider, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("OAuth state has expired or was already used.");
        }

        var result = new ExternalLoginStatePayload
        {
            Nonce = entity.Nonce,
            Provider = entity.Provider,
            ReturnUrl = entity.ReturnUrl,
            UserAccountId = entity.UserAccountId,
        };

        _identityContext.ExternalLoginStates.Remove(entity);

        try
        {
            await _identityContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ValidationException("OAuth state has expired or was already used.");
        }

        return result;
    }

    private async Task<Guid> ResolveOrCreateUserAsync(
        ProviderEntity providerEntity,
        ExternalOAuthProfile profile,
        Guid? linkUserAccountId,
        CancellationToken cancellationToken)
    {
        var existingLink = await _identityContext.UsersExternalLogins
            .AsNoTracking()
            .Where(x => x.ProviderId == providerEntity.Id && x.ProviderUserId == profile.ProviderUserId)
            .Select(x => x.UserAccountId)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (linkUserAccountId.HasValue)
        {
            var accountExists = await _identityContext.UsersAccounts
                .AsNoTracking()
                .AnyAsync(x => x.Id == linkUserAccountId.Value, cancellationToken).ConfigureAwait(false);

            if (!accountExists)
            {
                throw new NotFoundException("Current user account was not found.");
            }

            if (existingLink != Guid.Empty && existingLink != linkUserAccountId.Value)
            {
                throw new ValidationException("This external account is already linked to another user.");
            }

            return linkUserAccountId.Value;
        }

        if (existingLink != Guid.Empty)
        {
            return existingLink;
        }

        var normalizedEmail = profile.Email?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var verifiedUserAccountId = await _identityContext.UsersAccounts
                .AsNoTracking()
                .Where(x => x.Email == normalizedEmail && x.EmailVerified)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (verifiedUserAccountId != Guid.Empty)
            {
                if (!profile.EmailVerified)
                {
                    throw new ValidationException(
                        "An account with this email already exists. Sign in with your password, verify your email, or link the provider from account settings.");
                }

                return verifiedUserAccountId;
            }

            if (!profile.EmailVerified)
            {
                var squatExists = await _identityContext.UsersAccounts
                    .AsNoTracking()
                    .AnyAsync(x => x.Email == normalizedEmail && !x.EmailVerified, cancellationToken)
                    .ConfigureAwait(false);

                if (squatExists)
                {
                    throw new ValidationException(
                        "An account with this email already exists. Sign in with your password, verify your email, or link the provider from account settings.");
                }
            }
        }

        return await CreateUserAsync(profile, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Guid> CreateUserAsync(ExternalOAuthProfile profile, CancellationToken cancellationToken)
    {
        var userAccountId = Guid.NewGuid();
        var normalizedEmail = profile.Email?.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var userName = profile.Email ?? profile.DisplayName ?? profile.ProviderUserId;

        var account = new UserAccountEntity
        {
            Id = userAccountId,
            Email = normalizedEmail,
            UserName = userName,
            NormalizedUserName = userName.Trim().ToLowerInvariant(),
            EmailVerified = profile.EmailVerified && !string.IsNullOrWhiteSpace(normalizedEmail),
            PhoneNumberVerified = false,
            TwoFactorEnabled = false,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            IsActive = true,
            CreatedAt = now,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
            LastLoginAt = now,
        };

        await _identityContext.UsersAccounts.AddAsync(account, cancellationToken).ConfigureAwait(false);
        await _identityContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return userAccountId;
    }

    private async Task UpsertExternalLoginAsync(
        ProviderEntity providerEntity,
        Guid userAccountId,
        ExternalOAuthProfile profile,
        CancellationToken cancellationToken)
    {
        var existing = await _identityContext.UsersExternalLogins
            .FirstOrDefaultAsync(
                x => x.ProviderId == providerEntity.Id && x.ProviderUserId == profile.ProviderUserId,
                cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            if (existing.UserAccountId != userAccountId)
            {
                throw new ValidationException("This external account is already linked to another user.");
            }

            existing.ProviderEmail = profile.Email;
            existing.DisplayName = profile.DisplayName;
            existing.AvatarUrl = profile.AvatarUrl;
            existing.UpdatedAt = DateTime.UtcNow;
            await _identityContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await SyncProviderEmailEndpointAsync(userAccountId, existing.Id, profile.Email, profile.EmailVerified, cancellationToken).ConfigureAwait(false);
            return;
        }

        var conflict = await _identityContext.UsersExternalLogins
            .AsNoTracking()
            .AnyAsync(x => x.UserAccountId == userAccountId && x.ProviderId == providerEntity.Id, cancellationToken).ConfigureAwait(false);

        if (conflict)
        {
            throw new ValidationException("This provider is already linked to the current user.");
        }

        await _identityContext.UsersExternalLogins.AddAsync(new UserExternalLoginEntity
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            UserAccount = null!,
            ProviderId = providerEntity.Id,
            ProviderEntity = null!,
            ProviderUserId = profile.ProviderUserId,
            ProviderEmail = profile.Email,
            DisplayName = profile.DisplayName,
            AvatarUrl = profile.AvatarUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        }, cancellationToken).ConfigureAwait(false);

        var account = await _identityContext.UsersAccounts
            .FirstOrDefaultAsync(x => x.Id == userAccountId, cancellationToken).ConfigureAwait(false);

        if (account is not null)
        {
            account.LastLoginAt = DateTime.UtcNow;
        }

        await _identityContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var loginId = await _identityContext.UsersExternalLogins
            .AsNoTracking()
            .Where(x => x.ProviderId == providerEntity.Id && x.ProviderUserId == profile.ProviderUserId)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        await SyncProviderEmailEndpointAsync(userAccountId, loginId, profile.Email, profile.EmailVerified, cancellationToken).ConfigureAwait(false);
    }

    private async Task SyncProviderEmailEndpointAsync(
        Guid userAccountId,
        Guid externalLoginId,
        string? providerEmail,
        bool emailVerified,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerEmail))
        {
            return;
        }

        await _communicationEndpoints
            .UpsertAsync(
                userAccountId,
                ChannelEnum.Email,
                providerEmail,
                CommunicationEndpointSource.ExternalProvider,
                isVerified: emailVerified,
                entityId: externalLoginId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool IsExternalLoginLinkReturnUrl(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl)
           && returnUrl.Contains("ExternalLogins", StringComparison.OrdinalIgnoreCase);

    private static string EncodeState(ExternalLoginStatePayload payload)
        => Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
