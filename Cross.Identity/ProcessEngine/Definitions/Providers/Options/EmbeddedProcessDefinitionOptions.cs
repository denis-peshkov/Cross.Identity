namespace Cross.Identity.ProcessEngine.Definitions.Providers.Options;

/// <summary>
/// Опции загрузки JSON-дефиниций из embedded-ресурсов.
/// Используются при DI-регистрации провайдера.
/// </summary>
internal sealed class EmbeddedProcessDefinitionOptions
{
    /// <summary>
    /// Имя сборки, из которой читаются ресурсы (например, "MyCompany.MyApp").
    /// </summary>
    public string? AssemblyName
    {
        get => Assembly?.GetName().Name;
        set => Assembly = !string.IsNullOrWhiteSpace(value)
            ? Assembly.Load(value)
            : Assembly.GetExecutingAssembly();
    }

    /// <summary>
    /// Сборка, из которой читаются ресурсы.
    /// </summary>
    [JsonIgnore] // чтобы не сериализовалось обратно в JSON
    public Assembly Assembly { get; set; } = Assembly.GetExecutingAssembly();

    /// <summary>
    /// Базовый namespace, под которым публикуются JSON-ресурсы.
    /// Например: <c>"MyCompany.MyApp.Flows.Definitions"</c>.
    /// </summary>
    public string BaseNamespace { get; set; } = null!;
}
