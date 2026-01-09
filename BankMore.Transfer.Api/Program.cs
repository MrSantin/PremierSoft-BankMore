using BankMore.JwtService.Extensions;
using BankMore.JwtService.Security;
using BankMore.Transfer.Application.Abstractions;
using BankMore.Transfer.Application.Clients.Accounts.Api;
using BankMore.Transfer.Application.Extensions;
using BankMore.Transfer.Application.Shared;
using BankMore.Transfer.Infrastructure.DbContexts;
using BankMore.Transfer.Infrastructure.Extensions;
using BankMore.Transfer.Infrastructure.Http;
using BankMore.Transfer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Refit;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("Transfers")
    ?? throw new InvalidOperationException("Connection string 'Accounts' não encontrada.");

builder.Services.AddDbContext<BankMoreTransferContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<ITransferCommand>();
});


builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<AuthenticationDelegatingHandler>();

var accountApiBaseUrl = builder.Configuration["Services:AccountApi:BaseUrl"]
    ?? throw new InvalidOperationException("Missing config: Services:AccountApi:BaseUrl");

builder.Services.AddRefitClient<IAccountApiClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(accountApiBaseUrl))
    .AddHttpMessageHandler<AuthenticationDelegatingHandler>();



builder.Services.AddJwtAuth(builder.Configuration);


builder.Services.AddAuthorizationBuilder();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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

// Configure the HTTP request pipeline.
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
    var db = scope.ServiceProvider.GetRequiredService<BankMoreTransferContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
