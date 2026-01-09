using BankMore.Account.Api.Extensions;
using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.Extensions;
using BankMore.Account.Application.Shared;
using BankMore.Account.Infrastructure.DbContexts;
using BankMore.Account.Infrastructure.Extensions;
using BankMore.Account.Infrastructure.Security;
using BankMore.Account.Infrastructure.Persistence;
using BankMore.JwtService.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using BankMore.JwtService.Extensions;


var builder = WebApplication.CreateBuilder(args);
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRateLimiterConfiguration(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("Accounts")
    ?? throw new InvalidOperationException("Connection string 'Accounts' não encontrada.");

builder.Services.AddDbContext<BankMoreAccountContext>(options =>
    options.UseSqlServer(connectionString));


builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Redis");

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException(
            "ConnectionStrings:Redis não configurada.");

    var options = ConfigurationOptions.Parse(connectionString);
    options.AbortOnConnectFail = false; 

    return ConnectionMultiplexer.Connect(options);
});

builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<IAccountCommand>();
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(); 
builder.Services.AddScoped<IRefreshTokenStore, RedisRefreshTokenStore>();
builder.Services.AddScoped<ITokenService, TokenService>();


builder.Services.AddJwtAuth(builder.Configuration);

builder.Services.AddAuthorizationBuilder();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        var bearerScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Informe o token JWT no header: Authorization: Bearer {token}"
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        // Nome do esquema no Swagger
        document.Components.SecuritySchemes["Bearer"] = bearerScheme;

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    });
});



var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/openapi/v1.json", "BankMore Account API V1");
    });
}

//Sei que isso não é o ideal para migrações em produção, mas para o escopo desse projeto está aceitável.
//o ideal seria usar um pipeline de CI/CD para aplicar as migrações.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankMoreAccountContext>();
    await db.Database.MigrateAsync();
}


app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();