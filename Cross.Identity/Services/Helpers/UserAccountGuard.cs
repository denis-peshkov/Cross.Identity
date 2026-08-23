namespace Cross.Identity.Services.Helpers;

/// <summary>
/// Account-level guards: activation state and confirmed contact uniqueness.
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

    public static async Task EnsureNoOtherConfirmedEmailAsync(
        IdentityContext context,
        Guid userId,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);

        var conflict = await context.UsersAccounts
            .AsNoTracking()
            .AnyAsync(
                x => x.Email == normalizedEmail && x.EmailConfirmed && x.Id != userId,
                cancellationToken)
            .ConfigureAwait(false);

        if (conflict)
        {
            throw new InvalidOperationException("Email already exists.");
        }
    }

    public static async Task EnsureNoOtherConfirmedPhoneAsync(
        IdentityContext context,
        Guid userId,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedPhone);

        var conflict = await context.UsersAccounts
            .AsNoTracking()
            .AnyAsync(
                x => x.PhoneNumber == normalizedPhone && x.PhoneNumberConfirmed && x.Id != userId,
                cancellationToken)
            .ConfigureAwait(false);

        if (conflict)
        {
            throw new InvalidOperationException("PhoneNumber already exists.");
        }
    }
}
