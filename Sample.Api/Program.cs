var builder = WebApplication.CreateBuilder(args);

// DbContext for Cross.Identity (in-memory for the sample)
builder.Services.AddDbContext<IdentityContext>(options =>
    options.UseInMemoryDatabase("CrossIdentity"));

// HttpContextAccessor for JwtTokenService
builder.Services.AddHttpContextAccessor();

// Pepper from appsettings (Sample.Api local dev — no AUTH_PEPPERS_JSON required)
builder.Services.AddPepperOptions<EnvProviderOptions, EnvProviderOptionsValidator>(builder.Configuration);
builder.Services.TryAddScoped<IPepperVaultProvider, EnvPepperProvider>();

// Register Cross.Identity
builder.Services.AddCrossIdentity(builder.Configuration);

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

// OAuth redirect_uri bridge (Authentication:ExternalLogin:CallbackUrl).
// GET — query providers (Google / Microsoft / GitHub / Facebook).
// POST — Apple Sign In form_post (code, state, user). Forwards into main.ExternalLoginCallback.
async Task<IResult> OAuthCallbackBridgeAsync(
    HttpRequest request,
    IFlowExecutor flowExecutor,
    CancellationToken cancellation)
{
    string? Read(string name)
    {
        if (HttpMethods.IsPost(request.Method))
        {
            return NullIfWhiteSpace(request.Form[name].ToString());
        }

        return NullIfWhiteSpace(request.Query[name].ToString());
    }

    if (HttpMethods.IsPost(request.Method))
    {
        _ = await request.ReadFormAsync(cancellation).ConfigureAwait(false);
    }

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

static string? NullIfWhiteSpace(string? value)
    => string.IsNullOrWhiteSpace(value) ? null : value;

app.MapGet("/oauth/callback", OAuthCallbackBridgeAsync)
    .AllowAnonymous()
    .WithName("OAuthCallbackBridgeGet")
    .WithOpenApi();

app.MapPost("/oauth/callback", OAuthCallbackBridgeAsync)
    .AllowAnonymous()
    .DisableAntiforgery()
    .WithName("OAuthCallbackBridgePost")
    .WithOpenApi();

app.Run();
