using GeniusVendas.Api.Data;
using GeniusVendas.Api.Repositories;
using GeniusVendas.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<DatabaseConnectionFactory>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<CustomerRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<SyncRepository>();

var app = builder.Build();

if (app.Configuration.GetValue("Database:InitializeOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<DatabaseConnectionFactory>();
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
    await DatabaseInitializer.InitializeAsync(factory, hasher, logger);
}

app.UseMiddleware<BearerTokenMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "GeniusVendas.Api", utc = DateTime.UtcNow }));
app.Run();
