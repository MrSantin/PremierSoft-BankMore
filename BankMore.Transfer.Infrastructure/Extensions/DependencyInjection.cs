using BankMore.Transfer.Domain.Repositories.Shared;
using BankMore.Transfer.Infrastructure.Repositories.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BankMore.Transfer.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IBankMoreTransferRepository<>), typeof(BankMoreTransferRepository<>));

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
                        x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBankMoreTransferRepository<>)))
            })
            .Where(x => x.Services.Any());

        foreach (var item in implementations)
            foreach (var svc in item.Services)
                services.AddScoped(svc, item.Impl);

        return services;
    }

}
