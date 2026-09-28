using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OSCU.Application.Common.Interfaces;
using OSCU.Application.Features.Referrals;
using OSCU.Persistence.Repositories;

namespace OSCU.Persistence;

public static class DependencyInjection
{
    private const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddPersistenceDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            // Fail loudly at startup rather than handing null to Npgsql and
            // getting an obscure error on the first request.
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured. " +
                "Set ConnectionStrings:DefaultConnection in configuration, user secrets, " +
                "or the environment.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IReferralRepository, ReferralRepository>();

        // The context is the unit of work, so resolve the same scoped instance
        // rather than registering a second one.
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    /// <summary>
    /// Creates the database if it doesn't exist and applies any pending
    /// migrations. Call on the built service provider, not during registration.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
