namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг выпуска JWT-токена через MediatR-команду приложения
/// <c>TokenCommand(string email, string password)</c>.
/// <para>
/// Ключи:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/> и <see cref="PasswordKey"/>:
///     если ключ относительный (без точки), читается как <c>"{Name}.{Key}"</c>;
///     чтобы читать данные из другого шага, укажи абсолютный ключ вида <c>"other-step.Field"</c>.</description></item>
///   <item><description><see cref="ResultKey"/> — если ключ относительный, записывается как <c>"{Name}.{ResultKey}"</c>.</description></item>
/// </list>
/// </para>
/// Ожидается, что результат обработчика содержит строковое свойство <c>AccessToken</c>
/// (или <c>Token</c>), либо сам является строкой. Значение будет записано в <see cref="Bag"/>.
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

    /// <summary>Ключ в <see cref="Bag"/>, откуда взять e-mail/логин. Может быть относительным или абсолютным.</summary>
    public required string SelectorKey { get; init; }

    /// <summary>Ключ в <see cref="Bag"/>, откуда взять пароль. Может быть относительным или абсолютным.</summary>
    public required string PasswordKey { get; init; }

    /// <summary>Время жизни кода. По умолчанию 5 минут.</summary>
    public TimeSpan Ttl { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Канал доставки кода (например, <c>"email"</c> или <c>"phone"</c>).</summary>
    public required ChannelEnum Channel { get; set; }

    public ResolveBy ResolveBy { get; set; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // 1) достаём email или phoneNumber (с учётом относительных/абсолютных ключей)
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
                // сохраняем/отправляем через сервис
                await CodeService.SendAsync(msg, code, "", Ttl, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // delete after email setup
            Logger.LogError(ex, "{Kind} send failed: {Message}", Kind, ex.Message);
        }

        // Для отладки/тестов сохраняем последний код
        ctx.Set(BagKey.Qualify(Kind, "LastCode"), code); // todo: не отображается в схеме, не видно что оно есть, может отображать как коллекцию полей Output

        return StepResult.Ok(Next);
    }
}
