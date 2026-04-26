namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг ротации refresh-токена:
/// валидирует входной refresh, выпускает новую пару токенов
/// и инвалидирует старый refresh в рамках одной транзакции.
/// <para>
/// Ключи:
/// <list type="bullet">
///   <item><description><see cref="RefreshTokenKey"/>:
///     если ключ относительный (без точки), читается как <c>"{Kind}.{Key}"</c>;
///     чтобы читать данные из другого шага, укажи абсолютный ключ вида <c>"other-step.Field"</c>.</description></item>
///   <item><description>Результат пишется в ключи:
///     <c>AccessToken</c>, <c>RefreshToken</c>, <c>TokenType</c>, <c>ExpiresIn</c>, <c>UserId</c>
///     (с префиксом <c>{Kind}.</c> для относительного доступа).</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class RefreshTokenStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public required string? Next { get; init; }

    /// <summary>Ключ в <see cref="Bag"/>, откуда взять исходный refresh-токен. Может быть относительным или абсолютным.</summary>
    public required string RefreshTokenKey { get; init; }

    /// <summary>Логгер шага.</summary>
    public required ILogger Logger { get; init; }

    /// <summary>Сервис работы с JWT и сущностями токенов.</summary>
    public required IJwtTokenService JwtTokenService { get; init; }

    /// <summary>Сервис чтения пользователя.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Опции аутентификации.</summary>
    public required AuthenticationOptions AuthenticationOptions { get; init; }

    /// <summary>Контекст БД для транзакционного refresh-flow.</summary>
    public IdentityContext Context { get; set; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // 1) валидируем токен
        var oldRefreshTokenHashValue = ctx.Get<string>(BagKey.Qualify(Kind, RefreshTokenKey));
        if (!await JwtTokenService.ValidateRefreshTokenAsync(oldRefreshTokenHashValue).ConfigureAwait(false))
            throw new NotAuthorizedException("Invalid or expired refresh token.");

        // 2) open Transaction
        var transaction = await Context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using var _ = transaction.ConfigureAwait(false);
        // var transactionOptions = new TransactionOptions
        // {
        //     IsolationLevel = IsolationLevel.ReadCommitted,
        //     Timeout = TimeSpan.FromSeconds(60)
        // };
        // using var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);

        try
        {
            // 3) получаем UserId из рефреш токена
            var oldRefreshToken = await JwtTokenService.GetRefreshTokenAsync(oldRefreshTokenHashValue, cancellationToken).ConfigureAwait(false);
            if (oldRefreshToken is null)
                throw new InvalidOperationException("User not found when refresh token.");

            // 4) получаем данные юзера
            var user = (await UserService.GetUserByAsync(selectorField: "Id", selectorValue: oldRefreshToken.UserId.ToString(), cancellationToken).ConfigureAwait(false)).ToBag();
            ArgumentNullException.ThrowIfNull(user);
            var userId = user.TryGetValue("Id", out var idObj) && Guid.TryParse(idObj?.ToString(), out var guid) ? guid : Guid.Empty;
            if (userId == Guid.Empty)
                throw new InvalidOperationException("Invalid user ID when refresh token.");
            var email = user.TryGetValue("Email", out var emailObj) ? emailObj?.ToString() : null;
            var phone = user.TryGetValue("Phone", out var phoneObj) ? phoneObj?.ToString() : null;
            var username    = user.TryGetValue("UserName", out var usernameObj) ? usernameObj?.ToString() : null;

            // 5) генерация AccessToken
            var accessClaims = new List<Claim>()
                .AddIfNotNull(JwtRegisteredClaimNames.Sub, userId.ToString())
                .AddIfNotNull(ClaimTypes.Email, email)
                .AddIfNotNull(ClaimTypes.MobilePhone, phone)
                .AddIfNotNull(ClaimConstants.Username, username);
            var accessToken = await JwtTokenService.GenerateAccessTokenAsync(userId, oldRefreshToken.FamilyId, new List<string>(), accessClaims).ConfigureAwait(false);
            ArgumentException.ThrowIfNullOrEmpty(accessToken);

            // 6) генерация RefreshToken
            var refreshToken = await JwtTokenService.GenerateRefreshTokenAsync(userId, oldRefreshToken.FamilyId, new List<Claim>{new (JwtRegisteredClaimNames.Sub, userId.ToString())}).ConfigureAwait(false);
            ArgumentException.ThrowIfNullOrEmpty(refreshToken);

            // 7) Invalidate old RefreshToken
            var newJti = await JwtTokenService.GetClaimValueAsync(refreshToken, JwtRegisteredClaimNames.Jti).ConfigureAwait(false);
            ArgumentException.ThrowIfNullOrEmpty(newJti);
            await JwtTokenService.InvalidateRefreshTokenAsync(oldRefreshTokenHashValue, newJti, cancellationToken).ConfigureAwait(false);

            // 8) Complete Transaction
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            // scope.Complete();

            // 9) сохраняем токен в Bag
            ctx.Set(BagKey.Qualify(Kind, "AccessToken"), accessToken);
            ctx.Set(BagKey.Qualify(Kind, "RefreshToken"), refreshToken);
            ctx.Set(BagKey.Qualify(Kind, "TokenType"), "Bearer");
            ctx.Set(BagKey.Qualify(Kind, "ExpiresIn"), JwtTokenService.AccessTokenExpiresInSeconds);
            ctx.Set(BagKey.Qualify(Kind, "UserId"), userId);

            return StepResult.Ok(Next);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
