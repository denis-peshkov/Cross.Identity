namespace Cross.Identity.Services;

/// <summary>
/// User service: creation, lookup, and password operations.
/// Used by process steps:
/// <list type="bullet">
/// <item><description><c>CreateUserStep</c> — <see cref="CreateUserAsync"/></description></item>
/// <item><description><c>GetUserAccountIdStep</c> — <see cref="GetUserAccountIdByAsync"/></description></item>
/// <item><description><c>PasswordAuthStep</c> — <see cref="ValidatePasswordAsync"/></description></item>
/// <item><description><c>TokenStep</c> — <see cref="ValidatePasswordAsync"/>, <see cref="ValidateCodeAsync"/>, <see cref="GetUserByAsync"/></description></item>
/// <item><description><c>ResetPasswordStep</c> — <see cref="SetPasswordAsync"/></description></item>
/// <item><description><c>RefreshTokenStep</c>, <c>ExternalLoginCompleteStep</c> — <see cref="GetUserByAsync"/></description></item>
/// </list>
/// </summary>
internal interface IUserService
{
    /// <summary>
    /// Find a user identifier by selector field.
    /// Does not throw when the user is missing — returns <c>null</c> (anti user-enumeration in lookup steps).
    /// Allowed <paramref name="selectorField"/> values depend on the implementation
    /// (at minimum <c>"Id"</c>, <c>"UserAccountId"</c> (alias <c>"UserId"</c>), <c>"Email"</c>, <c>"UserName"</c>, and <c>"PhoneNumber"</c> are supported).
    /// For <c>PhoneNumber</c>, pass an already-valid E.164 value (e.g. <c>+79161234567</c>) as produced by <c>collectForm</c>.
    /// When several accounts share the same email or phone, the verified contact is preferred.
    /// </summary>
    /// <param name="selectorField">Field name to search by (e.g. <c>"Email"</c>).</param>
    /// <param name="selectorValue">Selector value (e.g. email address or Guid string for <c>"Id"</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User account id, or <c>null</c> if not found or the id value is invalid.</returns>
    /// <exception cref="NotSupportedException"><paramref name="selectorField"/> is not supported.</exception>
    Task<Guid?> GetUserAccountIdByAsync(
        string selectorField,
        string selectorValue,
        CancellationToken cancellationToken);

    /// <summary>
    /// Load a user account entity by selector field.
    /// Unlike <see cref="GetUserAccountIdByAsync"/>, throws when the user is missing.
    /// Allowed <paramref name="selectorField"/> values match <see cref="GetUserAccountIdByAsync"/>
    /// (<c>"Id"</c>, <c>"UserAccountId"</c>, <c>"Email"</c>, <c>"UserName"</c>, <c>"PhoneNumber"</c>; <c>"UserId"</c> alias for id).
    /// For <c>PhoneNumber</c>, pass an already-valid E.164 value as produced by <c>collectForm</c>.
    /// When several accounts share the same email or phone, the verified contact is preferred.
    /// </summary>
    /// <param name="selectorField">Field name to search by (e.g. <c>"Id"</c>).</param>
    /// <param name="selectorValue">Selector value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching <see cref="UserAccountEntity"/> (read-only, not tracked).</returns>
    /// <exception cref="NotFoundException">No user matches the selector.</exception>
    /// <exception cref="NotSupportedException"><paramref name="selectorField"/> is not supported.</exception>
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
    /// <exception cref="ConflictException">A verified email, phone, or username already exists.</exception>
    Task<Guid> CreateUserAsync(
        IDictionary<string, object?> map,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verify the password of a user found by selector.
    /// Applies account lockout (<see cref="UserAccountEntity.AccessFailedCount"/>, <see cref="UserAccountEntity.LockoutEnd"/>)
    /// per <c>Authentication:Lockout</c> on failed attempts; resets on success.
    /// Returns <c>false</c> when the user is missing, inactive, has no password, or is locked out (no exception).
    /// </summary>
    /// <param name="selectorField">Lookup field (e.g. <c>"Email"</c>).</param>
    /// <param name="selectorValue">Selector value.</param>
    /// <param name="password">Password to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the password is correct; otherwise <c>false</c>.</returns>
    /// <exception cref="NotSupportedException"><paramref name="selectorField"/> is not supported.</exception>
    Task<bool> ValidatePasswordAsync(
        string selectorField,
        string selectorValue,
        string password,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verify a one-time code for a user found by selector (code-login / email-confirm path).
    /// Delivery channel matches <see cref="ICommunicationEndpointService.ResolveOtpTargetAsync"/>
    /// (same as <c>SendCodeStep</c>), not the selector field type.
    /// On success, <c>EmailVerified</c> / <c>PhoneNumberVerified</c> follow the OTP channel and address,
    /// not the selector field (Email vs Phone).
    /// Applies the same account lockout policy as <see cref="ValidatePasswordAsync"/>:
    /// locked accounts are rejected; failed codes increment <c>AccessFailedCount</c>; success resets lockout.
    /// </summary>
    /// <param name="selectorField">Lookup field (e.g. <c>"Email"</c>).</param>
    /// <param name="selectorValue">Selector value.</param>
    /// <param name="code">Code to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the code is correct; otherwise <c>false</c> (wrong code, inactive account, or lockout).</returns>
    /// <exception cref="NotFoundException">No user matches the selector.</exception>
    /// <exception cref="NotSupportedException"><paramref name="selectorField"/> is not supported.</exception>
    Task<bool> ValidateCodeAsync(
        string selectorField,
        string selectorValue,
        string code,
        CancellationToken cancellationToken);

    /// <summary>
    /// Set (or replace) the password of a user found by selector.
    /// Rotates <c>SecurityStamp</c> and revokes all active access/refresh tokens for the user.
    /// </summary>
    /// <param name="selectorField">Lookup field (e.g. <c>"Id"</c> or <c>"Email"</c>).</param>
    /// <param name="selectorValue">Selector value.</param>
    /// <param name="newPassword">New password.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">No user matches the selector.</exception>
    /// <exception cref="NotSupportedException"><paramref name="selectorField"/> is not supported.</exception>
    /// <exception cref="InvalidOperationException">Current pepper version is not available.</exception>
    Task SetPasswordAsync(
        string selectorField,
        string selectorValue,
        string newPassword,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken);
}
