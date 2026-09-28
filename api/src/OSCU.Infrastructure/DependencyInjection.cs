using Microsoft.Extensions.DependencyInjection;
using OSCU.Application.Common.Interfaces;
using OSCU.Infrastructure.Services;

namespace OSCU.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
