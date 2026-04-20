namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// Поле name должно иметь одно из значений <see cref="Allowed"/> (сравнение строковое).
/// </summary>
internal sealed record OneOfRule(string Name, string[] Allowed, string? Message = null) : IFormSchemaRule;
