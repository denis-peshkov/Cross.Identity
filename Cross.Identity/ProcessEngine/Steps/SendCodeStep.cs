namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for sending a one-time code to the user.
/// Delivery channel/address come from <see cref="ICommunicationEndpointService.ResolveOtpTargetAsync"/>.
/// Template language is selected via <see cref="HostSuppliedLanguageContext"/> (<c>collectForm.LanguageCode</c>).
/// Unknown identity / missing OTP channel surface as <see cref="NotAuthorizedException"/> (<c>Invalid credentials.</c>);
/// the real reason is logged at Information (anti user-enumeration).
/// </summary>
internal sealed class SendCodeStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Code service (send/verify).</summary>
    public required ICodeService CodeService { get; init; }
    public required IUserService UserService { get; init; }
    public required IHostEnvironment Environment { get; init; }
    public required INotificationComposer NotificationComposer { get; init; }
    public required ILogger Logger { get; init; }

    /// <summary>Identity selector (bag keys for field name + value).</summary>
    public required Selector Selector { get; init; }

    /// <summary>Resolves preferred / OTP delivery target from user endpoints.</summary>
    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }

    /// <summary>
    /// Optional key in <see cref="Bag"/> for a per-request TTL (for example, <c>"collectForm.Ttl"</c>).
    /// </summary>
    public string? TtlKey { get; init; }

    /// <summary>Template name under Definitions/Templates. Defaults to <c>verify</c>.</summary>
    /// <remarks>
    /// Action link path: <c>reset</c> → <c>/reset-password</c>; other templates (e.g. <c>verify</c>) → <c>/verify</c>.
    /// When selector is Email / PhoneNumber, <c>email</c> / <c>phone</c> query params are appended (deep-link identity; channel is still resolved server-side on verify).
    /// </remarks>
    public required string Template { get; init; }

    /// <summary>Notification subject line. Defaults to <c>Verification Code</c>.</summary>
    public required string Subject { get; init; }

    public IConfiguration Configuration { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);

        var userAccountId = await UserService.GetUserAccountIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        if (userAccountId is not { } resolvedUserAccountId || resolvedUserAccountId == Guid.Empty)
        {
            Logger.LogInformation(
                "Send code rejected for {Field} identity {Identity}: user not found.",
                selector.Field,
                selector.Value);
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        DeliveryTarget target;
        try
        {
            target = await CommunicationEndpoints
                .ResolveOtpTargetAsync(resolvedUserAccountId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            Logger.LogInformation(
                "Send code rejected for {Field} identity {Identity}: {Reason}",
                selector.Field,
                selector.Value,
                ex.Message);
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        if (!target.Channel.SupportsOtp())
        {
            Logger.LogInformation(
                "Send code rejected for {Field} identity {Identity}: channel {Channel} does not support OTP.",
                selector.Field,
                selector.Value,
                target.Channel);
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        var ttl = ResolveTtl(ctx);
        var code = target.Channel.GenerateCode();

        var clientUrl = Configuration["Authentication:ClientUrl"]
            ?? throw new InvalidOperationException("Authentication:ClientUrl is not configured.");

        var actionUrl = BuildActionUrl(clientUrl, code, selector);
        var bodies = NotificationComposer.Compose(
            ctx,
            Template,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["{{email}}"] = selector.Value,
                ["{{code}}"] = code,
                ["{{url}}"] = actionUrl,
                ["{{verificationLink}}"] = actionUrl,
                ["{{helpLink}}"] = actionUrl,
                ["{{logoLink}}"] = actionUrl,
                ["{{imageLink}}"] = actionUrl,
                ["{{expires}}"] = ttl.ToHumanString(),
            });

        var msg = NotificationMessage.For(target.Channel, target.Address)
            .WithSubject(Subject)
            .WithTextBody(bodies.TextBody)
            .WithTextHtml(bodies.HtmlBody);

        try
        {
            await CodeService.SendAsync(msg, code, resolvedUserAccountId, ttl, cancellationToken).ConfigureAwait(false);

            if (Configuration.GetValue<bool>("Authentication:DeveloperMode"))
            {
                ctx.Set(BagKey.Qualify(Kind, "LastCode"), code);
            }

            return StepResult.Ok(Next);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Kind} send failed: {Message}", Kind, ex.Message);
            return StepResult.Fail(ex);
        }
    }

    private string BuildActionUrl(string clientUrl, string code, (string Field, string Value) selector)
    {
        var baseUrl = clientUrl.TrimEnd('/');
        var encodedCode = Uri.EscapeDataString(code);
        var path = Template.Equals("reset", StringComparison.OrdinalIgnoreCase)
            ? "reset-password"
            : "verify";
        var url = $"{baseUrl}/{path}?code={encodedCode}";

        if (selector.Field.Equals("Email", StringComparison.OrdinalIgnoreCase))
        {
            return url + $"&email={Uri.EscapeDataString(selector.Value)}";
        }

        if (selector.Field.Equals("PhoneNumber", StringComparison.OrdinalIgnoreCase)
            || selector.Field.Equals("Phone", StringComparison.OrdinalIgnoreCase))
        {
            return url + $"&phone={Uri.EscapeDataString(selector.Value)}";
        }

        return url;
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
