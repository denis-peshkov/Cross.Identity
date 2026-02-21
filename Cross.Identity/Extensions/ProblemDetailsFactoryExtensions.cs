namespace Cross.Identity.Extensions;

/// <summary>
/// Утилиты для преобразования ValidationException в ValidationProblemDetails.
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
}
