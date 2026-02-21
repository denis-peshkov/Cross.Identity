namespace Cross.PepperVault.Options;

public sealed record EnvProviderOptions
{

    /// <summary>Текущая версия перца (обязательна).</summary>
    public short Current { get; set; }

    /// <summary>Словарь версий → строка перца (обязателен; должен содержать ключ Current).</summary>
    public Dictionary<short, string> Peppers { get; set; } = new();
}
