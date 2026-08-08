namespace Cross.Identity.Helpers;

/// <summary>
/// Resolves an identity selector from optional email, phone, and/or user-name bag keys.
/// Preference order: <c>Email</c>, then <c>PhoneNumber</c>, then <c>UserName</c>.
/// </summary>
internal static class EmailOrPhoneBag
{
    public const string EmailField = "Email";

    public const string PhoneNumberField = "PhoneNumber";

    public const string UserNameField = "UserName";

    /// <summary>
    /// Reads identity keys from the bag (relative keys are qualified with <paramref name="stepKind"/>).
    /// </summary>
    /// <param name="emailKey">Bag key for email (required parameter; value may be empty).</param>
    /// <param name="phoneNumberKey">Optional bag key for phone (E.164).</param>
    /// <param name="userNameKey">Optional bag key for user name.</param>
    /// <returns>
    /// Lookup field, selector value, and delivery/verify channel
    /// (null for <c>UserName</c> — no direct OTP destination).
    /// </returns>
    public static (string Field, string Value, ChannelEnum? Channel) Resolve(
        Bag ctx,
        string stepKind,
        string emailKey,
        string? phoneNumberKey = null,
        string? userNameKey = null)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentException.ThrowIfNullOrWhiteSpace(stepKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(emailKey);

        ctx.TryGet<string?>(BagKey.Qualify(stepKind, emailKey), out var email);

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(phoneNumberKey))
            ctx.TryGet(BagKey.Qualify(stepKind, phoneNumberKey), out phone);

        string? userName = null;
        if (!string.IsNullOrWhiteSpace(userNameKey))
            ctx.TryGet(BagKey.Qualify(stepKind, userNameKey), out userName);

        if (!string.IsNullOrWhiteSpace(email))
            return (EmailField, email, ChannelEnum.Email);

        if (!string.IsNullOrWhiteSpace(phone))
            return (PhoneNumberField, phone, ChannelEnum.Sms);

        if (!string.IsNullOrWhiteSpace(userName))
            return (UserNameField, userName, null);

        throw new ValidationException("Provide an email, phone, or user name.");
    }

    /// <summary>
    /// Whether multi-selector resolution should run (optional phone and/or user-name keys are configured).
    /// </summary>
    public static bool IsMultiSelector(string? phoneNumberKey, string? userNameKey)
        => !string.IsNullOrWhiteSpace(phoneNumberKey) || !string.IsNullOrWhiteSpace(userNameKey);
}
