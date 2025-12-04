using DatabaseSimulation;
using DatabaseSimulation.Data;
using DatabaseSimulation.HostedServices;
using DatabaseSimulation.Services;
using DiningPhilosophers.Contracts;
using DiningPhilosophers.Strategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal class Program {
    private static async Task Main(string[] args) {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) => {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", false, false);
            })
            .ConfigureServices((context, services) => {
                services.Configure<SimulationOptions>(context.Configuration.GetSection("Simulation"));
                
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
                var philosopherNames = context.Configuration.GetSection("PhilosopherNames").Get<List<string>>();
                var philosopherCount = philosopherNames?.Count ?? 5;
                
                services.AddDbContext<SimulationDbContext>(options => 
                    options.UseNpgsql(connectionString));
                
                services.AddDbContextFactory<SimulationDbContext>(options => 
                    options.UseNpgsql(connectionString));
                
                services.AddSingleton<ITableManager>(new TableManager(philosopherCount));
                services.AddSingleton<IMetricsCollector, MetricsCollector>();
                services.AddSingleton<IStrategy, NaiveStrategy>();
                services.AddSingleton<IStateLogger, StateLogger>();

                services.AddSingleton<DisplayService>();
                services.AddHostedService(provider => provider.GetRequiredService<DisplayService>());

                services.AddHostedService<SimulationLifetimeService>();

                services.AddHostedService<AristotleHostedService>();
                services.AddHostedService<PlatoHostedService>();
                services.AddHostedService<SocratesHostedService>();
                services.AddHostedService<DescartesHostedService>();
                services.AddHostedService<KantHostedService>();
            })
            .Build();
        
        using (var scope = host.Services.CreateScope()) {
            var context = scope.ServiceProvider.GetRequiredService<SimulationDbContext>();
            await context.Database.MigrateAsync();
        }
        
        var stateLogger = host.Services.GetRequiredService<IStateLogger>();
        stateLogger.Initialize("NaiveStrategy", 5);

        await host.RunAsync();

        Console.ReadKey();
    }
}