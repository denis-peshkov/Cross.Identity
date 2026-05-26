namespace Cross.Identity.ProcessEngine.Definitions.Providers.Options;

/// <summary>
/// Опции файлового провайдера дефиниций.
/// </summary>
internal sealed class FileSystemProcessDefinitionOptions
{
    /// <summary>Путь к папке с файлами процессов (обязателен).</summary>
    public string Directory { get; set; }

    /// <summary>Включать ли авто-перезагрузку через FileSystemWatcher. По умолчанию true.</summary>
    public bool ReloadOnChange { get; set; } = true;
}
