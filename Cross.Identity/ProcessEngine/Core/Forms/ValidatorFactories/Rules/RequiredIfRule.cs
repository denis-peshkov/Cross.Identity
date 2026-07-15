namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// When <c>When.Field</c> equals <c>When.Value</c>, <c>Then.Required</c> makes the field required.
/// </summary>
internal sealed record RequiredIfRule(
    (string Field, string? Value) When,
    (string Name, bool Required) Then,
    string? Message = null) : IFormSchemaRule;
