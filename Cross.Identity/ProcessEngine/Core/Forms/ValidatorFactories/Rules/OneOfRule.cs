namespace Cross.Identity.ProcessEngine.Core.Forms.ValidatorFactories.Rules;

/// <summary>
/// The name field must have one of the <see cref="Allowed"/> values (string comparison).
/// </summary>
internal sealed record OneOfRule(string Name, string[] Allowed, string? Message = null) : IFormSchemaRule;
