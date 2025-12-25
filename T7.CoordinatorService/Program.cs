using EasyNetQ;
using T7.CoordinatorService.Services;

namespace T7.CoordinatorService;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddSingleton<SubscriptionsState>();
        builder.Services.AddHealthChecks()
            .AddCheck<CoordinatorHealthCheck>("coordinator_healthcheck");

        var options = new CoordinatorOptions {
            PhilosophersCount = int.Parse(Environment.GetEnvironmentVariable("PHILOSOPHERS_COUNT") ?? "5"),
            RabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            CoordinatorStepIntervalMs = int.Parse(Environment.GetEnvironmentVariable("COORDINATOR_STEP_INTERVAL_MS") ?? "10")
        };

        builder.Services.Configure<CoordinatorOptions>(opt => {
            opt.PhilosophersCount = options.PhilosophersCount;
            opt.RabbitMqHost = options.RabbitMqHost;
            opt.CoordinatorStepIntervalMs = options.CoordinatorStepIntervalMs;
        });

        var tableServiceUrl = Environment.GetEnvironmentVariable("TABLE_SERVICE_URL") ?? "http://localhost:8080";
        builder.Services.AddHttpClient<ITableServiceClient, TableServiceClient>(client => {
            client.BaseAddress = new Uri(tableServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        var connectionString = $"host={options.RabbitMqHost}";
        builder.Services.AddSingleton<IBus>(_ => RabbitHutch.CreateBus(connectionString));

        builder.Services.AddHostedService<CoordinatorWorker>();

        var app = builder.Build();

        app.UseRouting();
        app.MapControllers();
        app.MapHealthChecks("/health");

        Console.WriteLine($"[T7] Coordinator Service starting...");
        Console.WriteLine($"  RabbitMQ Host: {options.RabbitMqHost}");
        Console.WriteLine($"  Table Service: {tableServiceUrl}");
        Console.WriteLine($"  Expected philosophers: {options.PhilosophersCount}");

        app.Run();
    }
}
