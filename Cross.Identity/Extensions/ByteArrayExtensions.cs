namespace Cross.Identity.Extensions;

public static class ByteArrayExtensions
{
    public static byte[] GetBytes(this byte[] source, int length)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (length <= 0)
            return Array.Empty<byte>();

        if (length >= source.Length)
            return source.ToArray();

        var result = new byte[length];
        Buffer.BlockCopy(source, 0, result, 0, length);
        return result;
    }
}
