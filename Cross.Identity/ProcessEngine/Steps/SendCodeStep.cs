namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг отправки одноразового кода пользователю.
/// <para>
/// Использует <see cref="ICodeService"/> для генерации и отправки кода на указанный канал
/// (например, email или телефон).
/// </para>
/// Обычно используется:
/// <list type="number">
///   <item>После шага <c>collectForm</c>, где введены Email/Phone.</item>
///   <item>Перед шагом <c>verifyCode</c>, который проверит предъявленный код.</item>
/// </list>
/// Правила ключей:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/> — если относительный (без точки), читается как <c>"{Kind}.{SelectorKey}"</c>;
///       чтобы брать данные из другого шага, укажи абсолютный ключ вида <c>"other-step.Field"</c>.</description></item>
///   <item><description>Для отладки код сохраняется в <c>"{Kind}.LastCode"</c> (убери при проде, если не нужно).</description></item>
/// </list>
/// </summary>
internal sealed class SendCodeStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Сервис кодов (отправка/проверка).</summary>
    public required ICodeService CodeService { get; init; }
    public required IUserService UserService { get; init; }
    public required IHostEnvironment Environment { get; init; }
    public required IProcessDefinitionProvider ProcessDefinitionProvider { get; init; }
    public required ILogger Logger { get; init; }

    /// <summary>Канал доставки кода (например, <c>"email"</c> или <c>"phone"</c>).</summary>
    public required ChannelEnum Channel { get; init; }

    /// <summary>
    /// Ключ в <see cref="Bag"/>, откуда взять адрес назначения (email или телефон).
    /// Может быть относительным (будет квалифицирован как <c>"{Kind}.{SelectorKey}"</c>) или абсолютным.
    /// </summary>
    public required string SelectorKey { get; init; }

    /// <summary>Время жизни кода. По умолчанию 5 минут.</summary>
    public TimeSpan Ttl { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Настройки поиска пользователя: по какому полю искать (например, "Email" или "Phone").</summary>
    public required ResolveBy ResolveBy { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var destination = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));

        var userId = await UserService.GetUserIdByAsync(ResolveBy.Field, destination, cancellationToken);

        var code = Channel == ChannelEnum.Sms
            ? CodeGeneratorHelper.GenerateNumericCode()
            : CodeGeneratorHelper.GenerateCode();

        var msg = NotificationMessage.For(Channel, destination)
            .WithSubject("Verification Code");

        var clientUrl = "http://localhost:4000";
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
            if (Environment.IsDevelopment())
            {
                await CodeService.SendAsync(msg, code, userId, Ttl, cancellationToken); // todo: remove this row!!!

                // Для отладки/тестов сохраняем последний код
                ctx.Set(BagKey.Qualify(Kind, "LastCode"), code); // todo: не отображается в схеме, не видно что оно есть, может отображать как коллекцию полей Output?
            }
            else
            {
                // сохраняем/отправляем через сервис
                await CodeService.SendAsync(msg, code, userId, Ttl, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // delete after email setup
            Logger.LogError(ex, "{Kind} send failed: {Message}", Kind, ex.Message);
        }

        return StepResult.Ok(Next);
    }
}
