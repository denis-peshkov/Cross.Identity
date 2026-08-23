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
        };

    public static bool TryGet(string provider, out ExternalOAuthProviderDefinition definition)
        => Definitions.TryGetValue(provider, out definition!);

    private static async Task<ExternalOAuthProfile> FetchGoogleProfileAsync(
        HttpClient httpClient,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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
        emailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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
        string accessToken,
        CancellationToken cancellationToken)
        => throw new NotSupportedException("Apple Sign In is not supported yet.");
}
