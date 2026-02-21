namespace Cross.Identity.ProcessEngine.Core.Forms.Providers;

/// <summary>
/// Опциональный провайдер именованных схем форм (используется только при наличии свойства <c>schema</c> в шаге).
/// Если работаешь только с inline-схемами (<c>schemaDef</c>), регистрировать не обязательно.
/// </summary>
public interface IFormSchemaProvider
{
    /// <summary>Получить схему формы по имени.</summary>
    FormSchema Get(string name);
}
