namespace Cross.Identity.Services.Helpers;

/// <summary>
/// Account-level guards: activation state and verified contact uniqueness.
/// </summary>
internal static class UserAccountGuard
{
    public static void EnsureIsActive(UserAccountEntity user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsActive)
        {
            throw new NotAuthorizedException("Account is disabled.");
        }
    }

    public static async Task EnsureIsActiveAsync(
        IdentityContext context,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new NotAuthorizedException("Account is disabled.");
        }

        var isActive = await context.UsersAccounts
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.IsActive)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!isActive)
        {
            throw new NotAuthorizedException("Account is disabled.");
        }
    }

    public static async Task EnsureNoOtherVerifiedEmailAsync(
        IdentityContext context,
        Guid userId,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);

        var conflict = await context.UsersAccounts
            .AsNoTracking()
            .AnyAsync(
                x => x.Email == normalizedEmail && x.EmailVerified && x.Id != userId,
                cancellationToken)
            .ConfigureAwait(false);

        if (conflict)
        {
            throw new InvalidOperationException("Email already exists.");
        }
    }

    public static async Task EnsureNoOtherVerifiedPhoneAsync(
        IdentityContext context,
        Guid userId,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedPhone);

        var conflict = await context.UsersAccounts
            .AsNoTracking()
            .AnyAsync(
                x => x.PhoneNumber == normalizedPhone && x.PhoneNumberVerified && x.Id != userId,
                cancellationToken)
            .ConfigureAwait(false);

        if (conflict)
        {
            throw new InvalidOperationException("PhoneNumber already exists.");
        }
    }
}
