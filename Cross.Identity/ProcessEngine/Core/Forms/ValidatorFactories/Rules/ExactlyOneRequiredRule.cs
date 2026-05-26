namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// Ровно одно из перечисленных полей должно быть заполнено
/// </summary>
internal sealed record ExactlyOneRequiredRule(IReadOnlyList<string> Fields, string? Message = null) : IFormSchemaRule;
