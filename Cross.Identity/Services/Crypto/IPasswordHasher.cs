namespace Cross.Identity.Services.Crypto;

internal interface IPasswordHasher
{

    /// <summary>
    /// Возвращает PHC-строку: $argon2id$... или $pbkdf2-sha256$... или $sha256$...
    /// </summary>
    string Hash(string password, string pepper);

    /// <summary>
    /// Проверяет пароль по сохранённой PHC-строке, поддерживает «переучивание» (re-hash).
    /// </summary>
    PasswordVerificationEnum Verify(string password, string phc, string pepper);

    /// <summary>
    /// Нужно ли заново захешировать (например, при повышении параметров).
    /// </summary>
    bool NeedsRehash(string phc);
}
