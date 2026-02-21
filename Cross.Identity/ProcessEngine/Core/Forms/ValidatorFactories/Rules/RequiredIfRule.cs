namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// Если <c>When.Field</c> равно <c>When.Value</c>, то <c>Then.Required</c> делает поле обязательным.
/// </summary>
public sealed record RequiredIfRule(
    (string Field, string? Value) When,
    (string Name, bool Required) Then,
    string? Message = null) : IFormSchemaRule;
