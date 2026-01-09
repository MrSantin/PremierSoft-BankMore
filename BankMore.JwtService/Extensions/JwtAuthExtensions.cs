using BankMore.JwtService.Security.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BankMore.JwtService.Extensions;

public static class JwtAuthExtensions
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtSettings>(config.GetSection("Jwt"));

        var jwt = config.GetSection("Jwt").Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Configuração 'Jwt' não encontrada.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Retorna sempre 403 em caso de token inválido ou expirado
                o.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
