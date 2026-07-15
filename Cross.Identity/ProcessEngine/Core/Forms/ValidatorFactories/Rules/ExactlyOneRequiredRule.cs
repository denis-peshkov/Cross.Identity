namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// Exactly one of the listed fields must be filled in
/// </summary>
internal sealed record ExactlyOneRequiredRule(IReadOnlyList<string> Fields, string? Message = null) : IFormSchemaRule;
