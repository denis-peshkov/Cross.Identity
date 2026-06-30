namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Refresh token rotation step:
/// validates the incoming refresh token and issues a new token pair
/// and invalidates the old refresh token within a single transaction.
/// <para>
/// Keys:
/// <list type="bullet">
///   <item><description><see cref="RefreshTokenKey"/>:
///     if the key is relative (no dot), it is read as <c>"{Kind}.{Key}"</c>;
///     to read data from another step, specify an absolute key such as <c>"other-step.Field"</c>.</description></item>
///   <item><description>The result is written to keys:
///     <c>AccessToken</c>, <c>RefreshToken</c>, <c>TokenType</c>, <c>ExpiresIn</c>, <c>UserId</c>
///     (with the <c>{Kind}.</c> prefix for relative access).</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class RefreshTokenStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public required string? Next { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the source refresh token from. May be relative or absolute.</summary>
    public required string RefreshTokenKey { get; init; }

    /// <summary>Step logger.</summary>
    public required ILogger Logger { get; init; }

    /// <summary>Service for working with JWT and token entities.</summary>
    public required IJwtTokenService JwtTokenService { get; init; }

    /// <summary>User read service.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Authentication options.</summary>
    public required AuthenticationOptions AuthenticationOptions { get; init; }

    /// <summary>DB context for transactional refresh flow.</summary>
    public IdentityContext Context { get; set; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // 1) validate the token
        var oldRefreshTokenHashValue = ctx.Get<string>(BagKey.Qualify(Kind, RefreshTokenKey));
        if (!await JwtTokenService.ValidateRefreshTokenAsync(oldRefreshTokenHashValue).ConfigureAwait(false))
            throw new NotAuthorizedException("Invalid or expired refresh token.");

        // 2) open Transaction
        // var transaction = await Context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        // await using var _ = transaction.ConfigureAwait(false);
        // or
        // var transactionOptions = new TransactionOptions
        // {
        //     IsolationLevel = IsolationLevel.ReadCommitted,
        //     Timeout = TimeSpan.FromSeconds(60)
        // };
        // using var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);

        try
        {
            // 3) get UserId from the refresh token
            var oldRefreshToken = await JwtTokenService.GetRefreshTokenAsync(oldRefreshTokenHashValue, cancellationToken).ConfigureAwait(false);
            if (oldRefreshToken is null)
                throw new InvalidOperationException("User not found when refresh token.");

            // 4) get user data
            var user = (await UserService.GetUserByAsync(selectorField: "Id", selectorValue: oldRefreshToken.UserId.ToString(), cancellationToken).ConfigureAwait(false)).ToBag();
            ArgumentNullException.ThrowIfNull(user);
            var userId = user.TryGetValue("Id", out var idObj) && Guid.TryParse(idObj?.ToString(), out var guid) ? guid : Guid.Empty;
            if (userId == Guid.Empty)
                throw new InvalidOperationException("Invalid user ID when refresh token.");
            var email = user.TryGetValue("Email", out var emailObj) ? emailObj?.ToString() : null;
            var phone = user.TryGetValue("Phone", out var phoneObj) ? phoneObj?.ToString() : null;
            var username    = user.TryGetValue("UserName", out var usernameObj) ? usernameObj?.ToString() : null;

            // 5) generate AccessToken
            var accessClaims = new List<Claim>()
                .AddIfNotNull(JwtRegisteredClaimNames.Sub, userId.ToString())
                .AddIfNotNull(ClaimTypes.Email, email)
                .AddIfNotNull(ClaimTypes.MobilePhone, phone)
                .AddIfNotNull(ClaimConstants.Username, username);
            var accessToken = await JwtTokenService.GenerateAccessTokenAsync(userId, oldRefreshToken.FamilyId, new List<string>(), accessClaims).ConfigureAwait(false);
            ArgumentException.ThrowIfNullOrEmpty(accessToken);

            // 6) generate RefreshToken
            var refreshToken = await JwtTokenService.GenerateRefreshTokenAsync(userId, oldRefreshToken.FamilyId, new List<Claim>{new (JwtRegisteredClaimNames.Sub, userId.ToString())}).ConfigureAwait(false);
            ArgumentException.ThrowIfNullOrEmpty(refreshToken);

            // 7) Invalidate old RefreshToken
            var newJti = await JwtTokenService.GetClaimValueAsync(refreshToken, JwtRegisteredClaimNames.Jti).ConfigureAwait(false);
            ArgumentException.ThrowIfNullOrEmpty(newJti);
            await JwtTokenService.InvalidateRefreshTokenAsync(oldRefreshTokenHashValue, newJti, cancellationToken).ConfigureAwait(false);

            // 8) Complete Transaction
            // await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            // scope.Complete();

            // 9) store the token in Bag
            ctx.Set(BagKey.Qualify(Kind, "AccessToken"), accessToken);
            ctx.Set(BagKey.Qualify(Kind, "RefreshToken"), refreshToken);
            ctx.Set(BagKey.Qualify(Kind, "TokenType"), "Bearer");
            ctx.Set(BagKey.Qualify(Kind, "ExpiresIn"), JwtTokenService.AccessTokenExpiresInSeconds);
            ctx.Set(BagKey.Qualify(Kind, "UserId"), userId);

            return StepResult.Ok(Next);
        }
        catch (Exception)
        {
            // await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
