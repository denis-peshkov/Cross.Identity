namespace Cross.Identity.Services;

/// <summary>
/// Basic in-memory implementation of <see cref="IUserService"/>.
/// Supports creation, lookup by Email/UserName/PhoneNumber, and password verification (PBKDF2).
/// </summary>
internal sealed class UserService : IUserService
{
    private readonly IdentityContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly IPepperVaultProvider _pepperVault;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICommunicationEndpointService _communicationEndpoints;
    private readonly AuthenticationOptions _options;

    public UserService(
        IdentityContext context,
        ILogger<UserService> logger,
        IPepperVaultProvider pepperVault,
        IPasswordHasher hasher,
        IJwtTokenService jwtTokenService,
        ICommunicationEndpointService communicationEndpoints,
        IOptionsSnapshot<AuthenticationOptions> options)
    {
        _context = context;
        _logger = logger;
        _pepperVault = pepperVault;
        _hasher = hasher;
        _jwtTokenService = jwtTokenService;
        _communicationEndpoints = communicationEndpoints;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<Guid?> GetUserAccountIdByAsync(string selectorField, string selectorValue, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectorField);
        ArgumentNullException.ThrowIfNull(selectorValue);

        var field = ResolveSelectorField(selectorField);
        var user = await FindTrackedUserBySelectorAsync(field, selectorValue, cancellationToken).ConfigureAwait(false);
        return user?.Id;
    }

    public async Task<UserAccountEntity> GetUserByAsync(string selectorField, string selectorValue, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectorField);
        ArgumentNullException.ThrowIfNull(selectorValue);

        var field = ResolveSelectorField(selectorField);
        var userAccounts = _context.UsersAccounts.AsNoTracking();

        IQueryable<UserAccountEntity> userAccountsFiltered;
        if (field == nameof(UserAccountEntity.Id))
        {
            if (!TryParseUserAccountId(selectorValue, out var id))
                throw new NotFoundException($"User with given {field} '{selectorValue}' not found");

            userAccountsFiltered = userAccounts.Where(u => u.Id == id);
        }
        else
        {
            var displayValue = NormalizeSelectorValue(field, selectorValue)
                ?? throw new NotFoundException($"User with given {field} '{selectorValue}' not found");
            userAccountsFiltered = userAccounts.Where(u => EF.Property<string>(u, field) == displayValue);
        }

        return await PreferVerifiedContact(userAccountsFiltered, field)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false)
            ?? throw new NotFoundException($"User with given {field} '{selectorValue}' not found");
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateUserAsync(IDictionary<string, object?> map, CancellationToken cancellationToken)
    {
        // 1) Extract fields
        map.TryGetValue("Email", out var emailRaw);
        map.TryGetValue("UserName", out var userNameRaw);
        map.TryGetValue("PhoneNumber", out var phoneRaw);
        map.TryGetValue("Password", out var passwordRaw);

        // 2) Normalization
        var normalizedUserName = userNameRaw?.ToString()?.Trim().ToLowerInvariant();
        var normalizedEmail = emailRaw?.ToString()?.Trim().ToLowerInvariant();
        // PhoneNumber is expected already E.164 (collectForm / PhoneE164 at the form boundary).
        string? normalizedPhone = null;
        if (phoneRaw is string phone && !string.IsNullOrWhiteSpace(phone))
            normalizedPhone = phone;

        // 3) Uniqueness
        if (normalizedUserName is not null
            && await _context.UsersAccounts.AnyAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("UserName already exists.");
        if (normalizedEmail is not null
            && await _context.UsersAccounts.AnyAsync(
                u => u.Email == normalizedEmail && u.EmailVerified,
                cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Email already exists.");
        if (normalizedPhone is not null
            && await _context.UsersAccounts.AnyAsync(
                u => u.PhoneNumber == normalizedPhone && u.PhoneNumberVerified,
                cancellationToken).ConfigureAwait(false))
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
            PhoneNumber = normalizedPhone,
            UserName = userNameRaw as string,
            NormalizedUserName = normalizedUserName,
            PasswordPhc = passwordPhc,
            PasswordPepperVersion = pepperVersion,
            EmailVerified = false,
            PhoneNumberVerified = false,
            TwoFactorEnabled = false,
            LockoutEnabled = _options.Lockout.LockoutEnabled,
            AccessFailedCount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        };

        await _context.UsersAccounts.AddAsync(user, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return user.Id;
    }

    public async Task<bool> ValidatePasswordAsync(string selectorField, string selectorValue, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectorField);
        ArgumentNullException.ThrowIfNull(selectorValue);
        ArgumentNullException.ThrowIfNull(password);

        // 1) Resolve the DB field and normalize the selector value the same way as when creating a user
        var field = ResolveSelectorField(selectorField);
        var user = await FindTrackedUserBySelectorAsync(field, selectorValue, cancellationToken).ConfigureAwait(false);

        if (user is null || string.IsNullOrEmpty(user.PasswordPhc) || !user.IsActive)
            return false;

        var now = DateTimeOffset.UtcNow;
        if (UserAccountLockout.IsLockedOut(user, now))
        {
            _logger.LogWarning("Password validation rejected for locked-out user {UserAccountId}", user.Id);
            return false;
        }

        // 3) Get pepper by the version stored on the user
        if (!_pepperVault.TryGetValue(user.PasswordPepperVersion, out var pepper) || pepper is null)
        {
            _logger.LogError(
                "Pepper with version {Version} not found for user {UserAccountId}. Password validation failed.",
                user.PasswordPepperVersion,
                user.Id);
            return false;
        }

        // 4) Verify password
        var result = _hasher.Verify(password, user.PasswordPhc, pepper);
        if (result == PasswordVerificationEnum.Failed)
        {
            UserAccountLockout.RecordFailedAccess(user, _options.Lockout, now);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        UserAccountLockout.Reset(user);

        // 5) Re-hash with current parameters/pepper version if needed
        var currentVersion = _pepperVault.CurrentVersion;
        var needRehash = result == PasswordVerificationEnum.SuccessRehashNeeded
                         || user.PasswordPepperVersion != currentVersion
                         || _hasher.NeedsRehash(user.PasswordPhc);

        if (needRehash && _pepperVault.TryGetCurrentValue(out var currentPepper) && currentPepper is not null)
        {
            user.PasswordPhc = _hasher.Hash(password, currentPepper);
            user.PasswordPepperVersion = currentVersion;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (needRehash)
            {
                // Do not fail successful authentication due to re-hash issues; log only
                _logger.LogError(ex, "Failed to re-hash password for user {UserAccountId}", user.Id);
            }
            else
            {
                throw;
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

        var field = ResolveSelectorField(selectorField);
        var user = await FindTrackedUserBySelectorAsync(field, selectorValue.Trim(), cancellationToken).ConfigureAwait(false)
                   ?? throw new NotFoundException($"User with given {field} '{selectorValue}' not found");

        if (!user.IsActive)
        {
            _logger.LogWarning(
                "Code validation rejected for disabled account, {Channel} channel, identity: {Identity}",
                field,
                selectorValue);
            return false;
        }

        var lockoutNow = DateTimeOffset.UtcNow;
        if (UserAccountLockout.IsLockedOut(user, lockoutNow))
        {
            _logger.LogWarning("Code validation rejected for locked-out user {UserAccountId}", user.Id);
            return false;
        }

        // Same channel as SendCodeStep / VerifyCodeStep: preferred → email fallback (not selector field).
        var otpTarget = await _communicationEndpoints
            .ResolveOtpTargetAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var isValid = otpTarget.Channel == ChannelEnum.Email
            ? await TryValidateEmailCodeAsync(user.Id, code, now, cancellationToken).ConfigureAwait(false)
            : await TryValidatePhoneCodeAsync(user.Id, code, now, cancellationToken).ConfigureAwait(false);

        if (!isValid)
        {
            UserAccountLockout.RecordFailedAccess(user, _options.Lockout, lockoutNow);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(
                "Code validation failed for {Channel} channel, identity: {Identity}",
                field,
                selectorValue);
            return false;
        }

        UserAccountLockout.Reset(user);

        await ApplyVerifiedContactFromOtpTargetAsync(user, otpTarget, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _communicationEndpoints.SyncAccountContactsAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task ApplyVerifiedContactFromOtpTargetAsync(
        UserAccountEntity user,
        DeliveryTarget otpTarget,
        CancellationToken cancellationToken)
    {
        if (otpTarget.Channel == ChannelEnum.Email)
        {
            var normalizedEmail = ChannelEnum.Email.NormalizeAddress(otpTarget.Address);
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                user.Email = normalizedEmail;
            }

            if (!string.Equals(
                    ChannelEnum.Email.NormalizeAddress(user.Email!),
                    normalizedEmail,
                    StringComparison.Ordinal))
            {
                return;
            }

            await UserAccountGuard.EnsureNoOtherVerifiedEmailAsync(
                _context,
                user.Id,
                normalizedEmail,
                cancellationToken).ConfigureAwait(false);
            user.EmailVerified = true;
            return;
        }

        if (otpTarget.Channel == ChannelEnum.Sms)
        {
            var normalizedPhone = ChannelEnum.Sms.NormalizeAddress(otpTarget.Address);
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                user.PhoneNumber = normalizedPhone;
            }

            if (!string.Equals(
                    ChannelEnum.Sms.NormalizeAddress(user.PhoneNumber!),
                    normalizedPhone,
                    StringComparison.Ordinal))
            {
                return;
            }

            await UserAccountGuard.EnsureNoOtherVerifiedPhoneAsync(
                _context,
                user.Id,
                normalizedPhone,
                cancellationToken).ConfigureAwait(false);
            user.PhoneNumberVerified = true;
        }
    }

    public async Task SetPasswordAsync(
        string selectorField,
        string selectorValue,
        string newPassword,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken)
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
        UserAccountLockout.Reset(user);
        // Invalidate existing sessions: stamp rotation + revoke all tokens (PASSWORD_CHANGED).
        user.SecurityStamp = Guid.NewGuid();

        await _jwtTokenService
            .RevokeAllTokensForUserAsync(user.Id, RefreshTokenRevokedReason.PASSWORD_CHANGED, hostSuppliedClientContext, cancellationToken)
            .ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveSelectorField(string selectorField)
    {
        return selectorField.ToLowerInvariant() switch
        {
            "id" or "userid" or "useraccountid" => nameof(UserAccountEntity.Id),
            "email" => nameof(UserAccountEntity.Email),
            "username" => nameof(UserAccountEntity.NormalizedUserName),
            "phone" or "phonenumber" => nameof(UserAccountEntity.PhoneNumber),
            _ => throw new NotSupportedException($"Selector field '{selectorField}' is not supported."),
        };
    }

    private static bool TryParseUserAccountId(string selectorValue, out Guid id)
    {
        return Guid.TryParse(selectorValue.Trim(), out id) && id != Guid.Empty;
    }

    private async Task<UserAccountEntity?> FindTrackedUserBySelectorAsync(
        string field,
        string selectorValue,
        CancellationToken cancellationToken)
    {
        if (field == nameof(UserAccountEntity.Id))
        {
            if (!TryParseUserAccountId(selectorValue, out var id))
                return null;

            return await _context.UsersAccounts
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var value = NormalizeSelectorValue(field, selectorValue);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return await PreferVerifiedContact(
                _context.UsersAccounts.Where(u => EF.Property<string>(u, field) == value),
                field)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// When several rows share Email/Phone (unique only among verified), prefer the verified one.
    /// </summary>
    private static IQueryable<UserAccountEntity> PreferVerifiedContact(
        IQueryable<UserAccountEntity> query,
        string field)
    {
        return field switch
        {
            nameof(UserAccountEntity.Email) => query.OrderByDescending(u => u.EmailVerified),
            nameof(UserAccountEntity.PhoneNumber) => query.OrderByDescending(u => u.PhoneNumberVerified),
            _ => query,
        };
    }

    private string? NormalizeSelectorValue(string field, string selectorValue)
    {
        return field switch
        {
            nameof(UserAccountEntity.Email) or nameof(UserAccountEntity.NormalizedUserName)
                => selectorValue.Trim().ToLowerInvariant(),
            nameof(UserAccountEntity.PhoneNumber)
                => selectorValue,
            _ => selectorValue,
        };
    }

    private async Task<bool> TryValidateEmailCodeAsync(
        Guid userAccountId,
        string code,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var verification = await _context.EmailVerifications
            .Where(x => x.UserAccountId == userAccountId && x.ExpiresAt >= now && x.UsedAt == null)
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
        Guid userAccountId,
        string code,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var verification = await _context.PhoneVerifications
            .Where(x => x.UserAccountId == userAccountId && x.ExpiresAt >= now && x.UsedAt == null)
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
