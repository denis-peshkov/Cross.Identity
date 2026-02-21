namespace Cross.Identity.ProcessEngine.Definitions.Providers;

/// <summary>
/// Провайдер, читающий JSON-дефиниции процессов из <b>Embedded Resources</b> указанной сборки.
/// <para>
/// Конвенция: каждый файл называется <c>{flow}.{operation}.json</c> и публикуется как embedded-ресурс
/// под неймспейсом <see cref="_baseNamespace"/>. Пример полного имени ресурса:
/// <c>MyCompany.MyApp.Flows.Definitions.licenses.getuser.json</c>.
/// </para>
/// </summary>
public sealed class EmbeddedResourceProcessDefinitionProvider : IProcessDefinitionProvider
{
    private readonly IOptions<EmbeddedProcessDefinitionOptions> _opt;

    private readonly Dictionary<string, string> _flows = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _templates = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    /// <summary>
    /// Регэксп для извлечения <c>flow</c> и <c>operation</c> из хвоста имени ресурса.
    /// Разрешены буквы/цифры/подчёркивание/дефис.
    /// </summary>
    private static readonly Regex FlowKeyRegex =
        new(@"(?i)(?<flow>[a-z0-9_\-]+)\.(?<op>[a-z0-9_\-]+)\.json$", RegexOptions.Compiled);

    /// <summary>
    /// Templates.name.lang.format  (где format: txt|html)
    /// </summary>
    private static readonly Regex TemplateKeyRegex =
        new(@"(?i)(?<name>[a-z0-9_\-]+)\.(?<lang>[a-z0-9_\-]+)\.(?<fmt>txt|html)$", RegexOptions.Compiled);

    /// <summary>
    /// Создаёт провайдер.
    /// </summary>
    /// <param name="opt"></param>
    /// <exception cref="InvalidOperationException">Если ни один JSON не найден под указанным namespace.</exception>
    public EmbeddedResourceProcessDefinitionProvider(IOptions<EmbeddedProcessDefinitionOptions> opt)
    {
        _opt = opt;

        IndexResources();
    }

    /// <inheritdoc />
    public string GetJson(string flow, FlowOperationEnum operation)
    {
        ArgumentException.ThrowIfNullOrEmpty(flow);

        var key = $"{flow}.{operation}".ToLowerInvariant(); // пример: "licenses.getuser"
        lock (_lock)
        {
            if (_flows.TryGetValue(key, out var json))
                return json;
        }

        throw new KeyNotFoundException($"Process definition not found for '{key}'. Base namespace: '{_opt.Value.BaseNamespace}.Flows'.");
    }

    /// <inheritdoc />
    public string GetTemplate(string name, string languageCode, string format)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(languageCode);
        ArgumentException.ThrowIfNullOrEmpty(format);

        var key = $"{name}.{languageCode}.{format}".ToLowerInvariant(); // e.g. verify-code.ru.txt
        lock (_lock)
        {
            if (_templates.TryGetValue(key, out var tpl))
                return tpl;
        }

        throw new KeyNotFoundException($"Template not found for '{key}'. Base namespace: '{_opt.Value.BaseNamespace}.Templates'.");
    }

    /// <summary>
    /// Индексирует embedded-ресурсы:
    /// - Flows: {BaseNamespace}.{flow}.{op}.json
    /// - Templates: {BaseNamespace}.Templates.{name}.{lang}.{fmt}
    /// </summary>
    private void IndexResources()
    {
        var flowPrefix = _opt.Value.BaseNamespace + ".Flows";
        var tplPrefix = _opt.Value.BaseNamespace + ".Templates";

        foreach (var fullName in _opt.Value.Assembly.GetManifestResourceNames())
        {
            if (!fullName.StartsWith(flowPrefix, StringComparison.Ordinal) && !fullName.StartsWith(tplPrefix, StringComparison.Ordinal))
                continue;

            // хвост после base namespace (без ведущей точки)
            var flowTail = fullName.Substring(flowPrefix.Length).TrimStart('.'); // e.g. "game.auth.json" или "license.Register.json"

            // 1) Flows
            var flowMatch = FlowKeyRegex.Match(flowTail);
            if (flowMatch.Success)
            {
                var flow = flowMatch.Groups["flow"].Value.ToLowerInvariant();
                var op = flowMatch.Groups["op"].Value.ToLowerInvariant();
                var key = $"{flow}.{op}";

                var content = ReadResource(fullName);
                lock (_lock)
                    _flows[key] = content;
                continue;
            }

            // хвост после base namespace (без ведущей точки)
            var templateTail = fullName.Substring(tplPrefix.Length).TrimStart('.'); // e.g. "register.en.html" или "verify.ru.txt"

            // 2) Templates
            var tplMatch = TemplateKeyRegex.Match(templateTail);
            if (tplMatch.Success)
            {
                var name = tplMatch.Groups["name"].Value.ToLowerInvariant();
                var lang = tplMatch.Groups["lang"].Value.ToLowerInvariant();
                var fmt = tplMatch.Groups["fmt"].Value.ToLowerInvariant();
                var key = $"{name}.{lang}.{fmt}";

                var content = ReadResource(fullName);
                lock (_lock)
                    _templates[key] = content;
                continue;
            }
        }

        if (_flows.Count == 0 && _templates.Count == 0)
        {
            throw new InvalidOperationException(
                $"No resources found under '{_opt.Value.BaseNamespace}'. " +
                $"Ensure your .csproj embeds files and LogicalName/namespace matches.");
        }
    }

    private string ReadResource(string fullName)
    {
        using var s = _opt.Value.Assembly.GetManifestResourceStream(fullName)
                      ?? throw new InvalidOperationException($"Cannot open resource '{fullName}'.");
        using var r = new StreamReader(s, Encoding.UTF8);
        return r.ReadToEnd();
    }
}
