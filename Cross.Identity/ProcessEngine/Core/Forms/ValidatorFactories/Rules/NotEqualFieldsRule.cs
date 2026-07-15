namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// The left field must differ from the right field.
/// </summary>
internal sealed record NotEqualFieldsRule(string Left, string Right, string? Message = null) : IFormSchemaRule;
