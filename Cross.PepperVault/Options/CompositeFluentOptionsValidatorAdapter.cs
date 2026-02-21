namespace Cross.PepperVault.Options;

public sealed class CompositeFluentOptionsValidatorAdapter<T> : IValidateOptions<T> where T : class
{
    private readonly IEnumerable<IValidator<T>> _validators;
    public CompositeFluentOptionsValidatorAdapter(IEnumerable<IValidator<T>> validators)
        => _validators = validators;

    public ValidateOptionsResult Validate(string? name, T options)
    {
        var allErrors = new List<string>();
        foreach (var v in _validators)
        {
            var res = v.Validate(options!);
            if (!res.IsValid) allErrors.AddRange(res.Errors.Select(e => e.ErrorMessage));
        }
        return allErrors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(allErrors);
    }
}
