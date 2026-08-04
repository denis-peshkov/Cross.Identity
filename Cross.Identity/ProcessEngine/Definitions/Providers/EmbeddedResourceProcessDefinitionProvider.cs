namespace Cross.Identity.ProcessEngine.Definitions.Providers;

/// <summary>
/// Provider that reads process JSON definitions from <b>Embedded Resources</b> of the specified assembly.
/// <para>
/// Convention: each file is named <c>{flow}.{operation}.json</c> and published as an embedded resource
/// under namespace <see cref="_baseNamespace"/>. Example full resource name:
/// <c>MyCompany.MyApp.Flows.Definitions.licenses.getuser.json</c>.
/// </para>
/// </summary>
internal sealed class EmbeddedResourceProcessDefinitionProvider : IProcessDefinitionProvider
{
    private readonly IOptions<EmbeddedProcessDefinitionOptions> _opt;

    private readonly Dictionary<string, string> _flows = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _templates = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    /// <summary>
    /// Regex to extract <c>flow</c> and <c>operation</c> from the resource name tail.
    /// Letters/digits/underscore/hyphen are allowed.
    /// </summary>
    private static readonly Regex FlowKeyRegex =
        new(@"(?i)(?<flow>[a-z0-9_\-]+)\.(?<op>[a-z0-9_\-]+)\.json$", RegexOptions.Compiled);

    /// <summary>
    /// Templates.name.lang.format  (format: txt|html)
    /// </summary>
    private static readonly Regex TemplateKeyRegex =
        new(@"(?i)(?<name>[a-z0-9_\-]+)\.(?<lang>[a-z0-9_\-]+)\.(?<fmt>txt|html)$", RegexOptions.Compiled);

    /// <summary>
    /// Creates the provider.
    /// </summary>
    /// <param name="opt"></param>
    /// <exception cref="InvalidOperationException">When no JSON is found under the specified namespace.</exception>
    public EmbeddedResourceProcessDefinitionProvider(IOptions<EmbeddedProcessDefinitionOptions> opt)
    {
        _opt = opt;

        IndexResources();
    }

    /// <inheritdoc />
    public string GetJson(string flow, FlowOperationEnum operation)
    {
        ArgumentException.ThrowIfNullOrEmpty(flow);

        var key = $"{flow}.{operation}".ToLowerInvariant(); // example: "licenses.getuser"
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
    /// Indexes embedded resources:
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

            // tail after base namespace (without leading dot)
            var flowTail = fullName.Substring(flowPrefix.Length).TrimStart('.'); // e.g. "license.Register.json"

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

            // tail after base namespace (without leading dot)
            var templateTail = fullName.Substring(tplPrefix.Length).TrimStart('.'); // e.g. "register.en.html" or "verify.ru.txt"

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
