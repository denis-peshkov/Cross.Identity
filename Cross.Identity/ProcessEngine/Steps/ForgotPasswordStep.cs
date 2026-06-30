namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that issues a JWT token via an application MediatR command
/// <c>TokenCommand(string email, string password)</c>.
/// <para>
/// Keys:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/> and <see cref="PasswordKey"/>:
///     if the key is relative (no dot), it is read as <c>"{Name}.{Key}"</c>;
///     to read data from another step, specify an absolute key such as <c>"other-step.Field"</c>.</description></item>
///   <item><description><see cref="ResultKey"/> — if relative, is written as <c>"{Name}.{ResultKey}"</c>.</description></item>
/// </list>
/// </para>
/// The handler result is expected to contain a string property <c>AccessToken</c>
/// (or <c>Token</c>), or be a string itself. The value is written to <see cref="Bag"/>.
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

    /// <summary>Key in <see cref="Bag"/> to read e-mail/login from. May be relative or absolute.</summary>
    public required string SelectorKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the password from. May be relative or absolute.</summary>
    public required string PasswordKey { get; init; }

    /// <summary>Code lifetime. Defaults to 5 minutes.</summary>
    public TimeSpan Ttl { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Code delivery channel (for example, <c>"email"</c> or <c>"phone"</c>).</summary>
    public required ChannelEnum Channel { get; set; }

    public ResolveBy ResolveBy { get; set; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // 1) read email or phoneNumber (respecting relative/absolute keys)
        var selectorValue = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));

        var code = Channel == ChannelEnum.Sms
            ? CodeGeneratorHelper.GenerateNumericCode()
            : CodeGeneratorHelper.GenerateCode();

        var clientUrl = "http://localhost:4000";

        var msg = NotificationMessage.For(Channel, selectorValue)
            .WithSubject("Reset your password")
            .WithTextBody($"Please reset your password by clicking <a href=''>here</a>.");

        var year = DateTime.UtcNow.Year.ToString();

        var url = $"{clientUrl}/reset-password?code={code}";
        switch (Channel)
        {
            case ChannelEnum.Email:
                url += $"&email={selectorValue}";
                break;
            case ChannelEnum.Sms:
                url += $"&phone={selectorValue}";
                break;
            default:
                break;
        }

        string Replace(string s) => s
            .Replace("{{email}}", selectorValue)
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
                // store/send via the service
                await CodeService.SendAsync(msg, code, "", Ttl, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // delete after email setup
            Logger.LogError(ex, "{Kind} send failed: {Message}", Kind, ex.Message);
        }

        // For debugging/tests, store the last code
        ctx.Set(BagKey.Qualify(Kind, "LastCode"), code); // todo: not shown in the schema, not visible that it exists; maybe expose as an Output field collection

        return StepResult.Ok(Next);
    }
}
