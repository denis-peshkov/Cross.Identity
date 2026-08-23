namespace Cross.Identity.Services;

internal sealed class CommunicationEndpointService : ICommunicationEndpointService
{
    private readonly IdentityContext _context;
    private readonly IAuditService _audit;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly AuthenticationOptions _options;

    public CommunicationEndpointService(
        IdentityContext context,
        IAuditService audit,
        IJwtTokenService jwtTokenService,
        IOptions<AuthenticationOptions> options)
    {
        _context = context;
        _audit = audit;
        _jwtTokenService = jwtTokenService;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommunicationEndpointDto>> GetAllAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        await _jwtTokenService
            .EnsureRefreshTokenBelongsToUserAsync(refreshToken, userId, cancellationToken)
            .ConfigureAwait(false);

        var rows = await _context.UsersCommunicationEndpoints
            .AsNoTracking()
            .Where(x => x.UserAccountId == userId)
            .OrderByDescending(x => x.IsPreferred)
            .ThenBy(x => x.Channel)
            .ThenBy(x => x.Address)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<CommunicationEndpointDto> UpsertAsync(
        Guid userId,
        ChannelEnum channel,
        string address,
        CommunicationEndpointSource source,
        bool isVerified,
        Guid? entityId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        var normalized = channel.NormalizeAddress(address);

        var entity = await _context.UsersCommunicationEndpoints
            .FirstOrDefaultAsync(
                x => x.UserAccountId == userId && x.Channel == channel && x.Address == normalized,
                cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        if (entity is null)
        {
            entity = new UserCommunicationEndpointEntity
            {
                Id = Guid.NewGuid(),
                UserAccountId = userId,
                UserAccount = null!,
                Channel = channel,
                Address = normalized,
                IsVerified = isVerified,
                Source = source,
                EntityId = entityId,
                IsPreferred = false,
                CreatedAt = now,
            };
            await _context.UsersCommunicationEndpoints.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            entity.IsVerified = isVerified || entity.IsVerified;
            entity.Source = source;
            if (entityId.HasValue)
            {
                entity.EntityId = entityId;
            }

            entity.UpdatedAt = now;
        }

        if (entity.IsVerified)
        {
            var hasPreferred = await _context.UsersCommunicationEndpoints
                .AnyAsync(x => x.UserAccountId == userId && x.IsPreferred, cancellationToken)
                .ConfigureAwait(false);
            if (!hasPreferred)
            {
                entity.IsPreferred = true;
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task SetPreferredAsync(
        Guid userId,
        Guid endpointId,
        string refreshToken,
        ClientContext clientContext,
        CancellationToken cancellationToken = default)
    {
        await _jwtTokenService
            .EnsureRefreshTokenBelongsToUserAsync(refreshToken, userId, cancellationToken)
            .ConfigureAwait(false);

        var entity = await _context.UsersCommunicationEndpoints
            .FirstOrDefaultAsync(x => x.Id == endpointId && x.UserAccountId == userId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Communication endpoint was not found.");

        if (!entity.IsVerified)
        {
            throw new ValidationException("Only verified endpoints can be set as preferred for communication.");
        }

        var others = await _context.UsersCommunicationEndpoints
            .Where(x => x.UserAccountId == userId && x.IsPreferred && x.Id != endpointId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var other in others)
        {
            other.IsPreferred = false;
            other.UpdatedAt = DateTime.UtcNow;
        }

        entity.IsPreferred = true;
        entity.UpdatedAt = DateTime.UtcNow;

        _audit.Record(new AuditEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UserAccountId = userId,
            UserAccount = null!,
            Operation = AuditOperation.CommunicationEndpointChanged,
            EntityType = AuditEntityType.UserCommunicationEndpoint,
            EntityId = endpointId.ToString(),
            IpAddress = clientContext.IpAddress,
            UserAgent = clientContext.UserAgent,
            DeviceFingerprint = clientContext.DeviceFingerprint,
            Notes = "Preferred communication endpoint updated.",
        });

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DeliveryTarget> ResolveDeliveryTargetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await ResolveTargetCoreAsync(userId, allowUnverifiedAccountContact: false, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<DeliveryTarget> ResolveOtpTargetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var target = await ResolveTargetCoreAsync(userId, allowUnverifiedAccountContact: true, cancellationToken).ConfigureAwait(false);
        return new DeliveryTarget
        {
            Channel = target.Channel.ToEmailOrSms(),
            Address = target.Address,
        };
    }

    /// <inheritdoc />
    public async Task<CommunicationEndpointDto?> GetPreferredAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetPreferredEntityAsync(userId, cancellationToken).ConfigureAwait(false);
        return entity is null ? null : ToDto(entity);
    }

    /// <inheritdoc />
    public async Task SyncAccountContactsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await _context.UsersAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(account.Email) && account.EmailVerified)
        {
            await UpsertAsync(
                    userId,
                    ChannelEnum.Email,
                    account.Email,
                    CommunicationEndpointSource.Account,
                    isVerified: true,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(account.PhoneNumber) && account.PhoneNumberVerified)
        {
            await UpsertAsync(
                    userId,
                    ChannelEnum.Sms,
                    account.PhoneNumber,
                    CommunicationEndpointSource.Account,
                    isVerified: true,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<DeliveryTarget> ResolveTargetCoreAsync(
        Guid userId,
        bool allowUnverifiedAccountContact,
        CancellationToken cancellationToken)
    {
        if (_options.LockChannelAsEmail)
        {
            return await RequireEmailTargetAsync(userId, allowUnverifiedAccountContact, cancellationToken)
                .ConfigureAwait(false);
        }

        var preferred = await GetPreferredEntityAsync(userId, cancellationToken).ConfigureAwait(false);
        if (preferred is not null)
        {
            return ToTarget(preferred);
        }

        var email = await FindEmailTargetAsync(userId, allowUnverifiedAccountContact, cancellationToken)
            .ConfigureAwait(false);
        if (email is not null)
        {
            return email;
        }

        var phone = await FindPhoneTargetAsync(userId, allowUnverifiedAccountContact, cancellationToken)
            .ConfigureAwait(false);
        if (phone is not null)
        {
            return phone;
        }

        throw new ValidationException(
            allowUnverifiedAccountContact
                ? "No preferred verified communication channel and no email or phone. Set a preferred endpoint or provide an email or phone number."
                : "No preferred verified communication channel and no verified email or phone. Set a preferred endpoint or verify an email or phone number.");
    }

    private async Task<DeliveryTarget> RequireEmailTargetAsync(
        Guid userId,
        bool allowUnverifiedAccountEmail,
        CancellationToken cancellationToken)
    {
        var email = await FindEmailTargetAsync(userId, allowUnverifiedAccountEmail, cancellationToken)
            .ConfigureAwait(false);
        if (email is not null)
        {
            return email;
        }

        throw new ValidationException(
            allowUnverifiedAccountEmail
                ? "Authentication:LockChannelAsEmail is enabled but no email is available for the user."
                : "Authentication:LockChannelAsEmail is enabled but no verified email is available for the user.");
    }

    private async Task<DeliveryTarget?> FindEmailTargetAsync(
        Guid userId,
        bool allowUnverifiedAccountEmail,
        CancellationToken cancellationToken)
    {
        var endpoint = await _context.UsersCommunicationEndpoints
            .AsNoTracking()
            .Where(x => x.UserAccountId == userId && x.IsVerified && x.Channel == ChannelEnum.Email)
            .OrderByDescending(x => x.IsPreferred)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (endpoint is not null)
        {
            return ToTarget(endpoint);
        }

        var account = await _context.UsersAccounts
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { x.Email, x.EmailVerified })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (account is null || string.IsNullOrWhiteSpace(account.Email))
        {
            return null;
        }

        if (!account.EmailVerified && !allowUnverifiedAccountEmail)
        {
            return null;
        }

        return new DeliveryTarget
        {
            Channel = ChannelEnum.Email,
            Address = ChannelEnum.Email.NormalizeAddress(account.Email),
        };
    }

    private async Task<DeliveryTarget?> FindPhoneTargetAsync(
        Guid userId,
        bool allowUnverifiedAccountPhone,
        CancellationToken cancellationToken)
    {
        var endpoint = await _context.UsersCommunicationEndpoints
            .AsNoTracking()
            .Where(x => x.UserAccountId == userId && x.IsVerified && x.Channel == ChannelEnum.Sms)
            .OrderByDescending(x => x.IsPreferred)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (endpoint is not null)
        {
            return ToTarget(endpoint);
        }

        var account = await _context.UsersAccounts
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { x.PhoneNumber, x.PhoneNumberVerified })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (account is null || string.IsNullOrWhiteSpace(account.PhoneNumber))
        {
            return null;
        }

        if (!account.PhoneNumberVerified && !allowUnverifiedAccountPhone)
        {
            return null;
        }

        return new DeliveryTarget
        {
            Channel = ChannelEnum.Sms,
            Address = ChannelEnum.Sms.NormalizeAddress(account.PhoneNumber),
        };
    }

    private async Task<UserCommunicationEndpointEntity?> GetPreferredEntityAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.UsersCommunicationEndpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserAccountId == userId && x.IsPreferred && x.IsVerified, cancellationToken)
            .ConfigureAwait(false);
    }

    private static DeliveryTarget ToTarget(UserCommunicationEndpointEntity entity)
        => new()
        {
            Channel = entity.Channel,
            Address = entity.Address,
        };

    private static CommunicationEndpointDto ToDto(UserCommunicationEndpointEntity entity)
        => new()
        {
            Id = entity.Id,
            Channel = entity.Channel,
            Address = entity.Address,
            IsVerified = entity.IsVerified,
            Source = entity.Source,
            IsPreferred = entity.IsPreferred,
        };
}
