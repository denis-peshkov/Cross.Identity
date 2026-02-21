namespace Cross.Identity.Services.Crypto;

internal sealed class PasswordHasherOptions /*<TAlgorithmOptions>*/
{
    // Общие
    public int SaltSizeBytes { get; set; } = 16;     // длина создаваемой соли
    public int HashOutputBytes { get; set; } = 32;   // длина хеша
    public PasswordAlgoEnum DefaultAlgorithm { get; set; } = PasswordAlgoEnum.Argon2id; // По умолчанию используем Argon2id

    // public required TAlgorithmOptions AlgorithmOptions { get; init; }

    // Argon2id
    public int Argon2_Iterations { get; set; } = 3;       // t
    public int Argon2_MemoryKb { get; set; } = 64 * 1024; // m (64MB)
    public int Argon2_DegreeOfParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);

    // PBKDF2
    public int Pbkdf2_Iterations { get; set; } = 210_000; // OWASP 2025+
    public HashAlgorithmName Pbkdf2_Hash { get; set; } = HashAlgorithmName.SHA256;
}

// public sealed class Argon2Options
// {
//     public int Iterations { get; set; } = 3;       // t
//     public int MemoryKb { get; set; } = 64 * 1024; // m (64MB)
//     public int DegreeOfParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);
// }
//
// public sealed class Pbkdf2Options
// {
//     public int Iterations { get; set; } = 210_000; // OWASP 2025+
//     public HashAlgorithmName Hash { get; set; } = HashAlgorithmName.SHA256;
// }
