namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories;

/// <summary>
/// FluentValidation validator factory based on <see cref="FormSchema"/>.
/// </summary>
internal interface IFormValidatorFactory
{
    /// <summary>Build a FluentValidation validator for form data (field→value dictionary).</summary>
    IValidator<IDictionary<string, object?>> Create(FormSchema schema);
}
