namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories;

/// <summary>
/// Фабрика валидаторов FluentValidation на основе <see cref="FormSchema"/>.
/// </summary>
internal interface IFormValidatorFactory
{
    /// <summary>Построить FluentValidation-валидатор для данных формы (словарь field→value).</summary>
    IValidator<IDictionary<string, object?>> Create(FormSchema schema);
}
