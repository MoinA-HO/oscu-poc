using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OSCU.Application.Features.Referrals;

namespace OSCU.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the application use cases and every FluentValidation
    /// validator in this assembly.
    /// </summary>
    /// <remarks>
    /// Validators are discovered by assembly scan, so adding a new feature
    /// means adding a validator class and nothing else. Services are
    /// registered explicitly: the list is short, the wiring is greppable, and
    /// a convention-based scan would happily register things nobody meant to
    /// expose.
    /// </remarks>
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);

        services.AddScoped<IReferralService, ReferralService>();

        return services;
    }
}
