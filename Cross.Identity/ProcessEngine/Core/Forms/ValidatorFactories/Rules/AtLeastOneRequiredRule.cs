namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// At least one of the listed fields
/// </summary>
internal sealed record AtLeastOneRequiredRule(IReadOnlyList<string> Fields, string? Message = null) : IFormSchemaRule;
