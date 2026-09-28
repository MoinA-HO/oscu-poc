using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OSCU.Persistence;
using Testcontainers.PostgreSql;

namespace OSCU.Api.IntegrationTests;

/// <summary>
/// Boots the real API against a throwaway PostgreSQL container.
/// </summary>
/// <remarks>
/// Real Postgres, not the EF in-memory provider. The in-memory provider does
/// not enforce unique indexes, has no <c>timestamp with time zone</c> type,
/// and translates LINQ differently — so a suite built on it passes while the
/// behaviour that actually matters goes untested.
///
/// The container also runs the EF migrations, so every test run proves the
/// migration applies cleanly to an empty database.
///
/// Requires a running Docker daemon.
/// </remarks>
public class ReferralApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        // Pinned to the same major version as docker-compose and Azure
        // Database for PostgreSQL, so tests exercise the deployed engine.
        .WithImage("postgres:18")
        .WithDatabase("referral_test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitialiseAsync()
    {
        // Must complete before Services is touched: accessing Services builds
        // the host, which calls GetConnectionString() on this container.
        await _database.StartAsync();

        using IServiceScope scope = Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();
    }

    /// <summary>Empties the tables so each test starts from a known state.</summary>
    public async Task ResetAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE referrals RESTART IDENTITY CASCADE;");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Replace the DbContext registration outright rather than trying to
        // override ConnectionStrings:DefaultConnection in configuration.
        //
        // Program.cs reads the connection string eagerly, inside
        // AddPersistenceDependencies(builder.Configuration), at service
        // registration time. Any configuration source added from here lands
        // too late to affect the DbContextOptions that were already built, so
        // the tests silently connected to the docker-compose database on
        // localhost:5432 instead of the throwaway container.
        //
        // ConfigureTestServices runs after the application's own registrations,
        // so this always wins.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_database.GetConnectionString(), npgsql =>
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}
