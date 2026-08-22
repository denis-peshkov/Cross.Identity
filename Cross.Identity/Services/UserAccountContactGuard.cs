namespace Cross.Identity.Services;

/// <summary>
/// Email and phone uniqueness among confirmed accounts; multiple unconfirmed rows per contact are allowed.
/// </summary>
internal static class UserAccountContactGuard
{
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
