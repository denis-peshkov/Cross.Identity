var builder = WebApplication.CreateBuilder(args);

// DbContext for Cross.Identity (in-memory for the sample)
builder.Services.AddDbContext<IdentityContext>(options =>
    options.UseInMemoryDatabase("CrossIdentity"));

// HttpContextAccessor for JwtTokenService
builder.Services.AddHttpContextAccessor();

// HeadersContextAccessor for UserService (language, region, etc.)
builder.Services.AddScoped<IHeadersContextAccessor, HeadersContextAccessor>();

// Pepper setup via Cross.PepperVault.EnvJson (options from appsettings, JSON from env)
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
            var result = await flowExecutor.ExecuteAsync(body, flow, operation, cancellation).ConfigureAwait(false);
            return Results.Ok(result.Data);
        })
    .AllowAnonymous()
    .WithName("RunIdentityFlow")
    .WithOpenApi();

app.Run();
