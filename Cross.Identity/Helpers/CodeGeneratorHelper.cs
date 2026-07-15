namespace Cross.Identity.Helpers;

public static class CodeGeneratorHelper
{
    private const string LETTERS = "ABCDEFGHJKLMNPQRSTUVWXYZ";

    private const string DIGITS = "123456789";

    /// <summary>
    /// Generates a random alphanumeric code (e.g., F6T7UY1H)
    /// </summary>
    public static string GenerateCode(int length = 8)
    {
        var random = new Random();
        return new string(
            Enumerable.Repeat(LETTERS + DIGITS, length)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
    }

    /// <summary>
    /// Generates a random uppercase letter code (e.g., QKRVZA).
    /// </summary>
    public static string GenerateLetterCode(int length = 6)
    {
        var random = new Random();
        return new string(
            Enumerable.Repeat(LETTERS, length)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
    }

    /// <summary>
    /// Generates a numeric code with specified length (e.g., 867530).
    /// </summary>
    public static string GenerateNumericCode(int length = 6)
    {
        var random = new Random();
        return new string(
            Enumerable.Repeat(DIGITS, length)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
    }

    public static byte[] GenerateHash(string code)
    {
        var res = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return res;
    }
}
