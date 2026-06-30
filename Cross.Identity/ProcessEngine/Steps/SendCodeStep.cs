namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for sending a one-time code to the user.
/// <para>
/// Uses <see cref="ICodeService"/> to generate and send a code on the specified channel
/// (for example, email or phone).
/// </para>
/// Typical usage:
/// <list type="number">
///   <item>After the <c>collectForm</c> step, where Email/Phone are entered.</item>
///   <item>Before the <c>verifyCode</c> step, which validates the submitted code.</item>
/// </list>
/// Key rules:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/> — if relative (no dot), is read as <c>"{Kind}.{SelectorKey}"</c>;
///       to read data from another step, specify an absolute key such as <c>"other-step.Field"</c>.</description></item>
///   <item><description>For debugging, the code is stored in <c>"{Kind}.LastCode"</c> (remove in production if not needed).</description></item>
/// </list>
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

    /// <summary>Code delivery channel (for example, <c>"email"</c> or <c>"phone"</c>).</summary>
    public required ChannelEnum Channel { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> to read the destination address (email or phone) from.
    /// May be relative (qualified as <c>"{Kind}.{SelectorKey}"</c>) or absolute.
    /// </summary>
    public required string SelectorKey { get; init; }

    /// <summary>Code lifetime. Defaults to 5 minutes.</summary>
    public TimeSpan Ttl { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>User lookup settings: which field to search by (for example, "Email" or "Phone").</summary>
    public required ResolveBy ResolveBy { get; init; }

    public IConfiguration Configuration { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var destination = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));

        var userId = await UserService.GetUserIdByAsync(ResolveBy.Field, destination, cancellationToken).ConfigureAwait(false);

        var code = Channel == ChannelEnum.Sms
            ? CodeGeneratorHelper.GenerateNumericCode()
            : CodeGeneratorHelper.GenerateCode();

        var msg = NotificationMessage.For(Channel, destination)
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
            .Replace("{{expires}}", Ttl.ToHumanString())
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
            // The code must be stored and available for subsequent validation
            await CodeService.SendAsync(msg, code, userId, Ttl, cancellationToken).ConfigureAwait(false);

            var developerMode = Configuration.GetValue<bool>("Authentication:DeveloperMode");
            if (developerMode)
            {
                // For debugging/tests, store the last code
                ctx.Set(BagKey.Qualify(Kind, "LastCode"), code); // todo: not shown in the schema, not visible that it exists; maybe expose as an Output field collection?
            }

            return StepResult.Ok(Next);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Kind} send failed: {Message}", Kind, ex.Message);
            return StepResult.Fail(ex);
        }
    }
}
