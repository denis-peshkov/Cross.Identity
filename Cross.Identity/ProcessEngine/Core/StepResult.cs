namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Результат выполнения шага.
/// </summary>
/// <param name="Status">Статус шага.</param>
/// <param name="Next">Имя следующего шага (null = завершить).</param>
/// <param name="Error">Исключение, если Status = Fail.</param>
internal readonly record struct StepResult(StepStatusEnum Status, string? Next = null, Exception? Error = null)
{
    public static StepResult Ok(string? next = null)
        => new(StepStatusEnum.Ok, next);

    public static StepResult Fail(Exception ex)
        => new(StepStatusEnum.Fail, null, ex);
}
