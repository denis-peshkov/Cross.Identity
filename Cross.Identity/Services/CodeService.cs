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

    public CodeService(
        IdentityContext context,
        ILogger<CodeService> logger,
        IEmailSenderService email,
        ISmsSenderService sms,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _email = email;
        _sms = sms;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task SendAsync(NotificationMessage msg, string code, string userId, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var destination = msg.Destination.Trim();

        var id = Guid.TryParse(userId, out var guid)
            ? guid
            : throw new ArgumentException("Invalid user id", nameof(userId));

        var developerMode = _configuration.GetValue<bool>("Authentication:DeveloperMode");
        var now = DateTime.UtcNow;

        if (!msg.Channel.SupportsOtp())
        {
            throw new NotSupportedException($"OTP send via {msg.Channel} is not supported. Use Email or Sms; messenger delivery is not implemented yet.");
        }

        switch (msg.Channel)
        {
            case ChannelEnum.Email:
                if (!developerMode)
                {
                    await _email.SendAsync("", destination, msg.Subject, msg.TextBody, msg.HtmlBody, cancellationToken).ConfigureAwait(false);
                }

                var normalizedEmail = destination.ToLowerInvariant();
                await SupersedeActiveEmailVerificationsAsync(normalizedEmail, now, cancellationToken).ConfigureAwait(false);

                var emailEntity = new EmailVerificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserAccountId = id,
                    UserAccount = null!,
                    Email = normalizedEmail,
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

                await SupersedeActivePhoneVerificationsAsync(destination, now, cancellationToken).ConfigureAwait(false);

                var phoneEntity = new PhoneVerificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserAccountId = id,
                    UserAccount = null!,
                    PhoneNumber = destination,
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
    public async Task<bool> VerifyAsync(ChannelEnum channel, string identity, string code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(code);

        var now = DateTime.UtcNow;

        // Normalize identity based on channel
        var normalizedIdentity = channel switch
        {
            ChannelEnum.Email => identity.Trim().ToLowerInvariant(),
            ChannelEnum.Sms => identity.Trim(), // PhoneNumber should already be in E.164 format
            _ => identity.Trim()
        };

        switch (channel)
        {
            case ChannelEnum.Email:
                return await VerifyEmailAsync(normalizedIdentity, code, now, cancellationToken).ConfigureAwait(false);
            case ChannelEnum.Sms:
                return await VerifyPhoneAsync(normalizedIdentity, code, now, cancellationToken).ConfigureAwait(false);
            default:
                _logger.LogWarning("Unsupported channel for code verification: {Channel}", channel);
                return false;
        }
    }

    private async Task<bool> VerifyEmailAsync(
        string normalizedEmail,
        string code,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Look up the latest active email verification for this identity (hash is checked below).
        var entity = await _context.EmailVerifications
            .Where(x => x.Email == normalizedEmail && x.ExpiresAt >= now && x.UsedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            _logger.LogWarning("Email verification code not found for {Email}", normalizedEmail);
            return false;
        }

        if (!await IsUserAccountActiveAsync(entity.UserAccountId, cancellationToken).ConfigureAwait(false))
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
        string normalizedPhone,
        string code,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // For phone, find the latest unused record for this identity (hash is checked below).
        var entity = await _context.PhoneVerifications
            .Where(x => x.PhoneNumber == normalizedPhone && x.ExpiresAt >= now && x.UsedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            _logger.LogWarning("PhoneNumber verification code not found for {PhoneNumber}", normalizedPhone);
            return false;
        }

        if (!await IsUserAccountActiveAsync(entity.UserAccountId, cancellationToken).ConfigureAwait(false))
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

    private async Task SupersedeActiveEmailVerificationsAsync(
        string normalizedEmail,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var active = await _context.EmailVerifications
            .Where(x => x.Email == normalizedEmail && x.UsedAt == null && x.ExpiresAt > now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var verification in active)
        {
            verification.ExpiresAt = now;
        }
    }

    private async Task SupersedeActivePhoneVerificationsAsync(
        string phoneNumber,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var active = await _context.PhoneVerifications
            .Where(x => x.PhoneNumber == phoneNumber && x.UsedAt == null && x.ExpiresAt > now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var verification in active)
        {
            verification.ExpiresAt = now;
        }
    }

    private async Task<bool> IsUserAccountActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.UsersAccounts
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.IsActive)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
