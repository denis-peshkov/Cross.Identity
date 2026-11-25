namespace Cross.Identity.Services.ExternalOAuth;

/// <summary>
/// External OAuth: initiate/callback, exchange code for provider token, user provisioning.
/// OAuth state (see <see cref="ExternalLoginStatePayload"/>) is stored in
/// <c>auth.ExternalLoginStates</c> — see <see cref="InitiateAsync"/>, <see cref="ResolveStateAsync"/>.
/// </summary>
internal sealed class ExternalLoginService : IExternalLoginService
{
    private readonly IdentityContext _identityContext;
    private readonly IDbContextFactory<IdentityContext> _identityContextFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalLoginOptions _options;
    private readonly ILogger<ExternalLoginService> _logger;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICommunicationEndpointUpsertService _communicationEndpoints;
    private readonly IAuditService _audit;
    private readonly IExternalLoginUserProvisioner? _userProvisioner;

    public ExternalLoginService(
        IdentityContext identityContext,
        IDbContextFactory<IdentityContext> identityContextFactory,
        IHttpClientFactory httpClientFactory,
        IOptionsSnapshot<ExternalLoginOptions> options,
        ILogger<ExternalLoginService> logger,
        IJwtTokenService jwtTokenService,
        ICommunicationEndpointUpsertService communicationEndpoints,
        IAuditService audit,
        IExternalLoginUserProvisioner? userProvisioner = null)
    {
        _identityContext = identityContext;
        _identityContextFactory = identityContextFactory;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _jwtTokenService = jwtTokenService;
        _communicationEndpoints = communicationEndpoints;
        _audit = audit;
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

        // Apple requires FormCallbackUrl; other providers need CallbackUrl (validated in RequireRedirectUri).
        _ = RequireRedirectUri(definition);

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
        CancellationToken cancellationToken,
        string? userJson = null)
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
        var tokenResponse = await ExchangeCodeAsync(httpClient, definition, providerOptions, code, cancellationToken).ConfigureAwait(false);
        var profile = await definition
            .FetchProfileAsync(httpClient, tokenResponse, providerOptions.ClientSecret, cancellationToken)
            .ConfigureAwait(false);

        if (string.Equals(definition.Scheme, "apple", StringComparison.OrdinalIgnoreCase))
        {
            profile = ApplyAppleUserJson(profile, userJson);
        }

        if (string.IsNullOrWhiteSpace(profile.ProviderUserId))
        {
            throw new InvalidOperationException("External provider user id was not returned.");
        }

        // Email is required for sign-in / link / registration. ProviderUserId alone is not enough.
        if (string.IsNullOrWhiteSpace(profile.Email))
        {
            throw new ValidationException(
                "External provider did not return an email address. Sign-in cannot continue.");
        }

        if (payload.UserAccountId.HasValue)
        {
            await ThrowIfLinkConflictNeedsMergeAsync(
                    providerEntity,
                    profile,
                    payload.UserAccountId.Value,
                    cancellationToken)
                .ConfigureAwait(false);
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

        var emailEndpoints = await _identityContext.UsersCommunicationEndpoints
            .Where(x => x.UserAccountId == userAccountId && x.Channel == ChannelEnum.Email)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var matchedEndpoint = FindProviderEmailEndpoint(emailEndpoints, login.Id, login.ProviderEmail);
        if (matchedEndpoint is not null)
        {
            if (matchedEndpoint.IsPreferred || emailEndpoints.Count == 1)
            {
                throw new ValidationException(
                    "Cannot unlink this provider while its email is the preferred or only email endpoint.");
            }

            var removedAddress = matchedEndpoint.Address;
            _identityContext.UsersCommunicationEndpoints.Remove(matchedEndpoint);

            var remainingEmails = emailEndpoints.Where(x => x.Id != matchedEndpoint.Id).ToList();
            RetargetAccountEmailIfMatched(account, removedAddress, remainingEmails);
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
            .Select(x => new { x.Email, x.PasswordPhc })
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
            .Select(x => new { x.Id, x.ProviderId, x.ProviderEmail, x.AvatarUrl, x.ProfileUrl })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var connectedByProviderId = connectedLogins.ToDictionary(
            x => x.ProviderId,
            x => x);

        var hasPassword = !string.IsNullOrWhiteSpace(account.PasswordPhc);

        var emailEndpointRows = await _identityContext.UsersCommunicationEndpoints
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId && x.Channel == ChannelEnum.Email)
            .Select(x => new { x.Id, x.EntityId, x.Address, x.IsPreferred })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var emailEndpoints = emailEndpointRows
            .Select(x => new EmailEndpointUnlinkInfo(x.Id, x.EntityId, x.Address, x.IsPreferred))
            .ToList();

        var result = new List<ExternalLoginProviderItemDto>();
        foreach (var provider in providers)
        {
            var isConnected = connectedByProviderId.ContainsKey(provider.Id);
            if (!isConnected && !IsProviderConfigured(provider.Name))
                continue;

            connectedByProviderId.TryGetValue(provider.Id, out var connectedLogin);
            var canUnlink = false;
            if (isConnected && connectedLogin is not null)
            {
                var remainingExternals = connectedLogins.Count - 1;
                var isLastLogin = !hasPassword && remainingExternals == 0;
                var matched = FindProviderEmailEndpointInfo(
                    emailEndpoints,
                    connectedLogin.Id,
                    connectedLogin.ProviderEmail);
                var blocksOnEmail = matched is { } info
                    && (info.IsPreferred || emailEndpoints.Count == 1);
                canUnlink = !isLastLogin && !blocksOnEmail;
            }

            result.Add(new ExternalLoginProviderItemDto
            {
                Provider = provider.Name,
                DisplayName = provider.Name,
                IsConnected = isConnected,
                ProviderEmail = connectedLogin?.ProviderEmail,
                AvatarUrl = connectedLogin?.AvatarUrl ?? connectedLogin?.ProfileUrl,
                CanUnlink = canUnlink,
            });
        }

        return new ExternalLoginOverviewDto
        {
            AccountEmail = account.Email,
            Providers = result,
        };
    }

    private readonly record struct EmailEndpointUnlinkInfo(
        Guid Id,
        Guid? EntityId,
        string Address,
        bool IsPreferred);

    /// <summary>
    /// Match the email endpoint tied to this external login (by <c>EntityId</c> and/or normalized address).
    /// </summary>
    private static UserCommunicationEndpointEntity? FindProviderEmailEndpoint(
        IReadOnlyList<UserCommunicationEndpointEntity> emailEndpoints,
        Guid externalLoginId,
        string? providerEmail)
    {
        var info = FindProviderEmailEndpointInfo(
            emailEndpoints.Select(x => new EmailEndpointUnlinkInfo(x.Id, x.EntityId, x.Address, x.IsPreferred)).ToList(),
            externalLoginId,
            providerEmail);
        return info is null
            ? null
            : emailEndpoints.First(x => x.Id == info.Value.Id);
    }

    private static EmailEndpointUnlinkInfo? FindProviderEmailEndpointInfo(
        IReadOnlyList<EmailEndpointUnlinkInfo> emailEndpoints,
        Guid externalLoginId,
        string? providerEmail)
    {
        foreach (var endpoint in emailEndpoints)
        {
            if (endpoint.EntityId == externalLoginId)
            {
                return endpoint;
            }
        }

        if (string.IsNullOrWhiteSpace(providerEmail))
        {
            return null;
        }

        var normalized = ChannelEnum.Email.NormalizeAddress(providerEmail);
        foreach (var endpoint in emailEndpoints)
        {
            if (string.Equals(endpoint.Address, normalized, StringComparison.Ordinal))
            {
                return endpoint;
            }
        }

        return null;
    }

    /// <summary>
    /// When the removed address was the account primary email, point it at a remaining preferred email
    /// (or clear it) so the address can be claimed by a new registration.
    /// </summary>
    private static void RetargetAccountEmailIfMatched(
        UserAccountEntity account,
        string removedAddress,
        IReadOnlyList<UserCommunicationEndpointEntity> remainingEmailEndpoints)
    {
        var accountEmail = string.IsNullOrWhiteSpace(account.Email)
            ? null
            : ChannelEnum.Email.NormalizeAddress(account.Email);
        if (!string.Equals(accountEmail, removedAddress, StringComparison.Ordinal))
        {
            return;
        }

        var preferredEmail = remainingEmailEndpoints.FirstOrDefault(x => x.IsPreferred)
            ?? remainingEmailEndpoints.OrderByDescending(x => x.IsVerified).FirstOrDefault();

        if (preferredEmail is null)
        {
            account.Email = null;
            account.EmailVerified = false;
            return;
        }

        account.Email = preferredEmail.Address;
        account.EmailVerified = preferredEmail.IsVerified;
    }

    private bool IsProviderConfigured(string providerName)
    {
        if (!_options.Providers.TryGetValue(providerName, out var providerOptions))
            return false;

        return providerOptions.IsConfigured;
    }

    private string ResolveRedirectUri(ExternalOAuthProviderDefinition definition)
    {
        // Apple form_post needs a POST-capable host — FormCallbackUrl only (no CallbackUrl fallback).
        if (string.Equals(definition.Scheme, "apple", StringComparison.OrdinalIgnoreCase))
        {
            return _options.FormCallbackUrl;
        }

        return _options.CallbackUrl;
    }


    /// <inheritdoc/>
    public async Task<UserAccountsMergedDto> MergeAccountsAsync(
        string mergeToken,
        Guid survivorUserAccountId,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mergeToken))
            throw new ValidationException("Merge token is required.");
        if (survivorUserAccountId == Guid.Empty)
            throw new ValidationException("UserAccountId is required to merge accounts.");

        var now = DateTime.UtcNow;
        var pending = await _identityContext.AccountMergePendings
            .FirstOrDefaultAsync(
                x => x.MergeToken == mergeToken && x.ConsumedAt == null && x.ExpiresAt > now,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ValidationException("Merge token is invalid or has expired.");

        if (pending.SurvivorUserAccountId != survivorUserAccountId)
        {
            throw new ValidationException("Merge token does not belong to the current user.");
        }

        if (pending.SurvivorUserAccountId == pending.AbsorbedUserAccountId)
        {
            throw new ValidationException("Cannot merge an account into itself.");
        }

        var survivor = await _identityContext.UsersAccounts
            .FirstOrDefaultAsync(x => x.Id == pending.SurvivorUserAccountId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Current user account was not found.");

        var absorbed = await _identityContext.UsersAccounts
            .FirstOrDefaultAsync(x => x.Id == pending.AbsorbedUserAccountId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ValidationException("Absorbed user account was not found.");

        await UserAccountGuard.EnsureIsActiveAsync(_identityContext, survivor.Id, cancellationToken).ConfigureAwait(false);

        var survivorProviderIds = await _identityContext.UsersExternalLogins
            .Where(x => x.UserAccountId == survivor.Id)
            .Select(x => x.ProviderId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var survivorProviderIdSet = survivorProviderIds.ToHashSet();

        var absorbedLogins = await _identityContext.UsersExternalLogins
            .Where(x => x.UserAccountId == absorbed.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var absorbedProviderIds = absorbedLogins.Select(l => l.ProviderId).Distinct().ToList();
        var providerNames = await _identityContext.Providers
            .AsNoTracking()
            .Where(x => absorbedProviderIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var nameById = providerNames.ToDictionary(x => x.Id, x => x.Name);

        var movedProviders = new List<string>();

        foreach (var login in absorbedLogins)
        {
            if (survivorProviderIdSet.Contains(login.ProviderId))
            {
                // Survivor already has this provider — drop absorbed duplicate row.
                _identityContext.UsersExternalLogins.Remove(login);
                continue;
            }

            login.UserAccountId = survivor.Id;
            login.UpdatedAt = now;
            survivorProviderIdSet.Add(login.ProviderId);
            if (nameById.TryGetValue(login.ProviderId, out var providerName))
            {
                movedProviders.Add(providerName);
            }
        }

        var survivorHasPreferred = await _identityContext.UsersCommunicationEndpoints
            .AsNoTracking()
            .AnyAsync(x => x.UserAccountId == survivor.Id && x.IsPreferred, cancellationToken)
            .ConfigureAwait(false);

        var absorbedEndpoints = await _identityContext.UsersCommunicationEndpoints
            .Where(x => x.UserAccountId == absorbed.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var survivorEndpointKeys = await _identityContext.UsersCommunicationEndpoints
            .AsNoTracking()
            .Where(x => x.UserAccountId == survivor.Id)
            .Select(x => new { x.Channel, x.Address })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var survivorEndpointKeySet = survivorEndpointKeys
            .Select(x => (x.Channel, x.Address))
            .ToHashSet();

        foreach (var endpoint in absorbedEndpoints)
        {
            var key = (endpoint.Channel, endpoint.Address);
            if (survivorEndpointKeySet.Contains(key))
            {
                _identityContext.UsersCommunicationEndpoints.Remove(endpoint);
                continue;
            }

            endpoint.UserAccountId = survivor.Id;
            endpoint.UpdatedAt = now;
            if (survivorHasPreferred || endpoint.IsPreferred)
            {
                // At most one preferred per survivor after merge.
                endpoint.IsPreferred = !survivorHasPreferred && endpoint.IsPreferred;
                if (endpoint.IsPreferred)
                {
                    survivorHasPreferred = true;
                }
            }

            survivorEndpointKeySet.Add(key);
        }

        absorbed.IsActive = false;
        absorbed.SecurityStamp = Guid.NewGuid();
        survivor.SecurityStamp = Guid.NewGuid();

        pending.ConsumedAt = now;

        await _jwtTokenService
            .RevokeAllTokensForUserAsync(
                absorbed.Id,
                RefreshTokenRevokedReason.ACCOUNT_MERGED,
                hostSuppliedClientContext,
                cancellationToken)
            .ConfigureAwait(false);

        var movedProvidersNotes = movedProviders.Count == 0
            ? "(none)"
            : string.Join(',', movedProviders);

        _audit.Record(new AuditEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            UserAccountId = survivor.Id,
            UserAccount = null!,
            Operation = AuditOperation.UserAccountsMerged,
            EntityType = AuditEntityType.UserAccount,
            EntityId = absorbed.Id.ToString("D"),
            IpAddress = hostSuppliedClientContext.IpAddress,
            UserAgent = hostSuppliedClientContext.UserAgent,
            DeviceFingerprint = hostSuppliedClientContext.DeviceFingerprint,
            Notes = $"SurvivorUserAccountId={survivor.Id:D}; AbsorbedUserAccountId={absorbed.Id:D}; MovedProviders={movedProvidersNotes}",
        });

        await _identityContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Merged user accounts. SurvivorUserAccountId={SurvivorUserAccountId} AbsorbedUserAccountId={AbsorbedUserAccountId} MovedProviders={MovedProviders}",
            survivor.Id,
            absorbed.Id,
            movedProvidersNotes);

        return new UserAccountsMergedDto
        {
            SurvivorUserAccountId = survivor.Id,
            AbsorbedUserAccountId = absorbed.Id,
            MovedProviders = movedProviders,
        };
    }


    private string RequireRedirectUri(ExternalOAuthProviderDefinition definition)
    {
        var redirectUri = ResolveRedirectUri(definition);
        if (!string.IsNullOrWhiteSpace(redirectUri))
        {
            return redirectUri;
        }

        if (string.Equals(definition.Scheme, "apple", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Authentication:ExternalLogin:FormCallbackUrl is not configured.");
        }

        throw new InvalidOperationException("Authentication:ExternalLogin:CallbackUrl is not configured.");
    }

    private string BuildAuthorizationUrl(
        ExternalOAuthProviderDefinition definition,
        ExternalLoginProviderOptions providerOptions,
        string state)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = providerOptions.ClientId;
        query["redirect_uri"] = RequireRedirectUri(definition);
        query["response_type"] = "code";
        query["scope"] = definition.Scope;
        query["state"] = state;

        if (string.Equals(definition.Scheme, "microsoft", StringComparison.OrdinalIgnoreCase))
        {
            query["response_mode"] = "query";
        }

        // Apple requires form_post when requesting name/email scopes.
        if (string.Equals(definition.Scheme, "apple", StringComparison.OrdinalIgnoreCase))
        {
            query["response_mode"] = "form_post";
        }

        return $"{definition.AuthorizationEndpoint}?{query}";
    }

    private async Task<ExternalOAuthTokenResponse> ExchangeCodeAsync(
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
            ["redirect_uri"] = RequireRedirectUri(definition),
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

        var accessToken = accessTokenNode.GetString()
            ?? throw new InvalidOperationException("External provider returned an empty access_token.");

        string? idToken = null;
        if (document.RootElement.TryGetProperty("id_token", out var idTokenNode)
            && idTokenNode.ValueKind == JsonValueKind.String)
        {
            idToken = idTokenNode.GetString();
        }

        return new ExternalOAuthTokenResponse
        {
            AccessToken = accessToken,
            IdToken = idToken,
        };
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


    private async Task ThrowIfLinkConflictNeedsMergeAsync(
        ProviderEntity providerEntity,
        ExternalOAuthProfile profile,
        Guid survivorUserAccountId,
        CancellationToken cancellationToken)
    {
        var absorbedUserAccountId = await _identityContext.UsersExternalLogins
            .AsNoTracking()
            .Where(x => x.ProviderId == providerEntity.Id && x.ProviderUserId == profile.ProviderUserId)
            .Select(x => x.UserAccountId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (absorbedUserAccountId == Guid.Empty || absorbedUserAccountId == survivorUserAccountId)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var mergeToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        // Mint must survive ambient host TransactionScope when this method throws afterward.
        // Suppress alone is not enough if _identityContext's connection is already enlisted —
        // persist via a factory-created context (fresh connection) inside Suppress.
        using (var suppress = new TransactionScope(
                   TransactionScopeOption.Suppress,
                   TransactionScopeAsyncFlowOption.Enabled))
        {
            var mintContext = await _identityContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);
            try
            {
                await mintContext.AccountMergePendings.AddAsync(new AccountMergePendingEntity
                {
                    Id = Guid.NewGuid(),
                    MergeToken = mergeToken,
                    SurvivorUserAccountId = survivorUserAccountId,
                    AbsorbedUserAccountId = absorbedUserAccountId,
                    CreatedAt = now,
                    ExpiresAt = now.Add(_options.StateLifetime),
                    ConcurrencyStamp = Guid.NewGuid(),
                }, cancellationToken).ConfigureAwait(false);

                await mintContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await mintContext.DisposeAsync().ConfigureAwait(false);
            }

            suppress.Complete();
        }

        throw AccountMergeRequired.Create(
            "This external account is already linked to another user.",
            mergeToken,
            absorbedUserAccountId,
            survivorUserAccountId,
            providerEntity.Name);
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
            // Apple (and similar) omit name on subsequent sign-ins — do not wipe a previously saved value.
            if (!string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                existing.DisplayName = profile.DisplayName;
            }

            existing.AvatarUrl = profile.AvatarUrl;
            existing.UpdatedAt = DateTime.UtcNow;
            await _identityContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await SyncProviderEmailEndpointAsync(userAccountId, existing.Id, profile.Email, cancellationToken).ConfigureAwait(false);
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

        await SyncProviderEmailEndpointAsync(userAccountId, loginId, profile.Email, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Upserts the provider email as a verified <see cref="CommunicationEndpointSource.ExternalProvider"/> endpoint.
    /// Linked OAuth emails are trusted without OTP; <see cref="ExternalOAuthProfile.EmailVerified"/> still
    /// gates new-account <c>UsersAccounts.EmailVerified</c> and email-collision rules.
    /// </summary>
    private async Task SyncProviderEmailEndpointAsync(
        Guid userAccountId,
        Guid externalLoginId,
        string? providerEmail,
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
                isVerified: true,
                entityId: externalLoginId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Apple posts given/family name only once via form_post <c>user</c> JSON (not in <c>id_token</c>).
    /// Malformed payloads are ignored so a bad client field cannot break login.
    /// </summary>
    private static ExternalOAuthProfile ApplyAppleUserJson(ExternalOAuthProfile profile, string? userJson)
    {
        if (string.IsNullOrWhiteSpace(userJson) || !string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            return profile;
        }

        try
        {
            using var document = JsonDocument.Parse(userJson);
            var root = document.RootElement;
            // TryGetProperty throws InvalidOperationException when root is not an object (e.g. []).
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("name", out var nameNode)
                || nameNode.ValueKind != JsonValueKind.Object)
            {
                return profile;
            }

            var firstName = nameNode.TryGetProperty("firstName", out var firstNode)
                && firstNode.ValueKind == JsonValueKind.String
                    ? firstNode.GetString()
                    : null;
            var lastName = nameNode.TryGetProperty("lastName", out var lastNode)
                && lastNode.ValueKind == JsonValueKind.String
                    ? lastNode.GetString()
                    : null;

            var displayName = string.Join(
                ' ',
                new[] { firstName, lastName }.Where(static x => !string.IsNullOrWhiteSpace(x)));

            if (string.IsNullOrWhiteSpace(displayName))
            {
                return profile;
            }

            return new ExternalOAuthProfile
            {
                ProviderUserId = profile.ProviderUserId,
                Email = profile.Email,
                EmailVerified = profile.EmailVerified,
                DisplayName = displayName,
                AvatarUrl = profile.AvatarUrl,
            };
        }
        catch (JsonException)
        {
            return profile;
        }
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
