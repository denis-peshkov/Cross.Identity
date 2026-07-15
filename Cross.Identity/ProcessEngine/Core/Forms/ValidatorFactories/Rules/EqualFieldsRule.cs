namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// Two-field equality rule: <c>left == right</c>.
/// </summary>
internal sealed record EqualFieldsRule(string Left, string Right, string? Message = null) : IFormSchemaRule;
