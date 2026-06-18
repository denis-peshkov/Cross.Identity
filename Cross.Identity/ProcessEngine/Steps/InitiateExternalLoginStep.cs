using Cross.Identity.Services.ExternalOAuth;

namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг инициации внешнего OAuth-логина: формирует URL авторизации провайдера.
/// </summary>
internal sealed class InitiateExternalLoginStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string ProviderKey { get; init; }

    public string? ReturnUrlKey { get; init; }

    public string? LinkUserIdKey { get; init; }

    public required IExternalLoginService ExternalLoginService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var provider = ctx.Get<string>(BagKey.Qualify(Kind, ProviderKey));

        string? returnUrl = null;
        if (!string.IsNullOrWhiteSpace(ReturnUrlKey))
        {
            ctx.TryGet(BagKey.Qualify(Kind, ReturnUrlKey), out returnUrl);
        }

        Guid? linkUserId = null;
        if (!string.IsNullOrWhiteSpace(LinkUserIdKey))
        {
            if (ctx.TryGet<object?>(LinkUserIdKey, out var linkUserIdRaw) && linkUserIdRaw is not null)
            {
                if (linkUserIdRaw is Guid guid)
                {
                    linkUserId = guid;
                }
                else if (Guid.TryParse(linkUserIdRaw.ToString(), out var parsed))
                {
                    linkUserId = parsed;
                }
            }
        }

        var url = await ExternalLoginService.InitiateAsync(provider, returnUrl, linkUserId, cancellationToken).ConfigureAwait(false);
        ctx.Set(BagKey.Qualify(Kind, "Url"), url);

        return StepResult.Ok(Next);
    }
}
