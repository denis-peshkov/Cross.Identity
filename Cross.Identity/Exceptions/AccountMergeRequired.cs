namespace Cross.Identity.Exceptions;

/// <summary>
/// OAuth link-conflict helpers: mint details on a <see cref="ConflictException"/> (HTTP 409)
/// with the same ProblemDetails keys as the former ValidationException failures.
/// </summary>
public static class AccountMergeRequired
{
    /// <summary>Machine code exposed as ProblemDetails <c>errors.code</c> and ConflictException sub-code.</summary>
    public const string ErrorCode = "alreadyLinked";

    /// <summary><see cref="Exception.Data"/> key for the one-time merge token.</summary>
    public const string MergeTokenKey = "mergeToken";

    /// <summary><see cref="Exception.Data"/> key for absorbed account B.</summary>
    public const string AbsorbedUserAccountIdKey = "absorbedUserAccountId";

    /// <summary><see cref="Exception.Data"/> key for survivor account A.</summary>
    public const string SurvivorUserAccountIdKey = "survivorUserAccountId";

    /// <summary><see cref="Exception.Data"/> key for provider name.</summary>
    public const string ProviderKey = "provider";

    /// <summary>
    /// Builds a <see cref="ConflictException"/> with sub-code <see cref="ErrorCode"/> and structured <see cref="Exception.Data"/>.
    /// </summary>
    public static ConflictException Create(
        string message,
        string mergeToken,
        Guid absorbedUserAccountId,
        Guid survivorUserAccountId,
        string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(mergeToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        var ex = new ConflictException(message, ErrorCode);
        ex.Data[MergeTokenKey] = mergeToken;
        ex.Data[AbsorbedUserAccountIdKey] = absorbedUserAccountId.ToString("D");
        ex.Data[SurvivorUserAccountIdKey] = survivorUserAccountId.ToString("D");
        ex.Data[ProviderKey] = provider;
        return ex;
    }

    /// <summary>True when <paramref name="ex"/> carries account-merge conflict payload.</summary>
    public static bool TryGetMergeToken(ConflictException ex, out string mergeToken)
    {
        ArgumentNullException.ThrowIfNull(ex);
        if (ex.Data[MergeTokenKey] is string token && !string.IsNullOrWhiteSpace(token))
        {
            mergeToken = token;
            return true;
        }

        mergeToken = string.Empty;
        return false;
    }
}
