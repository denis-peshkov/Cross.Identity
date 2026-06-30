namespace Cross.Identity.ProcessEngine.Definitions.Providers;

/// <summary>
/// Process JSON definition provider that reads from a file-system folder.
/// Supports:
/// <list type="bullet">
/// <item>file name pattern: <c>{flow}.{operation}.json</c> (letters/digits/underscore/hyphen);</item>
/// <item>in-memory cache (ConcurrentDictionary);</item>
/// <item>hot reload via <see cref="FileSystemWatcher"/> (optional).</item>
/// </list>
/// </summary>
internal sealed class FileSystemProcessDefinitionProvider : IProcessDefinitionProvider, IDisposable
{
    private readonly IOptions<FileSystemProcessDefinitionOptions> _opt;

    private readonly string _flowRoot;
    private readonly FileSystemWatcher? _flowWatcher;
    private readonly ConcurrentDictionary<string, string> _flowCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex FlowFileNameRegex =
        new(@"(?i)^(?<flow>[a-z0-9_\-]+)\.(?<op>[a-z0-9_\-]+)\.json$", RegexOptions.Compiled);

    private readonly string _templateRoot;
    private readonly FileSystemWatcher? _templateWatcher;
    private readonly ConcurrentDictionary<string, string> _templateCache = new(StringComparer.OrdinalIgnoreCase);
    // Templates: name.lang.format (format: txt|html)
    private static readonly Regex TemplateFileNameRegex =
        new(@"(?i)^(?<name>[a-z0-9_\-]+)\.(?<lang>[a-z0-9_\-]+)\.(?<fmt>txt|html)$", RegexOptions.Compiled);

    /// <summary>
    /// Creates the provider.
    /// </summary>
    /// <param name="opt"></param>
    /// <exception cref="DirectoryNotFoundException">When the flows folder is missing.</exception>
    public FileSystemProcessDefinitionProvider(IOptions<FileSystemProcessDefinitionOptions> opt)
    {
        _opt = opt;

        // Flows root (required)
        _flowRoot = Path.GetFullPath(_opt.Value.Directory ?? throw new ArgumentNullException(nameof(_opt.Value.Directory)));
        if (!Directory.Exists(_flowRoot))
            throw new DirectoryNotFoundException($"Flows directory '{_flowRoot}' not found.");

        // Templates root (also required now)
        _templateRoot = ResolveTemplatesRoot(_flowRoot, "Templates");
        if (!Directory.Exists(_templateRoot))
            throw new DirectoryNotFoundException($"Templates directory '{_templateRoot}' not found.");

        // Indexing
        IndexFlowFiles();
        IndexTemplateFiles();

        // Watchers
        if (_opt.Value.ReloadOnChange)
        {
            _flowWatcher = new FileSystemWatcher(_flowRoot, "*.json")
            {
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            };
            _flowWatcher.Changed += OnFlowChanged;
            _flowWatcher.Created += OnFlowChanged;
            _flowWatcher.Renamed += OnFlowRenamed;
            _flowWatcher.Deleted += OnFlowDeleted;

            _templateWatcher = new FileSystemWatcher(_templateRoot)
            {
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                Filter = "*.*",
            };
            _templateWatcher.Changed += OnTemplateChanged;
            _templateWatcher.Created += OnTemplateChanged;
            _templateWatcher.Renamed += OnTemplateRenamed;
            _templateWatcher.Deleted += OnTemplateDeleted;
        }
    }

    /// <inheritdoc/>
    public string GetJson(string flow, FlowOperationEnum operation)
    {
        var key = $"{flow}.{operation}".ToLowerInvariant();

        if (_flowCache.TryGetValue(key, out var json))
            return json;

        // lazily try reading from disk (if not indexed yet)
        var file = Path.Combine(_flowRoot, $"{flow}.{operation}.json");
        if (File.Exists(file))
        {
            var txt = File.ReadAllText(file);
            _flowCache[key] = txt;
            return txt;
        }

        throw new KeyNotFoundException($"Process definition not found for '{key}' in '{_flowRoot}'.");
    }

    /// <inheritdoc/>
    public string GetTemplate(string name, string languageCode, string format)
    {
        var key = $"{name}.{languageCode}.{format}".ToLowerInvariant();

        if (_templateCache.TryGetValue(key, out var tpl))
            return tpl;

        var file = Path.Combine(_templateRoot, $"{name}.{languageCode}.{format}");
        if (File.Exists(file))
        {
            var text = File.ReadAllText(file);
            _templateCache[key] = text;
            return text;
        }

        throw new KeyNotFoundException(
            $"Template '{key}' not found in '{_templateRoot}'. Expected file '{name}.{languageCode}.{format}'.");
    }

    // ---- Indexing ----

    private void IndexFlowFiles()
    {
        foreach (var file in Directory.EnumerateFiles(_flowRoot, "*.json", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(file);
            var m = FlowFileNameRegex.Match(name);
            if (!m.Success) continue;

            var flow = m.Groups["flow"].Value.ToLowerInvariant();
            var op   = m.Groups["op"].Value.ToLowerInvariant();
            var key  = $"{flow}.{op}";

            var json = File.ReadAllText(file);
            _flowCache[key] = json;
        }
    }

    private void IndexTemplateFiles()
    {
        foreach (var file in Directory.EnumerateFiles(_templateRoot, "*.*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(file);
            var m = TemplateFileNameRegex.Match(name);
            if (!m.Success) continue;

            var nm  = m.Groups["name"].Value.ToLowerInvariant();
            var lng = m.Groups["lang"].Value.ToLowerInvariant();
            var fmt = m.Groups["fmt"].Value.ToLowerInvariant();

            var key = $"{nm}.{lng}.{fmt}";
            var text = File.ReadAllText(file);
            _templateCache[key] = text;
        }
    }

    // ---- Flow watchers ----

    private void OnFlowChanged(object? sender, FileSystemEventArgs e)
    {
        if (!IsFlowAcceptable(e.Name)) return;
        var key = FlowKeyFromFileName(e.Name!);
        // re-read (file may be locked by an editor; light retry logic)
        TryReloadWithRetry(e.FullPath, key, isTemplate: false);
    }

    private void OnFlowRenamed(object? sender, RenamedEventArgs e)
    {
        // remove the old key (if it was a valid name) and load the new one
        if (IsFlowAcceptable(e.OldName))
        {
            var oldKey = FlowKeyFromFileName(e.OldName!);
            _flowCache.TryRemove(oldKey, out _);
        }
        if (IsFlowAcceptable(e.Name))
        {
            var newKey = FlowKeyFromFileName(e.Name!);
            TryReloadWithRetry(e.FullPath, newKey, isTemplate: false);
        }
    }

    private void OnFlowDeleted(object? sender, FileSystemEventArgs e)
    {
        if (!IsFlowAcceptable(e.Name)) return;
        var key = FlowKeyFromFileName(e.Name!);
        _flowCache.TryRemove(key, out _);
    }

    // ---- Template watchers ----

    private void OnTemplateChanged(object? sender, FileSystemEventArgs e)
    {
        if (!IsTemplateAcceptable(e.Name)) return;
        var key = TemplateKeyFromFileName(e.Name!);
        TryReloadWithRetry(e.FullPath, key, isTemplate: true);
    }

    private void OnTemplateRenamed(object? sender, RenamedEventArgs e)
    {
        if (IsTemplateAcceptable(e.OldName))
        {
            var oldKey = TemplateKeyFromFileName(e.OldName!);
            _templateCache.TryRemove(oldKey, out _);
        }
        if (IsTemplateAcceptable(e.Name))
        {
            var newKey = TemplateKeyFromFileName(e.Name!);
            TryReloadWithRetry(e.FullPath, newKey, isTemplate: true);
        }
    }

    private void OnTemplateDeleted(object? sender, FileSystemEventArgs e)
    {
        if (!IsTemplateAcceptable(e.Name)) return;
        var key = TemplateKeyFromFileName(e.Name!);
        _templateCache.TryRemove(key, out _);
    }

    // ---- helpers ----

    private static string ResolveTemplatesRoot(string flowRoot, string templatesSubfolderOrAbsolute)
    {
        if (Path.IsPathRooted(templatesSubfolderOrAbsolute))
            return templatesSubfolderOrAbsolute;

        // 1) inside flowRoot
        var candidate1 = Path.Combine(flowRoot, templatesSubfolderOrAbsolute);
        if (Directory.Exists(candidate1)) return candidate1;

        // 2) sibling folder next to flowRoot (../Templates)
        var parent = Directory.GetParent(flowRoot)?.FullName;
        if (!string.IsNullOrEmpty(parent))
        {
            var candidate2 = Path.Combine(parent, templatesSubfolderOrAbsolute);
            if (Directory.Exists(candidate2)) return candidate2;
        }

        // if missing, return candidate1; constructor validates and throws DirectoryNotFoundException
        return candidate1;
    }

    private static bool IsFlowAcceptable(string? fileName)
        => fileName is not null && FlowFileNameRegex.IsMatch(fileName);

    private static string FlowKeyFromFileName(string fileName)
    {
        var m = FlowFileNameRegex.Match(fileName);
        var flow = m.Groups["flow"].Value.ToLowerInvariant();
        var op   = m.Groups["op"].Value.ToLowerInvariant();
        return $"{flow}.{op}";
    }

    private static bool IsTemplateAcceptable(string? fileName)
        => fileName is not null && TemplateFileNameRegex.IsMatch(fileName);

    private static string TemplateKeyFromFileName(string fileName)
    {
        var m = TemplateFileNameRegex.Match(fileName);
        var name = m.Groups["name"].Value.ToLowerInvariant();
        var lang = m.Groups["lang"].Value.ToLowerInvariant();
        var fmt  = m.Groups["fmt"].Value.ToLowerInvariant();
        return $"{name}.{lang}.{fmt}";
    }

    private void TryReloadWithRetry(string path, string key, bool isTemplate)
    {
        const int attempts = 5;
        for (int i = 0; i < attempts; i++)
        {
            try
            {
                var txt = File.ReadAllText(path);
                if (isTemplate)
                    _templateCache[key] = txt;
                else
                    _flowCache[key] = txt;
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }

        // on failure, invalidate so a subsequent Get... retries
        if (isTemplate)
            _templateCache.TryRemove(key, out _);
        else
            _flowCache.TryRemove(key, out _);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_flowWatcher is not null)
        {
            _flowWatcher.EnableRaisingEvents = false;
            _flowWatcher.Changed -= OnFlowChanged;
            _flowWatcher.Created -= OnFlowChanged;
            _flowWatcher.Renamed -= OnFlowRenamed;
            _flowWatcher.Deleted -= OnFlowDeleted;
            _flowWatcher.Dispose();
        }

        if (_templateWatcher is not null)
        {
            _templateWatcher.EnableRaisingEvents = false;
            _templateWatcher.Changed -= OnTemplateChanged;
            _templateWatcher.Created -= OnTemplateChanged;
            _templateWatcher.Renamed -= OnTemplateRenamed;
            _templateWatcher.Deleted -= OnTemplateDeleted;
            _templateWatcher.Dispose();
        }
    }
}
