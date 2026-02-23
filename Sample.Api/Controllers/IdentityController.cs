namespace Sample.Api.Controllers;

/// <summary>
/// Универсальный контроллер запуска динамических процессов идентификации/аутентификации.
/// Маршрут: <c>/api/v{version}/identity/{flow}/{operation}</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/identity/{flow}")]
public class IdentityController : ControllerBase
{
    private readonly IFlowExecutor _flowExecutor;

    public IdentityController(IFlowExecutor flowExecutor, IRequestInput input)
    {
        _flowExecutor = flowExecutor;
    }

    /// <summary>
    /// Запустить процесс: <c>{flow}.{operation}</c>.
    /// Тело запроса — произвольный JSON, соответствующий <c>collectForm</c> в первом шаге.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{operation}")]
    public async Task<IActionResult> RunAsync(
        [FromRoute] string flow,
        [FromRoute] FlowOperationEnum operation,
        [FromBody] Dictionary<string, object?> body,
        CancellationToken cancellation)
    {
        var result = await _flowExecutor.ExecuteAsync(body, flow, operation, cancellation);

        return Ok(result.Data);
    }
}
