namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step for sending a one-time code to the user.
/// <para>
/// Uses <see cref="ICodeService"/> to generate and send a code on the specified channel
/// (for example, email or phone).
/// </para>
/// Typical usage:
/// <list type="number">
///   <item>After the <c>collectForm</c> step, where Email/PhoneNumber are entered.</item>
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

    /// <summary>Code delivery channel (<see cref="ChannelEnum.Email"/> / <see cref="ChannelEnum.Sms"/>, …).</summary>
    public required ChannelEnum Channel { get; init; }

    /// <summary>
    /// Key in <see cref="Bag"/> to read the destination address (email or phone) from.
    /// May be relative (qualified as <c>"{Kind}.{SelectorKey}"</c>) or absolute.
    /// </summary>
    public required string SelectorKey { get; init; }

    /// <summary>
    /// Optional key for phone (E.164). When set with <see cref="UserNameKey"/>, destination is resolved as email / phone / user name.
    /// </summary>
    public string? PhoneNumberKey { get; init; }

    /// <summary>
    /// Optional key for user name. OTP delivery requires email or phone (not user name alone).
    /// </summary>
    public string? UserNameKey { get; init; }

    /// <summary>
    /// Optional key in <see cref="Bag"/> for a per-request TTL (for example, <c>"collectForm.Ttl"</c>).
    /// When set and present, overrides the default 5-minute lifetime.
    /// </summary>
    public string? TtlKey { get; init; }

    /// <summary>User lookup settings: which field to search by (for example, "Email" or "PhoneNumber").</summary>
    public required ResolveBy ResolveBy { get; init; }

    public IConfiguration Configuration { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        string destination;
        ChannelEnum channel;
        ResolveBy resolveBy;
        if (EmailOrPhoneBag.IsMultiSelector(PhoneNumberKey, UserNameKey))
        {
            string field;
            ChannelEnum? resolvedChannel;
            (field, destination, resolvedChannel) = EmailOrPhoneBag.Resolve(ctx, Kind, SelectorKey, PhoneNumberKey, UserNameKey);
            if (resolvedChannel is null)
                throw new ValidationException("Provide an email or a phone number to send a code.");
            channel = resolvedChannel.Value;
            resolveBy = new ResolveBy { Field = field, Required = ResolveBy.Required, CaseInsensitive = ResolveBy.CaseInsensitive };
        }
        else
        {
            destination = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));
            channel = Channel;
            resolveBy = ResolveBy;
        }

        var ttl = ResolveTtl(ctx);

        var userId = await UserService.GetUserIdByAsync(resolveBy.Field, destination, cancellationToken).ConfigureAwait(false);

        var code = channel == ChannelEnum.Sms
            ? CodeGeneratorHelper.GenerateNumericCode()
            : CodeGeneratorHelper.GenerateCode();

        var msg = NotificationMessage.For(channel, destination)
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
            // The code must be stored and available for subsequent validation
            await CodeService.SendAsync(msg, code, userId, ttl, cancellationToken).ConfigureAwait(false);

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

    private TimeSpan ResolveTtl(Bag ctx)
    {
        var ttlDefault = TimeSpan.FromMinutes(5);

        if (string.IsNullOrWhiteSpace(TtlKey))
            return ttlDefault;

        return ctx.TryGet(BagKey.Qualify(Kind, TtlKey), out TimeSpan? ttl) && ttl is not null
            ? ttl.Value
            : ttlDefault;
    }
}
