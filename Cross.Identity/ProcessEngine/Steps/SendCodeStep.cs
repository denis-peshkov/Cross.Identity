namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for sending a one-time code to the user.
/// Delivery channel/address come from <see cref="ICommunicationEndpointService.ResolveOtpTargetAsync"/>.
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
    public required IProcessDefinitionProvider ProcessDefinitionProvider { get; init; }
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
    public required string Template { get; init; }

    /// <summary>Notification subject line. Defaults to <c>Verification Code</c>.</summary>
    public required string Subject { get; init; }

    public IConfiguration Configuration { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);

        string userIdRaw;
        try
        {
            userIdRaw = await UserService.GetUserIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        }
        catch (NotFoundException ex)
        {
            Logger.LogInformation(
                "Send code rejected for {Field} identity {Identity}: {Reason}",
                selector.Field,
                selector.Value,
                ex.Message);
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        if (!Guid.TryParse(userIdRaw, out var userId) || userId == Guid.Empty)
        {
            Logger.LogInformation(
                "Send code rejected for {Field} identity {Identity}: resolved user id is missing or invalid.",
                selector.Field,
                selector.Value);
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        var target = await CommunicationEndpoints
            .ResolveOtpTargetAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (!target.Channel.SupportsOtp())
        {
            throw new ValidationException("Provide an email or a phone number to send a code.");
        }

        var ttl = ResolveTtl(ctx);
        var code = target.Channel.GenerateCode();

        var clientUrl = Configuration["Authentication:ClientUrl"]
            ?? throw new InvalidOperationException("Authentication:ClientUrl is not configured.");

        var actionUrl = BuildActionUrl(clientUrl, code, selector);
        var year = DateTime.UtcNow.Year.ToString();
        const string support = "support@peshkov.biz";
        const string brand = "peshkov.biz";

        string Replace(string s) => s
            .Replace("{{company}}", "Peshkov")
            .Replace("{{site}}", brand)
            .Replace("{{brand}}", brand)
            .Replace("{{email}}", selector.Value)
            .Replace("{{code}}", code)
            .Replace("{{url}}", actionUrl)
            .Replace("{{verificationLink}}", actionUrl)
            .Replace("{{helpLink}}", actionUrl)
            .Replace("{{logoLink}}", actionUrl)
            .Replace("{{imageLink}}", actionUrl)
            .Replace("{{logoWidth}}", "34")
            .Replace("{{logoHeight}}", "34")
            .Replace("{{imageWidth}}", "34")
            .Replace("{{imageHeight}}", "34")
            .Replace("{{fullName}}", "Denis Peshkov")
            .Replace("{{expires}}", ttl.ToHumanString())
            .Replace("{{year}}", year)
            .Replace("{{support}}", support)
            .Replace("{{supportEmail}}", support);

        var textTemplate = ProcessDefinitionProvider.GetTemplate(Template, "en", "txt");
        var htmlTemplate = ProcessDefinitionProvider.GetTemplate(Template, "en", "html");

        var msg = NotificationMessage.For(target.Channel, target.Address)
            .WithSubject(Subject)
            .WithTextBody(Replace(textTemplate))
            .WithTextHtml(Replace(htmlTemplate));

        try
        {
            await CodeService.SendAsync(msg, code, userIdRaw, ttl, cancellationToken).ConfigureAwait(false);

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
        var url = $"{clientUrl.TrimEnd('/')}/reset-password?code={Uri.EscapeDataString(code)}";

        // Reset links need identity in the query; verify/register keep code-only URLs.
        if (!Template.Equals("reset", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

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
