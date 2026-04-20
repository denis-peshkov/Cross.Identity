namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// Минимум одно из перечисленных полей
/// </summary>
internal sealed record AtLeastOneRequiredRule(IReadOnlyList<string> Fields, string? Message = null) : IFormSchemaRule;
