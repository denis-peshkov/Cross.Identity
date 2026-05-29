namespace Cross.Identity.Services;

/// <summary>
/// Сервис OTP-кодов на базе EF Core для отправки email/SMS.
/// </summary>
internal sealed class CodeService : ICodeService
{
    private readonly IdentityContext _context;
    private readonly ILogger<CodeService> _logger;
    private readonly IEmailSenderService _email;
    private readonly ISmsSenderService _sms;
    private readonly IOptionsSnapshot<MessagingEmailOptions> _options;

    public CodeService(
        IdentityContext context,
        ILogger<CodeService> logger,
        IEmailSenderService email,
        ISmsSenderService sms,
        IOptionsSnapshot<MessagingEmailOptions> options)
    {
        _context = context;
        _logger = logger;
        _email = email;
        _sms = sms;
        _options = options;
    }

    /// <inheritdoc />
    public async Task SendAsync(NotificationMessage msg, string code, string userId, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var destination = msg.Destination.Trim();

        var id = Guid.TryParse(userId, out var guid)
            ? guid
            : throw new ArgumentException("Invalid user id", nameof(userId));

        switch (msg.Channel)
        {
            case ChannelEnum.Email:
                await _email.SendAsync("", destination, msg.Subject, msg.TextBody, msg.HtmlBody, cancellationToken).ConfigureAwait(false);
                var emailEntity = new EmailVerificationEntity
                {
                    UserAccountId = id,
                    NormalizedEmail = destination.ToLowerInvariant(),
                    TokenHash = CodeGeneratorHelper.GenerateHash(code),
                    TokenLength = (byte)code.Length,
                    Attempts = 0,
                    MaxAttempts = 3,
                    ExpiresAt = DateTimeOffset.UtcNow.Add(ttl).UtcDateTime
                };
                await _context.EmailVerifications.AddAsync(emailEntity, cancellationToken).ConfigureAwait(false);
                break;

            case ChannelEnum.Sms:
                await _sms.SendAsync(destination, msg.TextBody, cancellationToken).ConfigureAwait(false);
                var phoneEntity = new PhoneVerificationEntity
                {
                    UserAccountId = id,
                    PhoneNumber = destination,
                    CodeHash = CodeGeneratorHelper.GenerateHash(code),
                    CodeLength = (byte)code.Length,
                    Attempts = 0,
                    MaxAttempts = 3,
                    ExpiresAt = DateTimeOffset.UtcNow.Add(ttl).UtcDateTime
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

        // Нормализуем identity в зависимости от канала
        var normalizedIdentity = channel.ToLowerInvariant() switch
        {
            "email" => identity.Trim().ToLowerInvariant(),
            "phone" => identity.Trim(), // Phone уже должен быть в E.164 формате
            _ => identity.Trim()
        };

        // Вычисляем хеш кода (SHA-256, 32 байта)
        var codeHash = SHA256.HashData(Encoding.UTF8.GetBytes(code));

        switch (channel.ToLowerInvariant())
        {
            case "email":
                {
                    // Ищем код для email
                    var entity = await _context.EmailVerifications
                        .Where(x => x.NormalizedEmail == normalizedIdentity && x.TokenHash == codeHash)
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

                    // Помечаем код как использованный
                    entity.UsedAt = now;
                    await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    return true;
                }
            case "phone":
                {
                    // Для телефона сначала находим последнюю запись (без проверки хеша)
                    var entity = await _context.PhoneVerifications
                        .Where(x => x.PhoneNumber == normalizedIdentity && x.CodeHash == codeHash)
                        .OrderByDescending(x => x.CreatedAt)
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

                    // Проверяем количество попыток
                    if (entity.Attempts >= entity.MaxAttempts)
                    {
                        _logger.LogWarning("Phone verification code max attempts exceeded for {Phone}", normalizedIdentity);
                        return false;
                    }

                    // Увеличиваем счётчик попыток (даже для неверного кода)
                    entity.Attempts++;

                    // Проверяем хеш кода
                    if (!entity.CodeHash.SequenceEqual(codeHash))
                    {
                        // Код неверный, но попытка уже засчитана
                        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        _logger.LogWarning("Phone verification code mismatch for {Phone}", normalizedIdentity);
                        return false;
                    }

                    // Код верный - помечаем как использованный
                    entity.UsedAt = now;
                    await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    return true;
                }
            default:
                _logger.LogWarning("Unsupported channel for code verification: {Channel}", channel);
                return false;
        }
    }
}
