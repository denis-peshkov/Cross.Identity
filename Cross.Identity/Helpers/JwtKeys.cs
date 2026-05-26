namespace Cross.Identity.Helpers;

public static class JwtKeys
{
    public static RsaSecurityKey GetRsaKey()
    {
        var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa)
        {
            KeyId = Guid.NewGuid().ToString("N")
        };
        return key;
    }
}
