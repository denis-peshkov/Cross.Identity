namespace Cross.Identity.Services;

/// <summary>
/// Basic in-memory implementation of <see cref="IUserService"/>.
/// Supports creation, lookup by Email/UserName/Phone, and password verification (PBKDF2).
/// </summary>
internal sealed class UserService : IUserService
{
    private readonly IdentityContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly IPepperVaultProvider _pepperVault;
    private readonly IPasswordHasher _hasher;
    private readonly IPhoneNormalizer _phoneNormalizer;
    private readonly IHeadersContextAccessor _headersContextAccessor;
    private readonly IJwtTokenService _jwtTokenService;

    public UserService(
        IdentityContext context,
        ILogger<UserService> logger,
        IPepperVaultProvider pepperVault,
        IPasswordHasher hasher,
        IPhoneNormalizer phoneNormalizer,
        IHeadersContextAccessor headersContextAccessor,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _logger = logger;
        _pepperVault = pepperVault;
        _hasher = hasher;
        _phoneNormalizer = phoneNormalizer;
        _headersContextAccessor = headersContextAccessor;
        _jwtTokenService = jwtTokenService;
    }

    /// <inheritdoc/>
    public async Task<string> GetUserIdByAsync(string selectorField, string selectorValue, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectorField);
        ArgumentNullException.ThrowIfNull(selectorValue);

        var field = ResolveSelectorField(selectorField);
        var user = await FindTrackedUserBySelectorAsync(field, selectorValue, cancellationToken).ConfigureAwait(false)
                   ?? throw new NotFoundException($"User with given {field} '{selectorValue}' not found");

        return user.Id.ToString();
    }

    public async Task<UserAccountEntity> GetUserByAsync(string selectorField, string selectorValue, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectorField);
        ArgumentNullException.ThrowIfNull(selectorValue);

        var field = ResolveSelectorField(selectorField);
        var userAccounts = _context.UsersAccounts.AsNoTracking();

        IQueryable<UserAccountEntity> userAccountsFiltered;
        var displayValue = selectorValue.ToLowerInvariant();
        if (field == nameof(UserAccountEntity.Id))
        {
            if (!TryParseUserId(selectorValue, out var id))
                throw new NotFoundException($"User with given {field} '{selectorValue}' not found");

            userAccountsFiltered = userAccounts.Where(u => u.Id == id);
        }
        else
        {
            userAccountsFiltered = userAccounts.Where(u => EF.Property<string>(u, field) == displayValue);
        }

        return await userAccountsFiltered
                   .FirstOrDefaultAsync(cancellationToken)
                   .ConfigureAwait(false)
               ?? throw new NotFoundException($"User with given {field} '{displayValue}' not found");
    }

    /// <inheritdoc/>
    public async Task<string> CreateUserAsync(IDictionary<string, object?> map, CancellationToken cancellationToken)
    {
        // 1) Extract fields
        map.TryGetValue("Email", out var emailRaw);
        map.TryGetValue("UserName", out var userNameRaw);
        map.TryGetValue("Phone", out var phoneRaw);
        map.TryGetValue("Password", out var passwordRaw);

        // 2) Normalization
        var normalizedUserName = userNameRaw?.ToString()?.Trim().ToLowerInvariant();
        var normalizedEmail = emailRaw?.ToString()?.Trim().ToLowerInvariant();
        var normalizedPhone = phoneRaw is string phone
            ? _phoneNormalizer.NormalizeToE164(phone, _headersContextAccessor.LanguageCode!) // тут баг, так как что бы правильно отформатировать надо знать не текущий язык а страну номера телефона
            : null;

        // 3) Uniqueness
        if (normalizedUserName is not null
            && await _context.UsersAccounts.AnyAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("UserName already exists.");
        if (normalizedEmail is not null
            && await _context.UsersAccounts.AnyAsync(u => u.Email == normalizedEmail, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Email already exists.");
        if (normalizedPhone is not null
            && await _context.UsersAccounts.AnyAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("PhoneNumber already exists.");

        // 4) Password hash (PHC) + current pepper version
        var pepperVersion = _pepperVault.CurrentVersion;
        _pepperVault.TryGetCurrentValue(out var pepper);
        ArgumentNullException.ThrowIfNull(pepper);
        var passwordPhc = passwordRaw is string password
            ? _hasher.Hash(password, pepper)
            : null;

        // 5) Create entity
        var user = new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
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

        await _context.UsersAccounts.AddAsync(user, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return user.Id.ToString();
    }

    public async Task<bool> ValidatePasswordAsync(string selectorField, string selectorValue, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectorField);
        ArgumentNullException.ThrowIfNull(selectorValue);
        ArgumentNullException.ThrowIfNull(password);

        // 1) Resolve the DB field and normalize the selector value the same way as when creating a user
        var field = ResolveSelectorField(selectorField);
        var user = await FindTrackedUserBySelectorAsync(field, selectorValue, cancellationToken).ConfigureAwait(false);

        if (user is null || string.IsNullOrEmpty(user.PasswordPhc))
            return false;

        // 3) Get pepper by the version stored on the user
        if (!_pepperVault.TryGetValue(user.PasswordPepperVersion, out var pepper) || pepper is null)
        {
            _logger.LogError(
                "Pepper with version {Version} not found for user {UserId}. Password validation failed.",
                user.PasswordPepperVersion,
                user.Id);
            return false;
        }

        // 4) Verify password
        var result = _hasher.Verify(password, user.PasswordPhc, pepper);
        if (result == PasswordVerificationEnum.Failed)
            return false;

        // 5) Re-hash with current parameters/pepper version if needed
        var currentVersion = _pepperVault.CurrentVersion;
        var needRehash = result == PasswordVerificationEnum.SuccessRehashNeeded
                         || user.PasswordPepperVersion != currentVersion
                         || _hasher.NeedsRehash(user.PasswordPhc);

        if (needRehash && _pepperVault.TryGetCurrentValue(out var currentPepper) && currentPepper is not null)
        {
            user.PasswordPhc = _hasher.Hash(password, currentPepper);
            user.PasswordPepperVersion = currentVersion;

            try
            {
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Do not fail successful authentication due to re-hash issues; log only
                _logger.LogError(ex, "Failed to re-hash password for user {UserId}", user.Id);
            }
        }

        return true;
    }

    public async Task<bool> ValidateCodeAsync(string selectorField, string selectorValue, string code, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectorField);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectorValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        code = code.Trim();

        var user = await GetUserByAsync(selectorField, selectorValue.Trim(), cancellationToken).ConfigureAwait(false);

        // 1) Resolve the DB field and normalize the selector value the same way as when creating a user
        var field = ResolveSelectorField(selectorField);

        var isValid = false;
        var now = DateTime.UtcNow;
        switch (field)
        {
            case nameof(UserAccountEntity.Email):
                isValid = await TryValidateEmailCodeAsync(user.Id, code, now, cancellationToken).ConfigureAwait(false);
                break;

            case nameof(UserAccountEntity.PhoneNumber):
                isValid = await TryValidatePhoneCodeAsync(user.Id, code, now, cancellationToken).ConfigureAwait(false);
                break;
        }

        if (isValid)
        {
            var account = await FindTrackedUserBySelectorAsync(field, selectorValue, cancellationToken).ConfigureAwait(false);
            if (account != null)
            {
                if (field == nameof(UserAccountEntity.Email))
                    account.EmailConfirmed = true;
                else
                    account.PhoneConfirmed = true;
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!isValid)
        {
            _logger.LogWarning(
                "Code validation failed for {Channel} channel, identity: {Identity}",
                field,
                selectorValue);
        }

        return isValid;
    }

    public async Task SetPasswordAsync(string selectorField, string selectorValue, string newPassword, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectorField);
        ArgumentNullException.ThrowIfNull(selectorValue);
        ArgumentNullException.ThrowIfNull(newPassword);

        var field = ResolveSelectorField(selectorField);
        var user = await FindTrackedUserBySelectorAsync(field, selectorValue, cancellationToken).ConfigureAwait(false)
                   ?? throw new NotFoundException($"User with given {field} '{selectorValue}' not found");

        if (!_pepperVault.TryGetCurrentValue(out var pepper) || string.IsNullOrWhiteSpace(pepper))
        {
            throw new InvalidOperationException("Current pepper version is not available.");
        }

        user.PasswordPhc = _hasher.Hash(newPassword, pepper);
        user.PasswordPepperVersion = _pepperVault.CurrentVersion;
        // Invalidate existing sessions: stamp rotation + revoke all tokens (PASSWORD_CHANGED).
        user.SecurityStamp = Guid.NewGuid();

        await _jwtTokenService
            .RevokeAllTokensForUserAsync(user.Id, RefreshTokenRevokeReason.PASSWORD_CHANGED, cancellationToken)
            .ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveSelectorField(string selectorField)
    {
        return selectorField.ToLowerInvariant() switch
        {
            "id" => nameof(UserAccountEntity.Id),
            "email" => nameof(UserAccountEntity.Email),
            "username" => nameof(UserAccountEntity.NormalizedUserName),
            "phone" or "phonenumber" => nameof(UserAccountEntity.PhoneNumber),
            _ => throw new NotSupportedException($"Selector field '{selectorField}' is not supported."),
        };
    }

    private static bool TryParseUserId(string selectorValue, out Guid id)
    {
        return Guid.TryParse(selectorValue.Trim(), out id) && id != Guid.Empty;
    }

    private async Task<UserAccountEntity?> FindTrackedUserBySelectorAsync(string field, string selectorValue, CancellationToken cancellationToken)
    {
        if (field == nameof(UserAccountEntity.Id))
        {
            if (!TryParseUserId(selectorValue, out var id))
                return null;

            return await _context.UsersAccounts
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var value = field switch
        {
            nameof(UserAccountEntity.Email) or nameof(UserAccountEntity.NormalizedUserName)
                => selectorValue.Trim().ToLowerInvariant(),
            nameof(UserAccountEntity.PhoneNumber)
                /**
                 * Кратко: в NormalizeToE164 нужен ISO region (страна), не язык.
                 * Что имеют в виду | Что передавать                          | Country code | Пример
                 * EN               |  не регион — язык. Обычно "US" или "GB" | +1 / +44     | зависит от страны
                 * RU               | "RU"                                    | +7           | 9123456789 → +79123456789
                 * UA               | "UA"                                    | +380         | 501234567  → +380501234567
                 * UK               | "GB" (не "UK")                          | +44          | 7911123456 → +447911123456
                 * RO               | "RO"                                    | +40          | 722123456  → +40722123456
                 */
                => _phoneNormalizer.NormalizeToE164(selectorValue, _headersContextAccessor.LanguageCode), // тут баг, так как что бы правильно отформатировать надо знать не текущий язык а страну номера телефона
            _ => selectorValue,
        };

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return await _context.UsersAccounts
            .Where(u => EF.Property<string>(u, field) == value)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<bool> TryValidateEmailCodeAsync(
        Guid userId,
        string code,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var verification = await _context.EmailVerifications
            .Where(x => x.UserAccountId == userId && x.ExpiresAt >= now && x.UsedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (verification is null || verification.Attempts >= verification.MaxAttempts)
            return false;

        var matches = verification.TokenLength == code.Length
                      && verification.TokenHash.SequenceEqual(CodeGeneratorHelper.GenerateHash(code));

        if (!matches)
        {
            verification.Attempts++;
            return false;
        }

        verification.UsedAt = now;

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private async Task<bool> TryValidatePhoneCodeAsync(
        Guid userId,
        string code,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var verification = await _context.PhoneVerifications
            .Where(x => x.UserAccountId == userId && x.ExpiresAt >= now && x.UsedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (verification is null || verification.Attempts >= verification.MaxAttempts)
            return false;

        var matches = verification.CodeLength == code.Length
                      && verification.CodeHash.SequenceEqual(CodeGeneratorHelper.GenerateHash(code));

        if (!matches)
        {
            verification.Attempts++;
            return false;
        }

        verification.UsedAt = now;

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
