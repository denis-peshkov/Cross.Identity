namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that sends a password-reset code.
/// </summary>
internal sealed class ForgotPasswordStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required ILogger Logger { get; init; }
    public required ICodeService CodeService { get; init; }
    public required IConfiguration Configuration { get; init; }
    public required IHostEnvironment Environment { get; init; }
    public required IProcessDefinitionProvider ProcessDefinitionProvider { get; init; }

    /// <summary>Identity selector (bag keys for field name + value).</summary>
    public required Selector Selector { get; init; }

    /// <summary>Code lifetime. Defaults to 5 minutes.</summary>
    public TimeSpan Ttl { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Default delivery channel when not inferred from selector field.</summary>
    public required ChannelEnum Channel { get; set; }

    public required IUserService UserService { get; init; }

    public required ICommunicationEndpointService CommunicationEndpoints { get; init; }

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
            throw new ValidationException("Provide an email or a phone number to reset a password.");

        var code = channel == ChannelEnum.Sms
            ? CodeGeneratorHelper.GenerateNumericCode()
            : CodeGeneratorHelper.GenerateCode();

        var clientUrl = "http://localhost:4000";

        var msg = NotificationMessage.For(channel, selector.Value)
            .WithSubject("Reset your password")
            .WithTextBody($"Please reset your password by clicking <a href=''>here</a>.");

        var year = DateTime.UtcNow.Year.ToString();

        var url = $"{clientUrl}/reset-password?code={code}";
        switch (channel)
        {
            case ChannelEnum.Email:
                url += $"&email={selector.Value}";
                break;
            case ChannelEnum.Sms:
                url += $"&phone={selector.Value}";
                break;
            default:
                break;
        }

        string Replace(string s) => s
            .Replace("{{email}}", selector.Value)
            .Replace("{{code}}", code)
            .Replace("{{expires}}", Ttl.ToString())
            .Replace("{{url}}", $"{url}")
            .Replace("{{support}}", "")
            .Replace("{{year}}", year)
            .Replace("{{brand}}", "peshkov.biz");

        var textTemplate = ProcessDefinitionProvider.GetTemplate("reset", "en", "txt");
        var htmlTemplate = ProcessDefinitionProvider.GetTemplate("reset", "en", "html");

        var textBody = Replace(textTemplate);
        var htmlBody = Replace(htmlTemplate);

        msg = msg
            .WithTextBody(textBody)
            .WithTextHtml(htmlBody);

        try
        {
            if (!Environment.IsDevelopment())
            {
                await CodeService.SendAsync(msg, code, userIdRaw, Ttl, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Kind} send failed: {Message}", Kind, ex.Message);
        }

        var developerMode = Configuration.GetValue<bool>("Authentication:DeveloperMode");
        if (developerMode)
        {
            ctx.Set(BagKey.Qualify(Kind, "LastCode"), code);
        }

        return StepResult.Ok(Next);
    }
}
