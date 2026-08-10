namespace Cross.Identity.Services;

/// <summary>
/// User service: creation, lookup, and password operations.
/// Used by process steps:
/// <list type="bullet">
/// <item><description><c>CreateUserStep</c> — <see cref="CreateUserAsync"/></description></item>
/// <item><description><c>PasswordAuthStep</c> — <see cref="ValidatePasswordAsync"/></description></item>
/// <item><description><c>GetUserStep</c> — <see cref="GetUserIdByAsync"/></description></item>
/// </list>
/// </summary>
internal interface IUserService
{
    /// <summary>
    /// Find a user identifier by selector field.
    /// Allowed <paramref name="selectorField"/> values depend on the implementation
    /// (at minimum <c>"UserId"</c>, <c>"Email"</c>, <c>"UserName"</c>, and <c>"PhoneNumber"</c> are supported).
    /// For <c>PhoneNumber</c>, pass an already-valid E.164 value (e.g. <c>+79161234567</c>) as produced by <c>collectForm</c>.
    /// </summary>
    /// <param name="selectorField">Field name to search by (e.g. <c>"Email"</c>).</param>
    /// <param name="selectorValue">Selector value (e.g. email address).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>String user identifier, or <c>null</c> if not found.</returns>
    Task<string> GetUserIdByAsync(
        string selectorField,
        string selectorValue,
        CancellationToken cancellationToken);

    Task<UserAccountEntity> GetUserByAsync(
        string selectorField,
        string selectorValue,
        CancellationToken cancellationToken);

    /// <summary>
    /// Create a new user from a flat value map.
    /// Map keys are logical field names (e.g. <c>"Email"</c>, <c>"UserName"</c>, <c>"PhoneNumber"</c>, <c>"Password"</c>).
    /// Optional keys may be omitted. <c>PhoneNumber</c>, when provided, must already be valid E.164
    /// (e.g. <c>+79161234567</c>) as produced by <c>collectForm</c>.
    /// </summary>
    /// <param name="map">User field map.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created user identifier.</returns>
    Task<string> CreateUserAsync(
        IDictionary<string, object?> map,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verify the password of a user found by selector.
    /// </summary>
    /// <param name="selectorField">Lookup field (e.g. <c>"Email"</c>).</param>
    /// <param name="selectorValue">Selector value.</param>
    /// <param name="password">Password to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the password is correct; otherwise <c>false</c>.</returns>
    Task<bool> ValidatePasswordAsync(
        string selectorField,
        string selectorValue,
        string password,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verify a one-time code for a user found by selector.
    /// </summary>
    /// <param name="selectorField">Lookup field (e.g. <c>"Email"</c>).</param>
    /// <param name="selectorValue">Selector value.</param>
    /// <param name="code">Code to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the code is correct; otherwise <c>false</c>.</returns>
    Task<bool> ValidateCodeAsync(
        string selectorField,
        string selectorValue,
        string code,
        CancellationToken cancellationToken);

    /// <summary>
    /// Set (or replace) the password of a user found by selector.
    /// Rotates <c>SecurityStamp</c> and revokes all active access/refresh tokens for the user.
    /// </summary>
    /// <param name="selectorField">Lookup field (e.g. <c>"Email"</c>).</param>
    /// <param name="selectorValue">Selector value.</param>
    /// <param name="newPassword">New password.</param>
    /// <param name="ipAddress">Optional client IP for token revoke audit fields.</param>
    /// <param name="userAgent">Optional User-Agent for token revoke audit fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetPasswordAsync(
        string selectorField,
        string selectorValue,
        string newPassword,
        string? ipAddress,
        string? userAgent,
        string? deviceFingerprint,
        CancellationToken cancellationToken);
}
