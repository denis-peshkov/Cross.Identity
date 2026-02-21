namespace Cross.PepperVault.Providers;

/// <summary>
/// Провайдер «перцев» для хэширования паролей.
/// Позволяет хранить несколько версий, ротацию и проверку совместимости.
/// </summary>
public interface IPepperVaultProvider
{
    /// <summary>Текущая активная версия перца.</summary>
    short CurrentVersion { get; }

    /// <summary>Все доступные версии перцев.</summary>
    IReadOnlyDictionary<short, string> Peppers { get; }

    /// <summary>Попытаться получить текущую активную версию перца.</summary>
    bool TryGetCurrentVersion([MaybeNullWhen(false)] out string pepper);

    /// <summary>Попытаться получить перец нужной версии.</summary>
    /// <param name="version">Версия перца (например, "v1").</param>
    /// <param name="pepper">Секретный перец.</param>
    /// <returns>Результат попытки получения перца.</returns>
    bool TryGet(short version, [MaybeNullWhen(false)] out string pepper);

    /// <summary>Перечитать секреты из хранилища (сбросить кэш).</summary>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}
