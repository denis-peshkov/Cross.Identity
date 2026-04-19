namespace Cross.Identity.Services.Crypto;

public interface IPhoneNormalizer
{
    string NormalizePhone(string phoneRaw);

    /// <summary>Нормализует строку телефона к E.164 (например, +40722123456).</summary>
    /// <param name="raw">Исходный ввод (может быть с пробелами, скобками и т.д.).</param>
    /// <param name="defaultRegion">Двухбуквенный ISO-код региона (например, "RO", "UA", "RU").</param>
    /// <returns>E.164 или null, если номер некорректен.</returns>
    string? NormalizeToE164(string raw, string defaultRegion);

    /// <summary>Бросает исключение, если номер некорректен.</summary>
    string NormalizeToE164OrThrow(string raw, string defaultRegion);
}
