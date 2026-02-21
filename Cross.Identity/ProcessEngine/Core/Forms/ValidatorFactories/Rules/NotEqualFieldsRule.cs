namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// Поле left должно отличаться от поля right.
/// </summary>
public sealed record NotEqualFieldsRule(string Left, string Right, string? Message = null) : IFormSchemaRule;
