namespace Cross.Identity.Services;

internal sealed class CommunicationEndpointService : ICommunicationEndpointService
{
    private readonly IdentityContext _context;
    private readonly IAuditService _audit;
    private readonly IJwtTokenService _jwtTokenService;

    public CommunicationEndpointService(
        IdentityContext context,
        IAuditService audit,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _audit = audit;
        _jwtTokenService = jwtTokenService;
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
    public async Task<ChannelEnum> ResolveDeliveryChannelAsync(
        Guid userId,
        string selectorField,
        string selectorValue,
        ChannelEnum? fallback = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectorField);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectorValue);

        var field = selectorField.Trim().ToLowerInvariant();
        if (field is "email")
        {
            return ChannelEnum.Email;
        }

        if (field is "phone" or "phonenumber")
        {
            var address = ChannelEnum.Sms.NormalizeAddress(selectorValue);
            var phoneEndpoints = await _context.UsersCommunicationEndpoints
                .AsNoTracking()
                .Where(x => x.UserAccountId == userId
                            && x.IsVerified
                            && x.Address == address
                            && ChannelEnumExtensions.PhoneChannels.Contains(x.Channel))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var preferred = phoneEndpoints.FirstOrDefault(x => x.IsPreferred);
            if (preferred is not null)
            {
                return preferred.Channel;
            }

            if (phoneEndpoints.Count > 0)
            {
                return phoneEndpoints[0].Channel;
            }

            return fallback is { } phoneFallback && phoneFallback.IsPhoneChannel()
                ? phoneFallback
                : ChannelEnum.Sms;
        }

        if (field is "username")
        {
            var preferred = await GetPreferredEntityAsync(userId, cancellationToken).ConfigureAwait(false)
                ?? throw new ValidationException(
                    "No preferred verified communication channel. Set one before sending by user name.");
            return preferred.Channel;
        }

        if (fallback.HasValue)
        {
            return fallback.Value;
        }

        throw new ValidationException($"Cannot resolve delivery channel for field '{selectorField}'.");
    }

    /// <inheritdoc />
    public async Task<ChannelEnum> ResolveOtpChannelAsync(
        Guid userId,
        string selectorField,
        string selectorValue,
        ChannelEnum? fallback = null,
        CancellationToken cancellationToken = default)
    {
        var channel = await ResolveDeliveryChannelAsync(
                userId,
                selectorField,
                selectorValue,
                fallback,
                cancellationToken)
            .ConfigureAwait(false);

        return channel.ToEmailOrSms();
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

        if (!string.IsNullOrWhiteSpace(account.Email) && account.EmailConfirmed)
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

        if (!string.IsNullOrWhiteSpace(account.PhoneNumber) && account.PhoneNumberConfirmed)
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

    private async Task<UserCommunicationEndpointEntity?> GetPreferredEntityAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.UsersCommunicationEndpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserAccountId == userId && x.IsPreferred && x.IsVerified, cancellationToken)
            .ConfigureAwait(false);
    }

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
