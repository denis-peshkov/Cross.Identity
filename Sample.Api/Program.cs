var builder = WebApplication.CreateBuilder(args);

// HttpContextAccessor for JwtTokenService
builder.Services.AddHttpContextAccessor();

// Pepper from appsettings (Sample.Api local dev — no AUTH_PEPPERS_JSON required)
builder.Services.AddPepperOptions<EnvProviderOptions, EnvProviderOptionsValidator>(builder.Configuration);
builder.Services.TryAddScoped<IPepperVaultProvider, EnvPepperProvider>();

// Register Cross.Identity + IdentityContext / factory (InMemory for the sample).
builder.Services.AddCrossIdentity(
    builder.Configuration,
    options => options.UseInMemoryDatabase("CrossIdentity"));

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Minimal API endpoint equivalent to IdentityController.RunAsync
app.MapPost(
        "/api/identity/{flow}/{operation}",
        async (
            string flow,
            FlowOperationEnum operation,
            Dictionary<string, object?> body,
            IFlowExecutor flowExecutor,
            CancellationToken cancellation) =>
        {
            var input = FlowInputNormalizer.Normalize(body);
            var result = await flowExecutor.ExecuteAsync(input, flow, operation, cancellation).ConfigureAwait(false);
            return Results.Ok(result.Data);
        })
    .AllowAnonymous()
    .WithName("RunIdentityFlow")
    .WithOpenApi();

// FormCallbackUrl bridge (API): Apple POSTs form_post (code, state, user) → main.ExternalLoginCallback.
// CallbackUrl for GET providers is the SPA route (auth/external-login-callback), not this host.
async Task<IResult> OAuthReturnBridgeAsync(
    HttpRequest request,
    IFlowExecutor flowExecutor,
    CancellationToken cancellation)
{
    _ = await request.ReadFormAsync(cancellation).ConfigureAwait(false);

    static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    string? Read(string name)
        => NullIfWhiteSpace(request.Form[name].ToString());

    var input = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
    {
        ["Code"] = Read("code"),
        ["State"] = Read("state") ?? string.Empty,
        ["Error"] = Read("error"),
        ["ErrorDescription"] = Read("error_description"),
        ["User"] = Read("user"),
    };

    var result = await flowExecutor
        .ExecuteAsync(input, "main", FlowOperationEnum.ExternalLoginCallback, cancellation)
        .ConfigureAwait(false);
    return Results.Ok(result.Data);
}

app.MapPost("/api/v1/Identity/ExternalLogin/OAuthReturn", OAuthReturnBridgeAsync)
    .AllowAnonymous()
    .DisableAntiforgery()
    .WithName("ExternalLoginOAuthReturn")
    .WithOpenApi();

app.Run();
