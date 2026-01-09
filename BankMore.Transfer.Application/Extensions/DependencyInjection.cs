using BankMore.Transfer.Application.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BankMore.Transfer.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.ScanRepositories(assembly);

        return services;
    }

    private static IServiceCollection ScanRepositories(this IServiceCollection services, Assembly assembly)
    {
        var implementations = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                Impl = t,
                Services = t.GetInterfaces().Where(i => typeof(ITransferService).IsAssignableFrom(i))
            })
            .Where(x => x.Services.Any());

        foreach (var item in implementations)
        {
            foreach (var svc in item.Services)
                services.AddScoped(svc, item.Impl);
        }

        return services;
    }
}


