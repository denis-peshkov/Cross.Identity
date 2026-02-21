namespace Cross.Identity.Services.Crypto;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum PasswordAlgoEnum
{
    Argon2id,
    PBKDF2,
    [Obsolete( "SHA256 has very low security.")]
    SHA256,
}
