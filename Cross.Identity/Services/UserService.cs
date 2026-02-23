namespace Cross.Identity.Services;

/// <summary>
/// Базовая in-memory реализация <see cref="IUserService"/>.
/// Поддерживает создание, поиск по Email/UserName/Phone и проверку пароля (PBKDF2).
/// </summary>
internal sealed class UserService : IUserService
{
    private readonly IdentityContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly IPepperVaultProvider _pepperVault;
    private readonly IPasswordHasher _hasher;
    private readonly IPhoneNormalizer _phoneNormalizer;
    private readonly IHeadersContextAccessor _headersContextAccessor;

    public UserService(
        IdentityContext context,
        ILogger<UserService> logger,
        IPepperVaultProvider pepperVault,
        IPasswordHasher hasher,
        IPhoneNormalizer phoneNormalizer,
        IHeadersContextAccessor headersContextAccessor)
    {
        _context = context;
        _logger = logger;
        _pepperVault = pepperVault;
        _hasher = hasher;
        _phoneNormalizer = phoneNormalizer;
        _headersContextAccessor = headersContextAccessor;
    }

    /// <inheritdoc/>
    public async Task<string> GetUserIdByAsync(string selectorField, string selectorValue, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectorField);
        ArgumentNullException.ThrowIfNull(selectorValue);

        // приведение к ожидаемому имени
        string field = selectorField.ToLowerInvariant() switch
        {
            "email" => nameof(UserAccountEntity.NormalizedEmail),
            "username" => nameof(UserAccountEntity.NormalizedUserName),
            "phone" or "phonenumber" => nameof(UserAccountEntity.PhoneNumber),
            _ => throw new NotSupportedException($"Selector field '{selectorField}' is not supported.")
        };

        string value = selectorValue.ToLowerInvariant();

        return await _context.UsersAccounts
                   .AsNoTracking()
                   .Where(u => EF.Property<string>(u, field) == value)
                   .Select(u => u.Id.ToString())
                   .FirstOrDefaultAsync(cancellationToken)
               ?? throw new NotFoundException($"User with given {field} '{value}' not found");
    }

    public async Task<UserAccountEntity> GetUserByAsync(string selectorField, string selectorValue, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectorField);
        ArgumentNullException.ThrowIfNull(selectorValue);

        // приведение к ожидаемому имени
        string field = selectorField.ToLowerInvariant() switch
        {
            "id" => nameof(UserAccountEntity.Id), // не работает так как Guid != String
            "email" => nameof(UserAccountEntity.NormalizedEmail),
            "username" => nameof(UserAccountEntity.NormalizedUserName),
            "phone" or "phonenumber" => nameof(UserAccountEntity.PhoneNumber),
            _ => throw new NotSupportedException($"Selector field '{selectorField}' is not supported.")
        };

        string value = selectorValue.ToLowerInvariant();

        var userAccounts = _context.UsersAccounts
            .AsNoTracking();
        IQueryable<UserAccountEntity> userAccountsFiltered;
        if (field == nameof(UserAccountEntity.Id))
        {
            Guid.TryParse(selectorValue, out var id);
            userAccountsFiltered = userAccounts
                .Where(u => EF.Property<Guid>(u, field) == id);
        }
        else
        {
            userAccountsFiltered = userAccounts
                .Where(u => EF.Property<string>(u, field) == value);
        }
        var result = await userAccountsFiltered
                         .FirstOrDefaultAsync(cancellationToken)
                     ?? throw new NotFoundException($"User with given {field} '{value}' not found");

        return result;
    }

    /// <inheritdoc/>
    public async Task<string> CreateUserAsync(IDictionary<string, object?> map, CancellationToken cancellationToken)
    {
        // 1) Вытаскиваем поля
        map.TryGetValue("Email", out var emailRaw);
        map.TryGetValue("UserName", out var userNameRaw);
        map.TryGetValue("Phone", out var phoneRaw);
        map.TryGetValue("Password", out var passwordRaw);

        // 2) Нормализация
        var normalizedUserName = userNameRaw?.ToString()?.Trim().ToLowerInvariant();
        var normalizedEmail = emailRaw?.ToString()?.Trim().ToLowerInvariant();
        var normalizedPhone = _phoneNormalizer.NormalizeToE164(phoneRaw as string, _headersContextAccessor.LanguageCode);

        // 3) Уникальность
        if (normalizedUserName is not null
            && await _context.UsersAccounts.AnyAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken))
            throw new InvalidOperationException("UserName already exists.");
        if (normalizedEmail is not null
            && await _context.UsersAccounts.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken))
            throw new InvalidOperationException("Email already exists.");
        if (normalizedPhone is not null
            && await _context.UsersAccounts.AnyAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken))
            throw new InvalidOperationException("PhoneNumber already exists.");

        // 4) Хеш пароля (PHC) + текущая версия pepper
        var pepperVersion = _pepperVault.CurrentVersion;
        _pepperVault.TryGetCurrentVersion(out var pepper);
        var passwordPhc = _hasher.Hash(passwordRaw as string, pepper);

        // 5) Создание сущности
        var user = new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = emailRaw as string,
            NormalizedEmail = normalizedEmail,
            UserName = userNameRaw as string,
            NormalizedUserName = normalizedUserName,
            PasswordPhc = passwordPhc,
            PasswordPepperVersion = pepperVersion,
            EmailConfirmed = false,
            PhoneConfirmed = false,
            TwoFactorEnabled = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        };

        _context.UsersAccounts.Add(user);

        await _context.SaveChangesAsync(cancellationToken);

        return user.Id.ToString();
    }

    public async Task<bool> ValidatePasswordAsync(string selectorField, string selectorValue, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectorField);
        ArgumentNullException.ThrowIfNull(selectorValue);
        ArgumentNullException.ThrowIfNull(password);

        // 1) Определяем поле в БД и нормализуем значение селектора так же, как при создании пользователя
        string field = selectorField.ToLowerInvariant() switch
        {
            "email" => nameof(UserAccountEntity.NormalizedEmail),
            "username" => nameof(UserAccountEntity.NormalizedUserName),
            "phone" or "phonenumber" => nameof(UserAccountEntity.PhoneNumber),
            _ => throw new NotSupportedException($"Selector field '{selectorField}' is not supported.")
        };

        string? value = field switch
        {
            nameof(UserAccountEntity.NormalizedEmail) or nameof(UserAccountEntity.NormalizedUserName)
                => selectorValue.Trim().ToLowerInvariant(),
            nameof(UserAccountEntity.PhoneNumber)
                => _phoneNormalizer.NormalizeToE164(selectorValue, _headersContextAccessor.LanguageCode),
            _ => selectorValue
        };

        if (string.IsNullOrWhiteSpace(value))
            return false;

        // 2) Ищем пользователя (tracked, без AsNoTracking — чтобы при необходимости обновить хеш/версию перца)
        var user = await _context.UsersAccounts
            .FirstOrDefaultAsync(u => EF.Property<string>(u, field) == value, cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.PasswordPhc))
            return false;

        // 3) Достаём перец по версии, сохранённой у пользователя
        if (!_pepperVault.TryGet(user.PasswordPepperVersion, out var pepper) || pepper is null)
        {
            _logger.LogError(
                "Pepper with version {Version} not found for user {UserId}. Password validation failed.",
                user.PasswordPepperVersion,
                user.Id);
            return false;
        }

        // 4) Проверяем пароль
        var result = _hasher.Verify(password, user.PasswordPhc, pepper);
        if (result == PasswordVerificationEnum.Failed)
            return false;

        // 5) При необходимости делаем re-hash с текущими параметрами/версией перца
        var currentVersion = _pepperVault.CurrentVersion;
        var needRehash = result == PasswordVerificationEnum.SuccessRehashNeeded
                         || user.PasswordPepperVersion != currentVersion
                         || _hasher.NeedsRehash(user.PasswordPhc);

        if (needRehash && _pepperVault.TryGetCurrentVersion(out var currentPepper) && currentPepper is not null)
        {
            user.PasswordPhc = _hasher.Hash(password, currentPepper);
            user.PasswordPepperVersion = currentVersion;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Не ломаем успешную аутентификацию из‑за проблем с re-hash, только логируем
                _logger.LogError(ex, "Failed to re-hash password for user {UserId}", user.Id);
            }
        }

        return true;
    }

    public async Task<bool> ValidateCodeAsync(string selectorField, string selectorValue, string code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectorField);
        ArgumentNullException.ThrowIfNull(selectorValue);
        ArgumentNullException.ThrowIfNull(code);

        var user = await GetUserByAsync(selectorField, selectorValue, cancellationToken);

        // 1) Определяем поле в БД и нормализуем значение селектора так же, как при создании пользователя
        var field = selectorField.ToLowerInvariant() switch
        {
            "email" => nameof(UserAccountEntity.NormalizedEmail),
            "username" => nameof(UserAccountEntity.NormalizedUserName),
            "phone" or "phonenumber" => nameof(UserAccountEntity.PhoneNumber),
            _ => throw new NotSupportedException($"Selector field '{selectorField}' is not supported.")
        };

        var isValid = false;
        switch (field)
        {
            case nameof(UserAccountEntity.NormalizedEmail):
                var emailVerification = await _context.EmailVerifications.FirstOrDefaultAsync(x => x.UserAccountId == user.Id, cancellationToken);
                if (emailVerification != null) emailVerification.Attempts++;
                isValid = emailVerification != null
                          && emailVerification.TokenLength == code.Length
                          && emailVerification.TokenHash.SequenceEqual(CodeGeneratorHelper.GenerateHash(code))
                          && emailVerification.MaxAttempts >= emailVerification.Attempts;
                break;

            case nameof(UserAccountEntity.PhoneNumber):
                var phoneVerification = await _context.PhoneVerifications.FirstOrDefaultAsync(x => x.UserAccountId == user.Id, cancellationToken);
                if (phoneVerification != null) phoneVerification.Attempts++;
                isValid = phoneVerification != null
                          && phoneVerification.CodeLength == code.Length
                          && phoneVerification.CodeHash.SequenceEqual(CodeGeneratorHelper.GenerateHash(code))
                          && phoneVerification.MaxAttempts >= phoneVerification.Attempts;
                break;
        }
        await _context.SaveChangesAsync(cancellationToken);

        if (!isValid)
        {
            _logger.LogWarning(
                "Code validation failed for {Channel} channel, identity: {Identity}",
                field,
                selectorValue);
        }

        return isValid;
    }

    public Task SetPasswordAsync(string selectorField, string selectorValue, string newPassword, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
