namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Step execution result.
/// </summary>
/// <param name="Status">Step status.</param>
/// <param name="Next">Next step name (null = finish).</param>
/// <param name="Error">Exception when Status = Fail.</param>
internal readonly record struct StepResult(StepStatusEnum Status, string? Next = null, Exception? Error = null)
{
    public static StepResult Ok(string? next = null)
        => new(StepStatusEnum.Ok, next);

    public static StepResult Fail(Exception ex)
        => new(StepStatusEnum.Fail, null, ex);
}
