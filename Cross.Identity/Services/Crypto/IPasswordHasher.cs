namespace Cross.Identity.Services.Crypto;

public interface IPasswordHasher
{

    /// <summary>
    /// Returns a PHC string: $argon2id$... or $pbkdf2-sha256$... or $sha256$...
    /// </summary>
    string Hash(string password, string pepper);

    /// <summary>
    /// Verifies a password against a stored PHC string; supports re-hashing.
    /// </summary>
    PasswordVerificationEnum Verify(string password, string phc, string pepper);

    /// <summary>
    /// Whether the hash should be recomputed (e.g. when parameters are increased).
    /// </summary>
    bool NeedsRehash(string phc);
}
