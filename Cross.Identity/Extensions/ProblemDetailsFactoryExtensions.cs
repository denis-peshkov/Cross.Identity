namespace Cross.Identity.Extensions;

/// <summary>
/// Utilities for converting library exceptions to <see cref="ValidationProblemDetails"/>.
/// </summary>
public static class ProblemDetailsFactoryExtensions
{
    public static ValidationProblemDetails ToValidationProblemDetails(this ValidationException ex)
    {
        var dict = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(dict)
        {
            Status = 406,
            Title = "Validation Failed"
        };
    }

    public static ValidationProblemDetails ToValidationProblemDetails(this IDictionary<string, string[]> errors)
    {
        return new ValidationProblemDetails(errors)
        {
            Status = 406,
            Title = "Validation Failed"
        };
    }

    /// <summary>
    /// Maps a conflict (including OAuth link-merge) to ProblemDetails with HTTP 409.
    /// When <see cref="AccountMergeRequired"/> data is present, includes
    /// <c>code</c>, <c>mergeToken</c>, <c>absorbedUserAccountId</c>, <c>survivorUserAccountId</c>, <c>provider</c>.
    /// </summary>
    public static ValidationProblemDetails ToConflictProblemDetails(this ConflictException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [string.Empty] = new[] { ex.Message },
        };

        if (ex.Data["SubCode"] is string subCode && !string.IsNullOrWhiteSpace(subCode))
        {
            errors["code"] = new[] { subCode };
        }

        CopyData(ex, errors, AccountMergeRequired.MergeTokenKey);
        CopyData(ex, errors, AccountMergeRequired.AbsorbedUserAccountIdKey);
        CopyData(ex, errors, AccountMergeRequired.SurvivorUserAccountIdKey);
        CopyData(ex, errors, AccountMergeRequired.ProviderKey);

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
        };
    }

    private static void CopyData(ConflictException ex, IDictionary<string, string[]> errors, string key)
    {
        if (ex.Data[key] is string value && !string.IsNullOrWhiteSpace(value))
        {
            errors[key] = new[] { value };
        }
    }
}
