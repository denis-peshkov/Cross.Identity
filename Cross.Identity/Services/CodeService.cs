namespace Cross.Identity.Services;

/// <summary>
/// EF Core-based OTP code service for email/SMS delivery.
/// </summary>
internal sealed class CodeService : ICodeService
{
    private readonly IdentityContext _context;
    private readonly ILogger<CodeService> _logger;
    private readonly IEmailSenderService _email;
    private readonly ISmsSenderService _sms;
    private readonly IConfiguration _configuration;
    private readonly AuthenticationOptions _options;

    public CodeService(
        IdentityContext context,
        ILogger<CodeService> logger,
        IEmailSenderService email,
        ISmsSenderService sms,
        IConfiguration configuration,
        IOptionsSnapshot<AuthenticationOptions> options)
    {
        _context = context;
        _logger = logger;
        _email = email;
        _sms = sms;
        _configuration = configuration;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task SendAsync(NotificationMessage msg, string code, Guid userAccountId, TimeSpan ttl, CancellationToken cancellationToken)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException("Invalid user id", nameof(userAccountId));
        }

        var destination = msg.Destination.Trim();

        var developerMode = _configuration.GetValue<bool>("Authentication:DeveloperMode");
        var now = DateTime.UtcNow;

        if (!msg.Channel.SupportsOtp())
        {
            throw new NotSupportedException($"OTP send via {msg.Channel} is not supported. Use Email or Sms; messenger delivery is not implemented yet.");
        }

        var normalizedDestination = msg.Channel.NormalizeAddress(destination);

        await EnsureOtpSendAllowedAsync(userAccountId, msg.Channel, normalizedDestination, now, cancellationToken)
            .ConfigureAwait(false);

        switch (msg.Channel)
        {
            case ChannelEnum.Email:
                if (!developerMode)
                {
                    await _email.SendAsync("", destination, msg.Subject, msg.TextBody, msg.HtmlBody, cancellationToken).ConfigureAwait(false);
                }

                await SupersedeActiveEmailVerificationsAsync(userAccountId, normalizedDestination, now, cancellationToken).ConfigureAwait(false);

                var emailEntity = new EmailVerificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserAccountId = userAccountId,
                    UserAccount = null!,
                    Email = normalizedDestination,
                    TokenHash = CodeGeneratorHelper.GenerateHash(code),
                    TokenLength = (byte)code.Length,
                    Attempts = 0,
                    MaxAttempts = 3,
                    ExpiresAt = DateTimeOffset.UtcNow.Add(ttl).UtcDateTime,
                    CreatedAt = now,
                };
                await _context.EmailVerifications.AddAsync(emailEntity, cancellationToken).ConfigureAwait(false);
                break;

            case ChannelEnum.Sms:
                if (!developerMode)
                {
                    await _sms.SendAsync(destination, msg.TextBody, cancellationToken).ConfigureAwait(false);
                }

                await SupersedeActivePhoneVerificationsAsync(userAccountId, normalizedDestination, now, cancellationToken).ConfigureAwait(false);

                var phoneEntity = new PhoneVerificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserAccountId = userAccountId,
                    UserAccount = null!,
                    PhoneNumber = normalizedDestination,
                    CodeHash = CodeGeneratorHelper.GenerateHash(code),
                    CodeLength = (byte)code.Length,
                    Attempts = 0,
                    MaxAttempts = 3,
                    ExpiresAt = DateTimeOffset.UtcNow.Add(ttl).UtcDateTime,
                    CreatedAt = now,
                };
                await _context.PhoneVerifications.AddAsync(phoneEntity, cancellationToken).ConfigureAwait(false);
                break;

            default:
                throw new NotSupportedException(
                    $"OTP send via {msg.Channel} is not supported. Use Email or Sms; messenger delivery is not implemented yet.");
        }
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("[Notifier] {Channel} → {Dest}: {Subject} | {Body}",
            msg.Channel, msg.Destination, msg.Subject, msg.TextBody);
    }

    /// <inheritdoc />
    public async Task<bool> VerifyAsync(
        Guid userAccountId,
        ChannelEnum channel,
        string identity,
        string code,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(code);
        if (userAccountId == Guid.Empty)
        {
            return false;
        }

        code = code.Trim();

        var now = DateTime.UtcNow;

        var normalizedIdentity = channel switch
        {
            ChannelEnum.Email or ChannelEnum.Sms => channel.NormalizeAddress(identity),
            _ => identity.Trim(),
        };

        switch (channel)
        {
            case ChannelEnum.Email:
                return await VerifyEmailAsync(userAccountId, normalizedIdentity, code, now, cancellationToken).ConfigureAwait(false);
            case ChannelEnum.Sms:
                return await VerifyPhoneAsync(userAccountId, normalizedIdentity, code, now, cancellationToken).ConfigureAwait(false);
            default:
                _logger.LogWarning("Unsupported channel for code verification: {Channel}", channel);
                return false;
        }
    }

    private async Task<bool> VerifyEmailAsync(
        Guid userAccountId,
        string normalizedEmail,
        string code,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Latest active email verification for this user + identity (hash is checked below).
        var entity = await _context.EmailVerifications
            .Where(x => x.UserAccountId == userAccountId
                        && x.Email == normalizedEmail
                        && x.ExpiresAt >= now
                        && x.UsedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            _logger.LogWarning("Email verification code not found for {Email}", normalizedEmail);
            return false;
        }

        if (!await IsUserAccountActiveAsync(userAccountId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("Email verification rejected for disabled account {Email}", normalizedEmail);
            return false;
        }

        // Check attempt count
        if (entity.Attempts >= entity.MaxAttempts)
        {
            _logger.LogWarning("Email verification code max attempts exceeded for {Email}", normalizedEmail);
            return false;
        }

        // Verify code hash
        var matches = entity.TokenLength == code.Length
                      && entity.TokenHash.SequenceEqual(CodeGeneratorHelper.GenerateHash(code));

        if (!matches)
        {
            // Wrong code for this identity — increment attempt counter
            entity.Attempts++;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Email verification code mismatch for {Email}", normalizedEmail);
            return false;
        }

        // Correct code — mark as used
        entity.UsedAt = now;

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Email verification code already used for {Email}", normalizedEmail);
            return false;
        }
    }

    private async Task<bool> VerifyPhoneAsync(
        Guid userAccountId,
        string normalizedPhone,
        string code,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Latest unused phone verification for this user + identity (hash is checked below).
        var entity = await _context.PhoneVerifications
            .Where(x => x.UserAccountId == userAccountId
                        && x.PhoneNumber == normalizedPhone
                        && x.ExpiresAt >= now
                        && x.UsedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            _logger.LogWarning("PhoneNumber verification code not found for {PhoneNumber}", normalizedPhone);
            return false;
        }

        if (!await IsUserAccountActiveAsync(userAccountId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("PhoneNumber verification rejected for disabled account {PhoneNumber}", normalizedPhone);
            return false;
        }

        // Check attempt count
        if (entity.Attempts >= entity.MaxAttempts)
        {
            _logger.LogWarning("PhoneNumber verification code max attempts exceeded for {PhoneNumber}", normalizedPhone);
            return false;
        }

        // Verify code hash
        var matches = entity.CodeLength == code.Length
                      && entity.CodeHash.SequenceEqual(CodeGeneratorHelper.GenerateHash(code));

        if (!matches)
        {
            // Wrong code for this identity — increment attempt counter
            entity.Attempts++;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("PhoneNumber verification code mismatch for {PhoneNumber}", normalizedPhone);
            return false;
        }

        // Correct code — mark as used
        entity.UsedAt = now;

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("PhoneNumber verification code already used for {PhoneNumber}", normalizedPhone);
            return false;
        }
    }

    private async Task EnsureOtpSendAllowedAsync(
        Guid userAccountId,
        ChannelEnum channel,
        string normalizedDestination,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var limits = _options.OtpSendRateLimit;
        var cooldownEnabled = limits.Cooldown > TimeSpan.Zero;
        var windowEnabled = limits.MaxSendsPerWindow > 0 && limits.Window > TimeSpan.Zero;
        if (!cooldownEnabled && !windowEnabled)
        {
            return;
        }

        if (channel == ChannelEnum.Email)
        {
            if (cooldownEnabled)
            {
                var lastCreatedAt = await _context.EmailVerifications
                    .AsNoTracking()
                    .Where(x => x.UserAccountId == userAccountId && x.Email == normalizedDestination)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => (DateTime?)x.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (lastCreatedAt.HasValue && now - lastCreatedAt.Value < limits.Cooldown)
                {
                    _logger.LogInformation(
                        "OTP send cooldown for user {UserAccountId} email {Email}: last send at {LastSendAt}",
                        userAccountId,
                        normalizedDestination,
                        lastCreatedAt.Value);
                    throw new ValidationException("Please wait before requesting another code.");
                }
            }

            if (windowEnabled)
            {
                var since = now - limits.Window;
                var count = await _context.EmailVerifications
                    .AsNoTracking()
                    .CountAsync(
                        x => x.UserAccountId == userAccountId
                             && x.Email == normalizedDestination
                             && x.CreatedAt >= since,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (count >= limits.MaxSendsPerWindow)
                {
                    _logger.LogInformation(
                        "OTP send window limit for user {UserAccountId} email {Email}: {Count} sends since {Since}",
                        userAccountId,
                        normalizedDestination,
                        count,
                        since);
                    throw new ValidationException("Too many verification codes requested. Please try again later.");
                }
            }

            return;
        }

        if (channel == ChannelEnum.Sms)
        {
            if (cooldownEnabled)
            {
                var lastCreatedAt = await _context.PhoneVerifications
                    .AsNoTracking()
                    .Where(x => x.UserAccountId == userAccountId && x.PhoneNumber == normalizedDestination)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => (DateTime?)x.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (lastCreatedAt.HasValue && now - lastCreatedAt.Value < limits.Cooldown)
                {
                    _logger.LogInformation(
                        "OTP send cooldown for user {UserAccountId} phone {Phone}: last send at {LastSendAt}",
                        userAccountId,
                        normalizedDestination,
                        lastCreatedAt.Value);
                    throw new ValidationException("Please wait before requesting another code.");
                }
            }

            if (windowEnabled)
            {
                var since = now - limits.Window;
                var count = await _context.PhoneVerifications
                    .AsNoTracking()
                    .CountAsync(
                        x => x.UserAccountId == userAccountId
                             && x.PhoneNumber == normalizedDestination
                             && x.CreatedAt >= since,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (count >= limits.MaxSendsPerWindow)
                {
                    _logger.LogInformation(
                        "OTP send window limit for user {UserAccountId} phone {Phone}: {Count} sends since {Since}",
                        userAccountId,
                        normalizedDestination,
                        count,
                        since);
                    throw new ValidationException("Too many verification codes requested. Please try again later.");
                }
            }
        }
    }

    private async Task SupersedeActiveEmailVerificationsAsync(
        Guid userAccountId,
        string normalizedEmail,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var active = await _context.EmailVerifications
            .Where(x => x.UserAccountId == userAccountId
                        && x.Email == normalizedEmail
                        && x.UsedAt == null
                        && x.ExpiresAt > now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var verification in active)
        {
            verification.ExpiresAt = now;
        }
    }

    private async Task SupersedeActivePhoneVerificationsAsync(
        Guid userAccountId,
        string phoneNumber,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var active = await _context.PhoneVerifications
            .Where(x => x.UserAccountId == userAccountId
                        && x.PhoneNumber == phoneNumber
                        && x.UsedAt == null
                        && x.ExpiresAt > now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var verification in active)
        {
            verification.ExpiresAt = now;
        }
    }

    private async Task<bool> IsUserAccountActiveAsync(Guid userAccountId, CancellationToken cancellationToken)
    {
        return await _context.UsersAccounts
            .AsNoTracking()
            .Where(x => x.Id == userAccountId)
            .Select(x => x.IsActive)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
