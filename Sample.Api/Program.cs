var builder = WebApplication.CreateBuilder(args);

// DbContext для Cross.Identity (in-memory для примера)
builder.Services.AddDbContext<IdentityContext>(options =>
    options.UseInMemoryDatabase("CrossIdentity"));

// HttpContextAccessor для JwtTokenService
builder.Services.AddHttpContextAccessor();

// HeadersContextAccessor для UserService (язык, регион и пр.)
builder.Services.AddScoped<IHeadersContextAccessor, HeadersContextAccessor>();

// Настройка перцев через Cross.PepperVault.EnvJson (опции из appsettings, JSON из env)
builder.Services.AddPepperOptions<EnvJsonProviderOptions, EnvJsonProviderOptionsValidator>(builder.Configuration);
builder.Services.TryAddScoped<IPepperVaultProvider, EnvJsonPepperProvider>();

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

app.UseHttpsRedirection();

// Minimal API endpoint, эквивалентный IdentityController.RunAsync
app.MapPost(
        "/api/identity/{flow}/{operation}",
        async (
            string flow,
            FlowOperationEnum operation,
            Dictionary<string, object?> body,
            IFlowExecutor flowExecutor,
            CancellationToken cancellation) =>
        {
            var result = await flowExecutor.ExecuteAsync(body, flow, operation, cancellation).ConfigureAwait(false);
            return Results.Ok(result.Data);
        })
    .AllowAnonymous()
    .WithName("RunIdentityFlow")
    .WithOpenApi();

app.Run();
