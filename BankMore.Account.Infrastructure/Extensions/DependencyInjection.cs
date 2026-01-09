using BankMore.Account.Domain.Repositories.Shared;
using BankMore.Account.Infrastructure.Repositories.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BankMore.Account.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IBankMoreAccountRepository<>), typeof(BankMoreAccountRepository<>));

        var assembly = typeof(DependencyInjection).Assembly;
        services.ScanRepositories(assembly);

        return services;
    }


    private static IServiceCollection ScanRepositories(this IServiceCollection services, Assembly assembly)
    {
        var implementations = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                Impl = t,
                Services = t.GetInterfaces()
                    .Where(i => i != typeof(IDisposable))
                    .Where(i => !i.IsGenericType)
                    .Where(i => i.GetInterfaces().Any(x =>
                        x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBankMoreAccountRepository<>)))
            })
            .Where(x => x.Services.Any());

        foreach (var item in implementations)
            foreach (var svc in item.Services)
                services.AddScoped(svc, item.Impl);

        return services;
    }

}
