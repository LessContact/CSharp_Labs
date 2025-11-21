using DiningPhilosophers.Contracts;
using DiningPhilosophers.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using T3.GenericHostSimulator;
using T3.GenericHostSimulator.HostedServices;
using T3.GenericHostSimulator.Services;

internal class Program {
    private static async Task Main(string[] args) {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) => {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", false, false);
            })
            .ConfigureServices((context, services) => {
                services.Configure<SimulationOptions>(context.Configuration.GetSection("Simulation"));
                
                var philosopherNames = context.Configuration.GetSection("PhilosopherNames").Get<List<string>>();
                var philosopherCount = philosopherNames?.Count ?? 5;

                services.AddSingleton<ITableManager>(new TableManager(philosopherCount));
                services.AddSingleton<IMetricsCollector, MetricsCollector>();
                services.AddSingleton<IStrategy, NaiveStrategy>();

                // double register display service: once as singleton, second as background service
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

        await host.RunAsync();

        Console.ReadKey();
    }
}