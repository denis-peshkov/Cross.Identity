namespace Cross.Identity;

public interface IFlowExecutor
{
    /// <summary>
    /// Запускает указанный процесс и возвращает результат его выполнения.
    /// </summary>
    /// <param name="input">Scoped-провайдер входных данных HTTP-запроса для шагов сбора формы. Контроллер/эндпойнт кладёт тело запроса, шаг <c>CollectFormStep</c> его читает.</param>
    /// <param name="flow">Идентификатор флоу (например, "game"). Пример: <c>"game"</c>, <c>"licenses"</c>, <c>"shop"</c>.</param>
    /// <param name="operation">Идентификатор операции в рамках флоу (например, "auth"). Пример: <c>"register"</c>, <c>"auth"</c>, <c>"getuser"</c>.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<FlowResult> ExecuteAsync(Dictionary<string, object?> input, string flow, FlowOperationEnum operation, CancellationToken cancellationToken);
}
