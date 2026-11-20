namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Stock <c>CommunicationEndpointSetVerified</c> step: verify OTP for the submitted email/phone, then
/// <see cref="ICommunicationEndpointService.SetVerifiedAsync"/> (marks Manual endpoint verified).
/// The host must authorize the caller for <see cref="UserAccountIdKey"/>.
/// </summary>
internal sealed class CommunicationEndpointSetVerifiedStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string UserAccountIdKey { get; init; }

    public required string EmailKey { get; init; }

    public required string PhoneKey { get; init; }

    public required string CodeKey { get; init; }

    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }

    public required ICodeService CodeService { get; init; }

    public required ILogger Logger { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var userAccountId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserAccountIdKey));
        var (channel, address) = ResolveChannelAndAddress(ctx);
        var code = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ValidationException("Code is required.");
        }

        var normalized = channel.NormalizeAddress(address);
        var ok = await CodeService
            .VerifyAsync(userAccountId, channel, normalized, code, cancellationToken)
            .ConfigureAwait(false);

        if (!ok)
        {
            var endpointId = await TryResolveEndpointIdAsync(userAccountId, channel, normalized, cancellationToken)
                .ConfigureAwait(false);
            Logger.LogInformation(
                "{Kind} rejected for user {UserAccountId} {UserCommunicationEndpointId} channel {Channel} address {Address}: invalid or expired code.",
                Kind,
                userAccountId,
                endpointId,
                channel,
                channel.MaskAddressForLog(normalized));
            throw new ValidationException("Invalid or expired verification code.");
        }

        var hostSuppliedClientContext = HostSuppliedClientContext.Read(ctx);
        var endpoint = await CommunicationEndpoints
            .SetVerifiedAsync(userAccountId, channel, normalized, hostSuppliedClientContext, cancellationToken)
            .ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "Endpoint"), endpoint);
        ctx.Set(BagKey.Qualify(Kind, "Channel"), channel);
        ctx.Set(BagKey.Qualify(Kind, "Address"), endpoint.Address);
        ctx.Set(BagKey.Qualify(Kind, "IsVerified"), endpoint.IsVerified);

        return StepResult.Ok(Next);
    }

    private (ChannelEnum Channel, string Address) ResolveChannelAndAddress(Bag ctx)
    {
        var email = ctx.TryGet<string>(BagKey.Qualify(Kind, EmailKey), out var emailValue)
            ? emailValue
            : null;
        var phone = ctx.TryGet<string>(BagKey.Qualify(Kind, PhoneKey), out var phoneValue)
            ? phoneValue
            : null;

        var hasEmail = !string.IsNullOrWhiteSpace(email);
        var hasPhone = !string.IsNullOrWhiteSpace(phone);

        if (hasEmail == hasPhone)
        {
            throw new ValidationException("Provide exactly one of Email or PhoneNumber.");
        }

        return hasEmail
            ? (ChannelEnum.Email, email!)
            : (ChannelEnum.Sms, phone!);
    }

    private async Task<Guid?> TryResolveEndpointIdAsync(
        Guid userAccountId,
        ChannelEnum channel,
        string normalizedAddress,
        CancellationToken cancellationToken)
    {
        var endpoints = await CommunicationEndpoints
            .GetAllAsync(userAccountId, cancellationToken)
            .ConfigureAwait(false);

        return endpoints
            .FirstOrDefault(x => x.Channel == channel && x.Address == normalizedAddress)
            ?.Id;
    }
}
