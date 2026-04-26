namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Сборщик процесса из JSON-дефиниции: поле <c>start</c> + массив <c>steps</c>.
/// Каждый шаг содержит <c>kind</c> и параметры; создание шага делегируется в <see cref="StepRegistry"/>.
/// </summary>
internal static class ProcessLoader
{
    /// <summary>
    /// Построить исполняемый процесс из JSON-строки.
    /// Требования к JSON:
    /// <code language="json">
    /// {
    ///   "start": "collectForm",
    ///   "steps": [
    ///     { "kind": "collectForm", ... },
    ///     { "kind": "passwordAuth", ... },
    ///     { "kind": "issueJwt", ... }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    /// <param name="json">JSON-дефиниция процесса.</param>
    /// <param name="reg">Реестр фабрик шагов.</param>
    /// <param name="sp">DI-провайдер для разрешения зависимостей шагов.</param>
    /// <exception cref="InvalidOperationException">
    /// Брошено, если отсутствует <c>start</c>, массив <c>steps</c>, шаги повторяют <c>kind</c>,
    /// либо <c>start</c> не соответствует ни одному шагу.
    /// </exception>
    public static ProcessExecutor FromJson(string json, StepRegistry reg, IServiceProvider sp)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        ArgumentNullException.ThrowIfNull(reg);
        ArgumentNullException.ThrowIfNull(sp);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // 1) start
        if (!root.TryGetProperty("start", out var startEl) || startEl.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("Process JSON must contain string property 'start'.");

        var start = startEl.GetString();
        if (string.IsNullOrWhiteSpace(start))
            throw new InvalidOperationException("'start' must be a non-empty string (step kind).");

        // 2) steps[]
        if (!root.TryGetProperty("steps", out var stepsEl) || stepsEl.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Process JSON must contain array 'steps'.");

        var steps = new List<IStep>();
        var kinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var stepJson in stepsEl.EnumerateArray())
        {
            // Делегируем создание шага реестру — он сам извлечёт kind и вызовет нужную фабрику.
            var step = reg.Create(stepJson, sp);

            // Проверка уникальности kind в рамках одного процесса
            if (!kinds.Add(step.Kind))
                throw new InvalidOperationException($"Duplicate step kind '{step.Kind}' in process. Each kind must be unique within a flow.");

            steps.Add(step);
        }

        // 3) Валидация: start должен указывать на существующий шаг
        if (!kinds.Contains(start!))
            throw new InvalidOperationException($"Start refers to unknown step kind '{start}'. Ensure there is a step with this kind in 'steps'.");

        // 4) Собираем исполняемый процесс
        return new ProcessExecutor(start!, steps);
    }
}
