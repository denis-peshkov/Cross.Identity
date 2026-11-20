namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Stock <c>CommunicationEndpointAdd</c> step: <see cref="ICommunicationEndpointService.AddAsync"/> (unverified Manual upsert),
/// then send OTP to that address (not <see cref="ICommunicationEndpointService.ResolveOtpTargetAsync"/>).
/// The host must authorize the caller for <see cref="UserAccountIdKey"/>.
/// </summary>
internal sealed class CommunicationEndpointAddStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string UserAccountIdKey { get; init; }

    public required string EmailKey { get; init; }

    public required string PhoneKey { get; init; }

    public string? TtlKey { get; init; }

    public required string Template { get; init; }

    public required string Subject { get; init; }

    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }

    public required ICodeService CodeService { get; init; }

    public required INotificationComposer NotificationComposer { get; init; }

    public required IConfiguration Configuration { get; init; }

    public required ILogger Logger { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var userAccountId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserAccountIdKey));
        var (channel, address) = ResolveChannelAndAddress(ctx);

        var endpoint = await CommunicationEndpoints
            .AddAsync(userAccountId, channel, address, cancellationToken)
            .ConfigureAwait(false);

        var ttl = ResolveTtl(ctx);
        var code = channel.GenerateCode();

        var clientUrl = Configuration["Authentication:ClientUrl"]
            ?? throw new InvalidOperationException("Authentication:ClientUrl is not configured.");

        var actionUrl = BuildActionUrl(clientUrl, code, channel, address);
        var templateName = ResolveTemplateName(channel);
        var bodies = NotificationComposer.Compose(
            ctx,
            templateName,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["{{email}}"] = address,
                ["{{code}}"] = code,
                ["{{url}}"] = actionUrl,
                ["{{verificationLink}}"] = actionUrl,
                ["{{helpLink}}"] = actionUrl,
                ["{{logoLink}}"] = actionUrl,
                ["{{imageLink}}"] = actionUrl,
                ["{{expires}}"] = ttl.ToHumanString(),
            });

        var msg = NotificationMessage.For(channel, address)
            .WithSubject(Subject)
            .WithTextBody(bodies.TextBody)
            .WithTextHtml(bodies.HtmlBody);

        try
        {
            await CodeService.SendAsync(msg, code, userAccountId, ttl, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "{Kind} send failed for user {UserAccountId} {UserCommunicationEndpointId} channel {Channel} address {Address}: {Message}",
                Kind,
                userAccountId,
                endpoint.Id,
                channel,
                channel.MaskAddressForLog(endpoint.Address),
                ex.Message);
            return StepResult.Fail(ex);
        }

        if (Configuration.GetValue<bool>("Authentication:DeveloperMode"))
        {
            ctx.Set(BagKey.Qualify(Kind, "LastCode"), code);
        }

        ctx.Set(BagKey.Qualify(Kind, "Endpoint"), endpoint);
        ctx.Set(BagKey.Qualify(Kind, "Channel"), channel);
        ctx.Set(BagKey.Qualify(Kind, "Address"), endpoint.Address);

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

    private string ResolveTemplateName(ChannelEnum channel)
    {
        if (channel != ChannelEnum.Sms)
        {
            return Template;
        }

        if (Template.EndsWith("-sms", StringComparison.OrdinalIgnoreCase))
        {
            return Template;
        }

        return Template + "-sms";
    }

    private string BuildActionUrl(string clientUrl, string code, ChannelEnum channel, string address)
    {
        var baseUrl = clientUrl.TrimEnd('/');
        var encodedCode = Uri.EscapeDataString(code);
        var baseTemplate = Template.EndsWith("-sms", StringComparison.OrdinalIgnoreCase)
            ? Template[..^4]
            : Template;
        var path = baseTemplate.Equals("reset", StringComparison.OrdinalIgnoreCase)
            ? "reset-password"
            : "verify";
        var url = $"{baseUrl}/{path}?code={encodedCode}";

        return channel == ChannelEnum.Email
            ? url + $"&email={Uri.EscapeDataString(address)}"
            : url + $"&phone={Uri.EscapeDataString(address)}";
    }

    private TimeSpan ResolveTtl(Bag ctx)
    {
        var ttlDefault = TimeSpan.FromMinutes(5);

        if (string.IsNullOrWhiteSpace(TtlKey))
        {
            return ttlDefault;
        }

        return ctx.Get<TimeSpan?>(BagKey.Qualify(Kind, TtlKey)) ?? ttlDefault;
    }
}
