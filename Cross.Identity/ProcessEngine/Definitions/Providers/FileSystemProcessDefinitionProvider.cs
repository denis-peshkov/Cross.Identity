namespace Cross.Identity.ProcessEngine.Definitions.Providers;

/// <summary>
/// Провайдер JSON-дефиниций процессов, читающий их из папки файловой системы.
/// Поддерживает:
/// <list type="bullet">
/// <item>маску имени файла: <c>{flow}.{operation}.json</c> (буквы/цифры/подчёркивание/дефис);</item>
/// <item>кэш в памяти (ConcurrentDictionary);</item>
/// <item>горячую перезагрузку через <see cref="FileSystemWatcher"/> (опционально).</item>
/// </list>
/// </summary>
public sealed class FileSystemProcessDefinitionProvider : IProcessDefinitionProvider, IDisposable
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
    /// Создаёт провайдер.
    /// </summary>
    /// <param name="opt"></param>
    /// <exception cref="DirectoryNotFoundException">Если папка flows отсутствует.</exception>
    public FileSystemProcessDefinitionProvider(IOptions<FileSystemProcessDefinitionOptions> opt)
    {
        _opt = opt;

        // Flows root (обязательно)
        _flowRoot = Path.GetFullPath(_opt.Value.Directory ?? throw new ArgumentNullException(nameof(_opt.Value.Directory)));
        if (!Directory.Exists(_flowRoot))
            throw new DirectoryNotFoundException($"Flows directory '{_flowRoot}' not found.");

        // Templates root (тоже обязательно теперь)
        _templateRoot = ResolveTemplatesRoot(_flowRoot, "Templates");
        if (!Directory.Exists(_templateRoot))
            throw new DirectoryNotFoundException($"Templates directory '{_templateRoot}' not found.");

        // Индексация
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

        // лениво попробуем считать с диска (если не проиндексировали ранее)
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
        // читаем заново (файл может быть занят редактором; добавим лёгкую ретри-логику)
        TryReloadWithRetry(e.FullPath, key, isTemplate: false);
    }

    private void OnFlowRenamed(object? sender, RenamedEventArgs e)
    {
        // удалим старый ключ (если был валидным именем) и загрузим новый
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

        // 1) внутри flowRoot
        var candidate1 = Path.Combine(flowRoot, templatesSubfolderOrAbsolute);
        if (Directory.Exists(candidate1)) return candidate1;

        // 2) соседняя папка рядом с flowRoot (../Templates)
        var parent = Directory.GetParent(flowRoot)?.FullName;
        if (!string.IsNullOrEmpty(parent))
        {
            var candidate2 = Path.Combine(parent, templatesSubfolderOrAbsolute);
            if (Directory.Exists(candidate2)) return candidate2;
        }

        // если не существует — вернём candidate1; конструктор проверит и уронит с DirectoryNotFoundException
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

        // если не удалось — инвалидируем, чтобы последующий Get... попробовал ещё раз
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
