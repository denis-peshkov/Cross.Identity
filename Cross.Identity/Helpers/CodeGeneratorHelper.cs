namespace Cross.Identity.Helpers;

public static class CodeGeneratorHelper
{
    private const string LETTERS = "ABCDEFGHJKLMNPQRSTUVWXYZ";

    private const string DIGITS = "123456789";

    /// <summary>
    /// Generates a random alphanumeric code (e.g., F6T7UY1H)
    /// </summary>
    public static string GenerateCode(int length = 8)
        => GenerateFromAlphabet(LETTERS + DIGITS, length);

    /// <summary>
    /// Generates a random uppercase letter code (e.g., QKRVZA).
    /// </summary>
    public static string GenerateLetterCode(int length = 6)
        => GenerateFromAlphabet(LETTERS, length);

    /// <summary>
    /// Generates a numeric code with specified length (e.g., 867530).
    /// </summary>
    public static string GenerateNumericCode(int length = 6)
        => GenerateFromAlphabet(DIGITS, length);

    /// <summary>
    /// Computes SHA-256 over the UTF-8 bytes of <paramref name="code"/> (32-byte digest).
    /// </summary>
    /// <param name="code">Plain OTP or verification code.</param>
    /// <returns>SHA-256 hash (32 bytes).</returns>
    public static byte[] GenerateHash(string code)
        => SHA256.HashData(Encoding.UTF8.GetBytes(code));

    private static string GenerateFromAlphabet(string alphabet, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(chars);
    }
}
