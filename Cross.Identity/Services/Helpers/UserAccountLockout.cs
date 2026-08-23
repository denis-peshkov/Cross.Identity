namespace Cross.Identity.Services.Helpers;

/// <summary>
/// Account lockout after repeated failed password or OTP sign-in attempts
/// (ASP.NET Identity-style fields on <see cref="UserAccountEntity"/>).
/// </summary>
internal static class UserAccountLockout
{
    public static bool IsLockedOut(UserAccountEntity user, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.LockoutEnabled
               && user.LockoutEnd.HasValue
               && user.LockoutEnd.Value > now;
    }

    public static void RecordFailedAccess(
        UserAccountEntity user,
        AuthenticationOptions.LockoutOptions options,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(options);

        if (!user.LockoutEnabled || options.MaxFailedAccessAttempts <= 0)
            return;

        user.AccessFailedCount++;
        if (user.AccessFailedCount >= options.MaxFailedAccessAttempts)
            user.LockoutEnd = now.Add(options.LockoutTimeout);
    }

    public static void Reset(UserAccountEntity user)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
    }
}
