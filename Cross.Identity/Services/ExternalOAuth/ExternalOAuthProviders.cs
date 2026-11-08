namespace Cross.Identity.Services.ExternalOAuth;

internal static class ExternalOAuthProviders
{
    private static readonly IReadOnlyDictionary<string, ExternalOAuthProviderDefinition> Definitions =
        new Dictionary<string, ExternalOAuthProviderDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["Google"] = new ExternalOAuthProviderDefinition
            {
                Scheme = "google",
                AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
                TokenEndpoint = "https://oauth2.googleapis.com/token",
                Scope = "openid email profile",
                FetchProfileAsync = FetchGoogleProfileAsync,
            },
            ["Microsoft"] = new ExternalOAuthProviderDefinition
            {
                Scheme = "microsoft",
                AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
                TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                Scope = "openid email profile User.Read",
                FetchProfileAsync = FetchMicrosoftProfileAsync,
            },
            ["GitHub"] = new ExternalOAuthProviderDefinition
            {
                Scheme = "github",
                AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
                TokenEndpoint = "https://github.com/login/oauth/access_token",
                Scope = "read:user user:email",
                FetchProfileAsync = FetchGitHubProfileAsync,
            },
            ["Apple"] = new ExternalOAuthProviderDefinition
            {
                Scheme = "apple",
                AuthorizationEndpoint = "https://appleid.apple.com/auth/authorize",
                TokenEndpoint = "https://appleid.apple.com/auth/token",
                Scope = "name email",
                FetchProfileAsync = FetchAppleProfileAsync,
            },
            ["Facebook"] = new ExternalOAuthProviderDefinition
            {
                Scheme = "facebook",
                AuthorizationEndpoint = "https://www.facebook.com/v21.0/dialog/oauth",
                TokenEndpoint = "https://graph.facebook.com/v21.0/oauth/access_token",
                Scope = "email public_profile",
                FetchProfileAsync = FetchFacebookProfileAsync,
            },
        };

    public static bool TryGet(string provider, out ExternalOAuthProviderDefinition definition)
        => Definitions.TryGetValue(provider, out definition!);

    private static async Task<ExternalOAuthProfile> FetchGoogleProfileAsync(
        HttpClient httpClient,
        ExternalOAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;

        return new ExternalOAuthProfile
        {
            ProviderUserId = root.GetProperty("sub").GetString() ?? string.Empty,
            Email = root.TryGetProperty("email", out var email) ? email.GetString() : null,
            EmailVerified = root.TryGetProperty("email_verified", out var emailVerified) && emailVerified.GetBoolean(),
            DisplayName = root.TryGetProperty("name", out var name) ? name.GetString() : null,
            AvatarUrl = root.TryGetProperty("picture", out var picture) ? picture.GetString() : null,
        };
    }

    private static async Task<ExternalOAuthProfile> FetchMicrosoftProfileAsync(
        HttpClient httpClient,
        ExternalOAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

        using var meResponse = await httpClient.SendAsync(meRequest, cancellationToken).ConfigureAwait(false);
        meResponse.EnsureSuccessStatusCode();

        using var meDocument = await JsonDocument.ParseAsync(await meResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
        var me = meDocument.RootElement;

        var graphEmail = me.TryGetProperty("mail", out var mail) && mail.ValueKind == JsonValueKind.String
            ? mail.GetString()
            : me.TryGetProperty("userPrincipalName", out var upn) && upn.ValueKind == JsonValueKind.String
                ? upn.GetString()
                : null;

        // Graph mail/UPN alone is not mailbox attestation (admin-editable / non-SMTP UPN).
        // EmailVerified only when OIDC userinfo supplies a non-empty email AND email_verified.
        string? email = graphEmail;
        string? oidcEmailAddress = null;
        var oidcEmailVerified = false;

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/oidc/userinfo");
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

        using var userInfoResponse = await httpClient.SendAsync(userInfoRequest, cancellationToken).ConfigureAwait(false);
        if (userInfoResponse.IsSuccessStatusCode)
        {
            using var userInfoDocument = await JsonDocument.ParseAsync(
                await userInfoResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var userInfo = userInfoDocument.RootElement;

            if (userInfo.TryGetProperty("email", out var oidcEmail) && oidcEmail.ValueKind == JsonValueKind.String)
            {
                var oidcEmailValue = oidcEmail.GetString();
                if (!string.IsNullOrWhiteSpace(oidcEmailValue))
                {
                    oidcEmailAddress = oidcEmailValue;
                    email = oidcEmailValue;
                }
            }

            oidcEmailVerified = userInfo.TryGetProperty("email_verified", out var verifiedNode)
                && verifiedNode.ValueKind == JsonValueKind.True;
        }

        return new ExternalOAuthProfile
        {
            ProviderUserId = me.GetProperty("id").GetString() ?? string.Empty,
            Email = email,
            EmailVerified = oidcEmailVerified && !string.IsNullOrWhiteSpace(oidcEmailAddress),
            DisplayName = me.TryGetProperty("displayName", out var displayName) ? displayName.GetString() : null,
        };
    }

    private static async Task<ExternalOAuthProfile> FetchGitHubProfileAsync(
        HttpClient httpClient,
        ExternalOAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        userRequest.Headers.UserAgent.ParseAdd("peshkov.biz");

        using var userResponse = await httpClient.SendAsync(userRequest, cancellationToken).ConfigureAwait(false);
        userResponse.EnsureSuccessStatusCode();

        using var userDocument = await JsonDocument.ParseAsync(await userResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
        var userRoot = userDocument.RootElement;
        var providerUserId = userRoot.GetProperty("id").GetInt64().ToString(CultureInfo.InvariantCulture);
        var displayName = userRoot.TryGetProperty("name", out var nameNode) && nameNode.ValueKind == JsonValueKind.String
            ? nameNode.GetString()
            : userRoot.TryGetProperty("login", out var loginNode) ? loginNode.GetString() : null;
        var avatarUrl = userRoot.TryGetProperty("avatar_url", out var avatarNode) ? avatarNode.GetString() : null;

        string? email = null;

        using var emailsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
        emailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        emailsRequest.Headers.UserAgent.ParseAdd("peshkov.biz");

        using var emailsResponse = await httpClient.SendAsync(emailsRequest, cancellationToken).ConfigureAwait(false);
        emailsResponse.EnsureSuccessStatusCode();

        using var emailsDocument = await JsonDocument.ParseAsync(await emailsResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
        string? verifiedPrimary = null;
        string? verifiedFallback = null;
        foreach (var item in emailsDocument.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("verified", out var verifiedNode) || !verifiedNode.GetBoolean())
            {
                continue;
            }

            var address = item.GetProperty("email").GetString();
            if (item.TryGetProperty("primary", out var primaryNode) && primaryNode.GetBoolean())
            {
                verifiedPrimary = address;
                break;
            }

            verifiedFallback ??= address;
        }

        email = verifiedPrimary ?? verifiedFallback;

        return new ExternalOAuthProfile
        {
            ProviderUserId = providerUserId,
            Email = email,
            EmailVerified = !string.IsNullOrWhiteSpace(email),
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
        };
    }

    private static Task<ExternalOAuthProfile> FetchAppleProfileAsync(
        HttpClient httpClient,
        ExternalOAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        _ = httpClient;
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(tokenResponse.IdToken))
        {
            throw new InvalidOperationException("Apple did not return id_token.");
        }

        // Profile claims live in the OIDC id_token from Apple's token endpoint (no userinfo Graph).
        // Token was just received over TLS from appleid.apple.com; payload is read without JWKS re-validation.
        JsonWebToken jwt;
        try
        {
            jwt = new JsonWebTokenHandler().ReadJsonWebToken(tokenResponse.IdToken);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException("Apple id_token is invalid.", ex);
        }
        catch (SecurityTokenException ex)
        {
            throw new InvalidOperationException("Apple id_token is invalid.", ex);
        }

        var email = jwt.TryGetPayloadValue<string>("email", out var emailClaim)
            ? emailClaim
            : null;

        // Apple may emit email_verified as boolean or string "true"/"false".
        var emailVerified = false;
        if (jwt.TryGetPayloadValue<bool>("email_verified", out var verifiedBool))
        {
            emailVerified = verifiedBool;
        }
        else if (jwt.TryGetPayloadValue<string>("email_verified", out var verifiedString))
        {
            emailVerified = string.Equals(verifiedString, "true", StringComparison.OrdinalIgnoreCase);
        }

        return Task.FromResult(new ExternalOAuthProfile
        {
            ProviderUserId = jwt.Subject ?? string.Empty,
            Email = email,
            EmailVerified = emailVerified && !string.IsNullOrWhiteSpace(email),
            // Given/family name are only posted once via form_post `user` JSON — not in id_token.
            DisplayName = null,
            AvatarUrl = null,
        });
    }

    private static async Task<ExternalOAuthProfile> FetchFacebookProfileAsync(
        HttpClient httpClient,
        ExternalOAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://graph.facebook.com/v21.0/me?fields=id,name,email,picture.type(large)");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;

        var email = root.TryGetProperty("email", out var emailNode) && emailNode.ValueKind == JsonValueKind.String
            ? emailNode.GetString()
            : null;

        string? avatarUrl = null;
        if (root.TryGetProperty("picture", out var pictureNode)
            && pictureNode.ValueKind == JsonValueKind.Object
            && pictureNode.TryGetProperty("data", out var pictureData)
            && pictureData.ValueKind == JsonValueKind.Object
            && pictureData.TryGetProperty("url", out var pictureUrl)
            && pictureUrl.ValueKind == JsonValueKind.String)
        {
            avatarUrl = pictureUrl.GetString();
        }

        // Graph may return email when granted, but has no OIDC email_verified attestation.
        return new ExternalOAuthProfile
        {
            ProviderUserId = root.GetProperty("id").GetString() ?? string.Empty,
            Email = email,
            EmailVerified = false,
            DisplayName = root.TryGetProperty("name", out var nameNode) && nameNode.ValueKind == JsonValueKind.String
                ? nameNode.GetString()
                : null,
            AvatarUrl = avatarUrl,
        };
    }
}
