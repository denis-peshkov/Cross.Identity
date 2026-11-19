namespace Cross.Identity.Services;

internal sealed class CommunicationEndpointService : ICommunicationEndpointService, ICommunicationEndpointUpsertService
{
    private readonly IdentityContext _context;
    private readonly IAuditService _audit;
    private readonly AuthenticationOptions _options;

    public CommunicationEndpointService(
        IdentityContext context,
        IAuditService audit,
        IOptions<AuthenticationOptions> options)
    {
        _context = context;
        _audit = audit;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommunicationEndpointDto>> GetAllAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.UsersCommunicationEndpoints
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId)
            .OrderByDescending(x => x.IsPreferred)
            .ThenBy(x => x.Channel)
            .ThenBy(x => x.Address)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<CommunicationEndpointDto> UpsertAsync(
        Guid userAccountId,
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
                x => x.UserAccountId == userAccountId && x.Channel == channel && x.Address == normalized,
                cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        if (entity is null)
        {
            entity = new UserCommunicationEndpointEntity
            {
                Id = Guid.NewGuid(),
                UserAccountId = userAccountId,
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
                .AnyAsync(x => x.UserAccountId == userAccountId && x.IsPreferred, cancellationToken)
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
        Guid userAccountId,
        Guid endpointId,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.UsersCommunicationEndpoints
            .FirstOrDefaultAsync(x => x.Id == endpointId && x.UserAccountId == userAccountId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Communication endpoint was not found.");

        if (!entity.IsVerified)
        {
            throw new ValidationException("Only verified endpoints can be set as preferred for communication.");
        }

        var others = await _context.UsersCommunicationEndpoints
            .Where(x => x.UserAccountId == userAccountId && x.IsPreferred && x.Id != endpointId)
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
            UserAccountId = userAccountId,
            UserAccount = null!,
            Operation = AuditOperation.CommunicationEndpointChanged,
            EntityType = AuditEntityType.UserCommunicationEndpoint,
            EntityId = endpointId.ToString(),
            IpAddress = hostSuppliedClientContext.IpAddress,
            UserAgent = hostSuppliedClientContext.UserAgent,
            DeviceFingerprint = hostSuppliedClientContext.DeviceFingerprint,
            Notes = "Preferred communication endpoint updated.",
        });

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DeliveryTarget> ResolveDeliveryTargetAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
        => await ResolveTargetCoreAsync(userAccountId, allowUnverifiedAccountContact: false, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<DeliveryTarget> ResolveOtpTargetAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        var target = await ResolveTargetCoreAsync(userAccountId, allowUnverifiedAccountContact: true, cancellationToken).ConfigureAwait(false);
        return new DeliveryTarget
        {
            Channel = target.Channel.ToEmailOrSms(),
            Address = target.Address,
        };
    }

    /// <inheritdoc />
    public async Task<CommunicationEndpointDto?> GetPreferredAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetPreferredEntityAsync(userAccountId, cancellationToken).ConfigureAwait(false);
        return entity is null ? null : ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<CommunicationEndpointDto> AddAsync(
        Guid userAccountId,
        ChannelEnum channel,
        string address,
        CancellationToken cancellationToken = default)
    {
        EnsureOtpChannel(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        await UserAccountGuard.EnsureIsActiveAsync(_context, userAccountId, cancellationToken).ConfigureAwait(false);

        var normalized = channel.NormalizeAddress(address);
        await EnsureAddressAvailableForAddAsync(userAccountId, channel, normalized, cancellationToken)
            .ConfigureAwait(false);

        var existing = await _context.UsersCommunicationEndpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserAccountId == userAccountId && x.Channel == channel && x.Address == normalized,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is { IsVerified: true })
        {
            throw new ValidationException("Communication endpoint is already verified.");
        }

        return await UpsertAsync(
                userAccountId,
                channel,
                normalized,
                CommunicationEndpointSource.Manual,
                isVerified: false,
                entityId: null,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CommunicationEndpointDto> SetVerifiedAsync(
        Guid userAccountId,
        ChannelEnum channel,
        string address,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken = default)
    {
        EnsureOtpChannel(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        await UserAccountGuard.EnsureIsActiveAsync(_context, userAccountId, cancellationToken).ConfigureAwait(false);

        var normalized = channel.NormalizeAddress(address);
        await EnsureAddressAvailableForAddAsync(userAccountId, channel, normalized, cancellationToken)
            .ConfigureAwait(false);

        var dto = await UpsertAsync(
                userAccountId,
                channel,
                normalized,
                CommunicationEndpointSource.Manual,
                isVerified: true,
                entityId: null,
                cancellationToken)
            .ConfigureAwait(false);

        await ApplyVerifiedAccountContactIfMatchingAsync(userAccountId, channel, normalized, cancellationToken)
            .ConfigureAwait(false);

        _audit.Record(new AuditEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UserAccountId = userAccountId,
            UserAccount = null!,
            Operation = AuditOperation.CommunicationEndpointChanged,
            EntityType = AuditEntityType.UserCommunicationEndpoint,
            EntityId = dto.Id.ToString(),
            IpAddress = hostSuppliedClientContext.IpAddress,
            UserAgent = hostSuppliedClientContext.UserAgent,
            DeviceFingerprint = hostSuppliedClientContext.DeviceFingerprint,
            Notes = "Communication endpoint verified via CommunicationEndpointSetVerified OTP.",
        });

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return dto;
    }

    /// <inheritdoc />
    public async Task SyncAccountContactsAsync(Guid userAccountId, CancellationToken cancellationToken = default)
    {
        var account = await _context.UsersAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userAccountId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(account.Email) && account.EmailVerified)
        {
            await UpsertAsync(
                    userAccountId,
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
                    userAccountId,
                    ChannelEnum.Sms,
                    account.PhoneNumber,
                    CommunicationEndpointSource.Account,
                    isVerified: true,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<DeliveryTarget> ResolveTargetCoreAsync(
        Guid userAccountId,
        bool allowUnverifiedAccountContact,
        CancellationToken cancellationToken)
    {
        if (_options.LockChannelAsEmail)
        {
            return await RequireEmailTargetAsync(userAccountId, allowUnverifiedAccountContact, cancellationToken)
                .ConfigureAwait(false);
        }

        var preferred = await GetPreferredEntityAsync(userAccountId, cancellationToken).ConfigureAwait(false);
        if (preferred is not null)
        {
            return ToTarget(preferred);
        }

        var email = await FindEmailTargetAsync(userAccountId, allowUnverifiedAccountContact, cancellationToken)
            .ConfigureAwait(false);
        if (email is not null)
        {
            return email;
        }

        var phone = await FindPhoneTargetAsync(userAccountId, allowUnverifiedAccountContact, cancellationToken)
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
        Guid userAccountId,
        bool allowUnverifiedAccountEmail,
        CancellationToken cancellationToken)
    {
        var email = await FindEmailTargetAsync(userAccountId, allowUnverifiedAccountEmail, cancellationToken)
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
        Guid userAccountId,
        bool allowUnverifiedAccountEmail,
        CancellationToken cancellationToken)
    {
        var endpoint = await _context.UsersCommunicationEndpoints
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId && x.IsVerified && x.Channel == ChannelEnum.Email)
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
            .Where(x => x.Id == userAccountId)
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
        Guid userAccountId,
        bool allowUnverifiedAccountPhone,
        CancellationToken cancellationToken)
    {
        var endpoint = await _context.UsersCommunicationEndpoints
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId && x.IsVerified && x.Channel == ChannelEnum.Sms)
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
            .Where(x => x.Id == userAccountId)
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
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        return await _context.UsersCommunicationEndpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserAccountId == userAccountId && x.IsPreferred && x.IsVerified, cancellationToken)
            .ConfigureAwait(false);
    }

    private static void EnsureOtpChannel(ChannelEnum channel)
    {
        if (!channel.SupportsOtp())
        {
            throw new ValidationException("Only email and SMS channels can be added via OTP. Messenger channels are not supported yet.");
        }
    }

    private async Task EnsureAddressAvailableForAddAsync(
        Guid userAccountId,
        ChannelEnum channel,
        string normalizedAddress,
        CancellationToken cancellationToken)
    {
        var takenByEndpoint = await _context.UsersCommunicationEndpoints
            .AsNoTracking()
            .AnyAsync(
                x => x.Channel == channel
                    && x.Address == normalizedAddress
                    && x.IsVerified
                    && x.UserAccountId != userAccountId,
                cancellationToken)
            .ConfigureAwait(false);

        if (takenByEndpoint)
        {
            throw new ConflictException(channel == ChannelEnum.Email
                ? "Email already exists."
                : "PhoneNumber already exists.");
        }

        if (channel == ChannelEnum.Email)
        {
            await UserAccountGuard.EnsureNoOtherVerifiedEmailAsync(
                    _context,
                    userAccountId,
                    normalizedAddress,
                    cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        await UserAccountGuard.EnsureNoOtherVerifiedPhoneAsync(
                _context,
                userAccountId,
                normalizedAddress,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// When the verified address matches (or fills) the account primary contact, mark it verified on UsersAccounts.
    /// </summary>
    private async Task ApplyVerifiedAccountContactIfMatchingAsync(
        Guid userAccountId,
        ChannelEnum channel,
        string normalizedAddress,
        CancellationToken cancellationToken)
    {
        var account = await _context.UsersAccounts
            .FirstOrDefaultAsync(x => x.Id == userAccountId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return;
        }

        if (channel == ChannelEnum.Email)
        {
            if (string.IsNullOrWhiteSpace(account.Email))
            {
                account.Email = normalizedAddress;
            }

            if (string.Equals(
                    ChannelEnum.Email.NormalizeAddress(account.Email!),
                    normalizedAddress,
                    StringComparison.Ordinal))
            {
                account.EmailVerified = true;
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(account.PhoneNumber))
        {
            account.PhoneNumber = normalizedAddress;
        }

        if (string.Equals(
                ChannelEnum.Sms.NormalizeAddress(account.PhoneNumber!),
                normalizedAddress,
                StringComparison.Ordinal))
        {
            account.PhoneNumberVerified = true;
        }
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
