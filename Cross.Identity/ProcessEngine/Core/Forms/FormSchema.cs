namespace Cross.Identity.ProcessEngine.Core.Forms;

/// <summary>
/// Описание схемы формы (поля + inline-валидаторы).
/// ВАЖНО: имя схемы (<see cref="Name"/>) — служебное.
/// Префикс для Bag задаётся <b>kind шага</b>, а не именем схемы.
/// </summary>
public sealed class FormSchema
{
    /// <summary>Произвольное имя схемы (для отладки/логов/кэша).</summary>
    public string Name { get; }

    /// <summary>Список полей формы.</summary>
    public IReadOnlyList<FieldDescriptor> Fields { get; }

    /// <summary>Inline-валидаторы схемы (например, сравнение полей).</summary>
    public IReadOnlyList<IFormSchemaRule> Validators { get; }

    public FormSchema(
        string name,
        IEnumerable<FieldDescriptor> fields,
        IEnumerable<IFormSchemaRule>? validators = null)
    {
        Name       = name;
        Fields     = new List<FieldDescriptor>(fields);
        Validators = validators is null ? new List<IFormSchemaRule>() : new List<IFormSchemaRule>(validators);
    }
}
