namespace Cross.Identity.ProcessEngine.Core.Inputs;

/// <summary>
/// Scoped-провайдер входных данных HTTP-запроса для шагов сбора формы.
/// Контроллер/эндпойнт кладёт тело запроса, шаг <c>CollectFormStep</c> его читает.
/// </summary>
internal interface IRequestInput
{
    /// <summary>Получить данные запроса.</summary>
    Task<IDictionary<string, object?>> GetAsync(CancellationToken cancellation);

    /// <summary>Установить данные запроса (обычно из контроллера).</summary>
    void Set(IDictionary<string, object?> data);
}
