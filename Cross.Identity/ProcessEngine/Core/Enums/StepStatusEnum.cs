namespace Cross.Identity.ProcessEngine.Core.Enums;

/// <summary>
/// Статус выполнения шага.
/// </summary>
public enum StepStatusEnum
{
    /// <summary>Успешно.</summary>
    Ok,

    /// <summary>Ошибка, выполнение процесса прерывается.</summary>
    Fail,
}
