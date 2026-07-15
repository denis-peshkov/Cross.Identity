namespace Cross.Identity.ProcessEngine.Definitions.Providers.Options;

/// <summary>
/// File-system definition provider options.
/// </summary>
internal sealed class FileSystemProcessDefinitionOptions
{
    /// <summary>Path to the folder with process files (required).</summary>
    public string Directory { get; set; }

    /// <summary>Whether to enable auto-reload via FileSystemWatcher. Defaults to true.</summary>
    public bool ReloadOnChange { get; set; } = true;
}
