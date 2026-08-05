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

        switch (msg.Channel)
        {
            case ChannelEnum.Email:
                if (!developerMode)
                {
                    await _email.SendAsync("", destination, msg.Subject, msg.TextBody, msg.HtmlBody, cancellationToken).ConfigureAwait(false);
                }
                var emailEntity = new EmailVerificationEntity
                {
                    UserAccountId = id,
                    Email = destination.ToLowerInvariant(),
                    TokenHash = CodeGeneratorHelper.GenerateHash(code),
                    TokenLength = (byte)code.Length,
                    Attempts = 0,
                    MaxAttempts = 3,
                    ExpiresAt = DateTimeOffset.UtcNow.Add(ttl).UtcDateTime,
                    CreatedAt = DateTime.UtcNow,
                };
                await _context.EmailVerifications.AddAsync(emailEntity, cancellationToken).ConfigureAwait(false);
                break;

            case ChannelEnum.Sms:
                if (!developerMode)
                {
                    await _sms.SendAsync(destination, msg.TextBody, cancellationToken).ConfigureAwait(false);
                }
                var phoneEntity = new PhoneVerificationEntity
                {
                    UserAccountId = id,
                    PhoneNumber = destination,
                    CodeHash = CodeGeneratorHelper.GenerateHash(code),
                    CodeLength = (byte)code.Length,
                    Attempts = 0,
                    MaxAttempts = 3,
                    ExpiresAt = DateTimeOffset.UtcNow.Add(ttl).UtcDateTime,
                    CreatedAt = DateTime.UtcNow,
                };
                await _context.PhoneVerifications.AddAsync(phoneEntity, cancellationToken).ConfigureAwait(false);
                break;

            case ChannelEnum.Telegram:
            case ChannelEnum.Viber:
            case ChannelEnum.WatsApp:
            default:
                break;
        }
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("[Notifier] {Channel} → {Dest}: {Subject} | {Body}",
            msg.Channel, msg.Destination, msg.Subject, msg.TextBody);
    }

    /// <inheritdoc />
    public async Task<bool> VerifyAsync(string channel, string identity, string code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(code);

        var now = DateTime.UtcNow;

        // Normalize identity based on channel
        var normalizedIdentity = channel.ToLowerInvariant() switch
        {
            "email" => identity.Trim().ToLowerInvariant(),
            "phone" => identity.Trim(), // Phone should already be in E.164 format
            _ => identity.Trim()
        };

        // Compute code hash (SHA-256, 32 bytes)
        var codeHash = SHA256.HashData(Encoding.UTF8.GetBytes(code));

        switch (channel.ToLowerInvariant())
        {
            case "email":
                {
                    // Look up email code
                    var entity = await _context.EmailVerifications
                        .Where(x => x.Email == normalizedIdentity && x.TokenHash == codeHash && x.UsedAt == null)
                        .OrderByDescending(x => x.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (entity is null)
                    {
                        _logger.LogWarning("Email verification code not found for {Email}", normalizedIdentity);
                        return false;
                    }

                    if (entity.ExpiresAt < now)
                    {
                        _logger.LogWarning("Email verification code expired for {Email}", normalizedIdentity);
                        return false;
                    }

                    // Mark code as used
                    entity.UsedAt = now;

                    try
                    {
                        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        return true;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        _logger.LogWarning("Email verification code already used for {Email}", normalizedIdentity);
                        return false;
                    }
                }
            case "phone":
                {
                    // For phone, find the latest unused record
                    var entity = await _context.PhoneVerifications
                        .Where(x => x.PhoneNumber == normalizedIdentity && x.CodeHash == codeHash && x.UsedAt == null)
                        .FirstOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (entity is null)
                    {
                        _logger.LogWarning("Phone verification code not found for {Phone}", normalizedIdentity);
                        return false;
                    }

                    if (entity.ExpiresAt < now)
                    {
                        _logger.LogWarning("Phone verification code expired for {Phone}", normalizedIdentity);
                        return false;
                    }

                    // Check attempt count
                    if (entity.Attempts >= entity.MaxAttempts)
                    {
                        _logger.LogWarning("Phone verification code max attempts exceeded for {Phone}", normalizedIdentity);
                        return false;
                    }

                    // Increment attempt counter (even for wrong code)
                    entity.Attempts++;

                    // Verify code hash
                    if (!entity.CodeHash.SequenceEqual(codeHash))
                    {
                        // Wrong code, but attempt already counted
                        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        _logger.LogWarning("Phone verification code mismatch for {Phone}", normalizedIdentity);
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
                        _logger.LogWarning("Phone verification code already used for {Phone}", normalizedIdentity);
                        return false;
                    }
                }
            default:
                _logger.LogWarning("Unsupported channel for code verification: {Channel}", channel);
                return false;
        }
    }
}
