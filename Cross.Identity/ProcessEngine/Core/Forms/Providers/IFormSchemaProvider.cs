namespace Cross.Identity.ProcessEngine.Core.Forms.Providers;

/// <summary>
/// Optional named form schema provider (used only when a step has a <c>schema</c> property).
/// Registration is optional if you use only inline schemas (<c>schemaDef</c>).
/// </summary>
internal interface IFormSchemaProvider
{
    /// <summary>Get a form schema by name.</summary>
    FormSchema Get(string name);
}
