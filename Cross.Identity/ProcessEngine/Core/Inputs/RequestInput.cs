namespace Cross.Identity.ProcessEngine.Core.Inputs;

/// <summary>
/// Стандартная реализация <see cref="IRequestInput"/> для одного HTTP-запроса.
/// </summary>
internal sealed class RequestInput : IRequestInput
{
    private IDictionary<string, object?>? _data;

    /// <inheritdoc />
    public Task<IDictionary<string, object?>> GetAsync(CancellationToken cancellation)
        => Task.FromResult(_data ?? new Dictionary<string, object?>());

    /// <inheritdoc />
    public void Set(IDictionary<string, object?> data) => _data = data;
}
