namespace Cross.Identity.Services.Crypto;

internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasherOptions _options;
    private static readonly Encoding _encoding = Encoding.UTF8;

    public PasswordHasher(IOptionsMonitor<PasswordHasherOptions> options)
    {
        _options = options.CurrentValue;
    }

    public string Hash(string password, string pepper)
    {
        var phc = _options.DefaultAlgorithm switch
        {
            PasswordAlgoEnum.Argon2id => HashArgon2id(password, pepper),
            PasswordAlgoEnum.PBKDF2 => HashPbkdf2(password, pepper),
            PasswordAlgoEnum.SHA256 => HashSha256(password, pepper),
            _ => throw new NotSupportedException()
        };

        return phc;
    }

    public PasswordVerificationEnum Verify(string password, string phc, string pepper)
    {
        if (phc.StartsWith("$argon2id$", StringComparison.Ordinal))
            return VerifyArgon2id(password, phc, pepper);

        if (phc.StartsWith("$pbkdf2-", StringComparison.Ordinal))
            return VerifyPbkdf2(password, phc, pepper);

        if (phc.StartsWith("$sha256$", StringComparison.Ordinal))
            return VerifySha256(password, phc, pepper);

        return PasswordVerificationEnum.Failed;
    }

    public bool NeedsRehash(string phc)
    {
        if (phc.StartsWith("$argon2id$"))
            return NeedsRehashArgon2id(phc);
        if (phc.StartsWith("$pbkdf2-"))
            return NeedsRehashPbkdf2(phc);
        if (phc.StartsWith("$sha256$"))
            return NeedsRehashSha256(phc);
        return true;
    }

    // -------- internals --------

    private string HashArgon2id(string password, string pepper)
    {
        var salt = RandomNumberGenerator.GetBytes(_options.SaltSizeBytes);
        var (t, m, p, outLen) = (_options.Argon2_Iterations, _options.Argon2_MemoryKb, _options.Argon2_DegreeOfParallelism, _options.HashOutputBytes);

        var hash = Argon2id(ToBytes(password, pepper), salt, t, m, p, outLen);

        // PHC: $argon2id$v=19$m=65536,t=3,p=4$base64(salt)$base64(hash)
        return $"$argon2id$v=19$m={m},t={t},p={p}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private PasswordVerificationEnum VerifyArgon2id(string password, string phc, string pepper)
    {
        // Пример: $argon2id$v=19$m=65536,t=3,p=4$<saltB64>$<hashB64>
        var parts = phc.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5 || parts[0] != "argon2id")
            return PasswordVerificationEnum.Failed;

        var paramsPart = parts[2]; // m=...,t=...,p=...
        var saltB64 = parts[3];
        var hashB64 = parts[4];

        var dict = paramsPart.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Split('='))
            .ToDictionary(a => a[0], a => int.Parse(a[1]));
        var m = dict["m"];
        var t = dict["t"];
        var p = dict["p"];

        var salt = Convert.FromBase64String(saltB64);
        var expected = Convert.FromBase64String(hashB64);
        var actual = Argon2id(ToBytes(password, pepper), salt, t, m, p, expected.Length);

        var ok = CryptographicOperations.FixedTimeEquals(actual, expected);
        if (!ok)
            return PasswordVerificationEnum.Failed;

        // Нужен ли rehash (например, повысили параметры)?
        return NeedsRehashArgon2id(phc)
            ? PasswordVerificationEnum.SuccessRehashNeeded
            : PasswordVerificationEnum.Success;
    }

    private bool NeedsRehashArgon2id(string phc)
    {
        try
        {
            var parts = phc.Split('$', StringSplitOptions.RemoveEmptyEntries);
            var dict = parts[2]
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Split('='))
                .ToDictionary(a => a[0], a => int.Parse(a[1]));
            var m = dict["m"];
            var t = dict["t"];
            var p = dict["p"];
            // если текущие параметры меньше желаемых — хотим rehash
            return t < _options.Argon2_Iterations || m < _options.Argon2_MemoryKb || p < _options.Argon2_DegreeOfParallelism;
        }
        catch
        {
            return true;
        }
    }

    private static byte[] Argon2id(byte[] password, byte[] salt, int t, int m, int p, int outLen)
    {
        var argon = new Argon2id(password)
        {
            DegreeOfParallelism = p,
            MemorySize = m,
            Iterations = t,
            Salt = salt
        };
        return argon.GetBytes(outLen);
    }

    private string HashPbkdf2(string password, string pepper)
    {
        var salt = RandomNumberGenerator.GetBytes(_options.SaltSizeBytes);
        var iter = _options.Pbkdf2_Iterations;
        var len = _options.HashOutputBytes;
        var alg = _options.Pbkdf2_Hash;

        var hash = Pbkdf2(ToBytes(password, pepper), salt, iter, len, alg);

        // PHC-подобно: $pbkdf2-sha256$i=210000$base64(salt)$base64(hash)
        var algoTag = alg == HashAlgorithmName.SHA512
            ? "sha512"
            : alg == HashAlgorithmName.SHA384
                ? "sha384"
                : "sha256";

        return $"$pbkdf2-{algoTag}$i={iter}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private PasswordVerificationEnum VerifyPbkdf2(string password, string phc, string pepper)
    {
        // $pbkdf2-sha256$i=210000$<saltB64>$<hashB64>
        var parts = phc.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !parts[0].StartsWith("pbkdf2-", StringComparison.Ordinal))
            return PasswordVerificationEnum.Failed;

        var algo = parts[0].Substring("pbkdf2-".Length);
        var iter = int.Parse(parts[1].Split('=')[1]);
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);

        var alg = algo switch
        {
            "sha512" => HashAlgorithmName.SHA512,
            "sha384" => HashAlgorithmName.SHA384,
            _ => HashAlgorithmName.SHA256
        };

        var actual = Pbkdf2(ToBytes(password, pepper), salt, iter, expected.Length, alg);
        var ok = CryptographicOperations.FixedTimeEquals(actual, expected);
        if (!ok)
            return PasswordVerificationEnum.Failed;

        return NeedsRehashPbkdf2(phc)
            ? PasswordVerificationEnum.SuccessRehashNeeded
            : PasswordVerificationEnum.Success;
    }

    private bool NeedsRehashPbkdf2(string phc)
    {
        try
        {
            var parts = phc.Split('$', StringSplitOptions.RemoveEmptyEntries);
            var algo = parts[0].Substring("pbkdf2-".Length);
            var iter = int.Parse(parts[1].Split('=')[1]);
            var desiredAlgo = _options.Pbkdf2_Hash switch
            {
                var a when a == HashAlgorithmName.SHA512 => "sha512",
                var a when a == HashAlgorithmName.SHA384 => "sha384",
                _ => "sha256"
            };
            return iter < _options.Pbkdf2_Iterations || !algo.Equals(desiredAlgo, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }

    private static byte[] Pbkdf2(byte[] password, byte[] salt, int iterations, int length, HashAlgorithmName alg)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, alg);
        return pbkdf2.GetBytes(length);
    }

    private string HashSha256(string password, string pepper)
    {
        // генерируем соль
        var saltBytes = RandomNumberGenerator.GetBytes(_options.SaltSizeBytes);
        var passwordBytes = _encoding.GetBytes(password);

        var hash = Sha256(passwordBytes, saltBytes, _options.HashOutputBytes);

        // PHC: $sha256$base64(salt)$base64(hash)
        return $"$sha256${Convert.ToBase64String(saltBytes)}${Convert.ToBase64String(hash)}";
    }

    private static byte[] Sha256(byte[] password, byte[] salt, int length)
    {
        // склеиваем password+salt и считаем SHA256
        var hash = SHA256.HashData(password.Concat(salt).ToArray());

        return hash.GetBytes(length);
    }

    public PasswordVerificationEnum VerifySha256(string password, string phc, string pepper)
    {
        // $sha256$<saltB64>$<hashB64>
        var parts = phc.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts is not ["sha256", _, _])
            return PasswordVerificationEnum.Failed;

        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);

        var actual = Sha256(_encoding.GetBytes(password), salt, expected.Length);
        var ok = CryptographicOperations.FixedTimeEquals(actual, expected);
        if (!ok)
            return PasswordVerificationEnum.Failed;

        return NeedsRehashSha256(phc)
            ? PasswordVerificationEnum.SuccessRehashNeeded
            : PasswordVerificationEnum.Success;
    }

    private bool NeedsRehashSha256(string phc)
    {
        return false;
    }

    private static byte[] ToBytes(string password, string? pepper)
    {
        // «Перец» добавляем к паролю перед хешированием
        return _encoding.GetBytes(
            pepper is { Length: > 0 }
                ? password + pepper
                : password);
    }
}
