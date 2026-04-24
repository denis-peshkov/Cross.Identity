namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг выпуска JWT-токена через MediatR-команду приложения
/// <c>TokenCommand(string email, string password)</c>.
/// <para>
/// Ключи:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/> и <see cref="PasswordKey"/>:
///     если ключ относительный (без точки), читается как <c>"{Name}.{Key}"</c>;
///     чтобы читать данные из другого шага, укажи абсолютный ключ вида <c>"other-step.Field"</c>.</description></item>
///   <item><description><see cref="ResultKey"/> — если ключ относительный, записывается как <c>"{Name}.{ResultKey}"</c>.</description></item>
/// </list>
/// </para>
/// Ожидается, что результат обработчика содержит строковое свойство <c>AccessToken</c>
/// (или <c>Token</c>), либо сам является строкой. Значение будет записано в <see cref="Bag"/>.
/// </summary>
internal sealed class RefreshTokenStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public required string? Next { get; init; }

    /// <summary>Ключ в <see cref="Bag"/>, откуда взять код. Может быть относительным или абсолютным.</summary>
    public required string RefreshTokenKey { get; init; }

    public required ILogger Logger { get; init; }
    public required IJwtTokenService JwtTokenService { get; init; }
    public required IUserService UserService { get; init; }
    public required AuthenticationOptions AuthenticationOptions { get; init; }
    public IdentityContext Context { get; set; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // 1) валидируем токен
        var oldRefreshTokenHashValue = ctx.Get<string>(BagKey.Qualify(Kind, RefreshTokenKey));
        if (!await JwtTokenService.ValidateRefreshTokenAsync(oldRefreshTokenHashValue))
            throw new NotAuthorizedException("Invalid or expired refresh token.");

        // 2) open Transaction
        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
        // var transactionOptions = new TransactionOptions
        // {
        //     IsolationLevel = IsolationLevel.ReadCommitted,
        //     Timeout = TimeSpan.FromSeconds(60)
        // };
        // using var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);

        try
        {
            // 3) получаем UserId из рефреш токена
            var oldRefreshToken = await JwtTokenService.GetRefreshTokenAsync(oldRefreshTokenHashValue, cancellationToken);
            if (oldRefreshToken is null)
                throw new InvalidOperationException("User not found when refresh token.");

            // 4) получаем данные юзера
            var user = (await UserService.GetUserByAsync(selectorField: "Id", selectorValue: oldRefreshToken.UserId.ToString(), cancellationToken)).ToBag();
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
            var accessToken = await JwtTokenService.GenerateAccessTokenAsync(userId, oldRefreshToken.FamilyId, new List<string>(), accessClaims);
            ArgumentException.ThrowIfNullOrEmpty(accessToken);

            // 6) генерация RefreshToken
            var refreshToken = await JwtTokenService.GenerateRefreshTokenAsync(userId, oldRefreshToken.FamilyId, new List<Claim>{new (JwtRegisteredClaimNames.Sub, userId.ToString())});
            ArgumentException.ThrowIfNullOrEmpty(refreshToken);

            // 7) Invalidate old RefreshToken
            var newJti = await JwtTokenService.GetClaimValueAsync(refreshToken, JwtRegisteredClaimNames.Jti);
            ArgumentException.ThrowIfNullOrEmpty(newJti);
            await JwtTokenService.InvalidateRefreshTokenAsync(oldRefreshTokenHashValue, newJti, cancellationToken);

            // 8) Complete Transaction
            await transaction.CommitAsync(cancellationToken);
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
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
