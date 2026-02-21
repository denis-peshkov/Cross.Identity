namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// Правило равенства двух полей: <c>left == right</c>.
/// </summary>
public sealed record EqualFieldsRule(string Left, string Right, string? Message = null) : IFormSchemaRule;
