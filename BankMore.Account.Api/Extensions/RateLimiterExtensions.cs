using System.Threading.RateLimiting;

namespace BankMore.Account.Api.Extensions;

public static class RateLimiterExtensions
{
    //Middleware de Rate Limiting para o endpoint de login, para evitar que um ataque de força bruta derrube o container por falta de memória
    public static IServiceCollection AddRateLimiterConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue<int>("RateLimitSettings:TentativasDeloginPermitidasPorMinuto", 5);
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("LoginPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Muitas tentativas. Tente novamente mais tarde.", token);
            };
        });

        return services;
    }
}

