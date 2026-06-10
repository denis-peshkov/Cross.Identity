namespace Cross.Identity;

internal class FlowExecutor : IFlowExecutor
{
    private readonly IServiceProvider _sp;
    private readonly StepRegistry _registry;
    private readonly IProcessDefinitionProvider _definition;
    private readonly IRequestInput _requestInput;
    private readonly IRequestInput _requestInput1;

    /// <summary>
    /// Создаёт обработчик.
    /// </summary>
    /// <param name="sp">Корневой DI-провайдер (нужен для создания scoped-зависимостей шагов).</param>
    /// <param name="registry">Реестр фабрик шагов процесса.</param>
    /// <param name="definition">Провайдер JSON-дефиниций процессов.</param>
    /// <param name="requestInput"></param>
    public FlowExecutor(
        IServiceProvider sp,
        StepRegistry registry,
        IProcessDefinitionProvider definition,
        IRequestInput requestInput)
    {
        _sp = sp;
        _registry = registry;
        _definition = definition;
        _requestInput = requestInput;
    }

    /// <inheritdoc/>
    public async Task<FlowResult> ExecuteAsync(Dictionary<string, object?> input, string flow, FlowOperationEnum operation, CancellationToken cancellationToken)
    {
        // 1) Передаём входной payload в движок (шаг collectForm его считает)
        _requestInput.Set(input);

        // 2) достаём JSON процесса
        var json = _definition.GetJson(flow, operation);

        // 3) создаём scope для зависимостей шагов (IRequestInput, IUserService, ICodeService, IJwtIssuer и т.п.)
        using var scope = _sp.CreateScope();

        // 3.1) Передаем данные в новый scope
        var scopedInput = scope.ServiceProvider.GetRequiredService<IRequestInput>();
        scopedInput.Set(input);

        // 4) собираем процесс из JSON
        var process = ProcessLoader.FromJson(json, _registry, scope.ServiceProvider);

        // 5) исполняем
        var bag = new Bag();
        await process.RunAsync(bag, cancellationToken).ConfigureAwait(false);

        // 6) политика возврата по префиксу "collectResult.":
        //    - нет collectResult.*       -> вернуть весь Bag (как словарь)
        //    - один collectResult.*      -> вернуть само значение (а не словарь)
        //    - несколько collectResult.* -> вернуть словарь { <prefix-with-collectResult> : value }
        const string prefix = "collectResult.";
        var all = bag.ToDictionary();
        var resultPairs = all
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        object? data;
        switch (resultPairs.Count)
        {
            case > 0:
                // несколько полей — отдаём объект { field : value }, с обрезанным префиксом
                var trimmed = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var (k, v) in resultPairs)
                {
                    var name = k.Substring(prefix.Length); // "userId" из "collectResult.userId"
                    trimmed[name] = v;
                }
                data = trimmed;
                break;
            default:
                data = null;
                break;
        }

        return new FlowResult { Data = data };
    }
}
