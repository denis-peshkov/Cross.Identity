namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for sending a one-time code to the user.
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

    /// <summary>Default delivery channel when not inferred from selector field.</summary>
    public required ChannelEnum Channel { get; init; }

    /// <summary>Identity selector (bag keys for field name + value).</summary>
    public required Selector Selector { get; init; }

    /// <summary>Resolves preferred / OTP delivery channel from user endpoints.</summary>
    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }

    /// <summary>
    /// Optional key in <see cref="Bag"/> for a per-request TTL (for example, <c>"collectForm.Ttl"</c>).
    /// </summary>
    public string? TtlKey { get; init; }

    public IConfiguration Configuration { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var selector = Selector.Resolve(ctx);

        var userIdRaw = await UserService.GetUserIdByAsync(selector.Field, selector.Value, cancellationToken).ConfigureAwait(false);
        if (!Guid.TryParse(userIdRaw, out var userId) || userId == Guid.Empty)
            throw new NotFoundException("User not found.");

        var channel = await CommunicationEndpoints
            .ResolveOtpChannelAsync(userId, selector.Field, selector.Value, Channel, cancellationToken)
            .ConfigureAwait(false);
        if (!channel.SupportsOtp())
            throw new ValidationException("Provide an email or a phone number to send a code.");

        var ttl = ResolveTtl(ctx);

        var code = channel == ChannelEnum.Sms
            ? CodeGeneratorHelper.GenerateNumericCode()
            : CodeGeneratorHelper.GenerateCode();

        var msg = NotificationMessage.For(channel, selector.Value)
            .WithSubject("Verification Code");

        var clientUrl = Configuration["Authentication:ClientUrl"]
            ?? throw new InvalidOperationException("Authentication:ClientUrl is not configured.");

        var year = DateTime.UtcNow.Year.ToString();
        var verificationLink = $"{clientUrl}/reset-password?code={code}";
        var helpLink = $"{clientUrl}/reset-password?code={code}";
        var logoLink = $"{clientUrl}/reset-password?code={code}";

        string Replace(string s) => s
            .Replace("{{company}}", "Peshkov")
            .Replace("{{site}}", "peshkov.biz")
            .Replace("{{code}}", code)
            .Replace("{{verificationLink}}", $"{verificationLink}")
            .Replace("{{helpLink}}", $"{helpLink}")
            .Replace("{{logoLink}}", $"{logoLink}")
            .Replace("{{logoWidth}}", $"34")
            .Replace("{{logoHeight}}", $"34")
            .Replace("{{fullName}}", "Denis Peshkov")
            .Replace("{{expires}}", ttl.ToHumanString())
            .Replace("{{year}}", year)
            .Replace("{{supportEmail}}", "support@peshkov.biz")
        ;

        var textTemplate = ProcessDefinitionProvider.GetTemplate("verify", "en", "txt");
        var htmlTemplate = ProcessDefinitionProvider.GetTemplate("verify", "en", "html");

        var textBody = Replace(textTemplate);
        var htmlBody = Replace(htmlTemplate);

        msg = msg
            .WithTextBody(textBody)
            .WithTextHtml(htmlBody);

        try
        {
            await CodeService.SendAsync(msg, code, userIdRaw, ttl, cancellationToken).ConfigureAwait(false);

            var developerMode = Configuration.GetValue<bool>("Authentication:DeveloperMode");
            if (developerMode)
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

    private TimeSpan ResolveTtl(Bag ctx)
    {
        var ttlDefault = TimeSpan.FromMinutes(5);

        if (string.IsNullOrWhiteSpace(TtlKey))
            return ttlDefault;

        return ctx.Get<TimeSpan?>(BagKey.Qualify(Kind, TtlKey)) ?? ttlDefault;
    }
}
