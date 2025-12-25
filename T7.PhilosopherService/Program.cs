using EasyNetQ;
using T7.PhilosopherService.Services;

namespace T7.PhilosopherService;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddHealthChecks();

        var options = new PhilosopherOptions {
            PhilosopherId = Environment.GetEnvironmentVariable("PHILOSOPHER_ID") ?? "philosopher-0",
            PhilosopherName = Environment.GetEnvironmentVariable("PHILOSOPHER_NAME") ?? "Unknown",
            LeftForkId = int.Parse(Environment.GetEnvironmentVariable("LEFT_FORK_ID") ?? "0"),
            RightForkId = int.Parse(Environment.GetEnvironmentVariable("RIGHT_FORK_ID") ?? "1"),
            SimulationDurationMinutes = int.Parse(Environment.GetEnvironmentVariable("SIMULATION_DURATION_MINUTES") ?? "5"),
            ThinkingTimeMinMs = int.Parse(Environment.GetEnvironmentVariable("THINKING_TIME_MIN_MS") ?? "100"),
            ThinkingTimeMaxMs = int.Parse(Environment.GetEnvironmentVariable("THINKING_TIME_MAX_MS") ?? "500"),
            EatingTimeMinMs = int.Parse(Environment.GetEnvironmentVariable("EATING_TIME_MIN_MS") ?? "100"),
            EatingTimeMaxMs = int.Parse(Environment.GetEnvironmentVariable("EATING_TIME_MAX_MS") ?? "300"),
            ForkAcquisitionTimeMs = int.Parse(Environment.GetEnvironmentVariable("FORK_ACQUISITION_TIME_MS") ?? "50"),
            RetryDelayMs = int.Parse(Environment.GetEnvironmentVariable("RETRY_DELAY_MS") ?? "10"),
            RabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost"
        };

        builder.Services.Configure<PhilosopherOptions>(opt => {
            opt.PhilosopherId = options.PhilosopherId;
            opt.PhilosopherName = options.PhilosopherName;
            opt.LeftForkId = options.LeftForkId;
            opt.RightForkId = options.RightForkId;
            opt.SimulationDurationMinutes = options.SimulationDurationMinutes;
            opt.ThinkingTimeMinMs = options.ThinkingTimeMinMs;
            opt.ThinkingTimeMaxMs = options.ThinkingTimeMaxMs;
            opt.EatingTimeMinMs = options.EatingTimeMinMs;
            opt.EatingTimeMaxMs = options.EatingTimeMaxMs;
            opt.ForkAcquisitionTimeMs = options.ForkAcquisitionTimeMs;
            opt.RetryDelayMs = options.RetryDelayMs;
            opt.RabbitMqHost = options.RabbitMqHost;
        });

        var tableServiceUrl = Environment.GetEnvironmentVariable("TABLE_SERVICE_URL") ?? "http://localhost:8080";
        builder.Services.AddHttpClient<ITableServiceClient, TableServiceClient>(client => {
            client.BaseAddress = new Uri(tableServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        var connectionString = $"host={options.RabbitMqHost}";
        builder.Services.AddSingleton<IBus>(_ => RabbitHutch.CreateBus(connectionString));

        builder.Services.AddHostedService<PhilosopherWorker>();

        var app = builder.Build();

        app.UseRouting();
        app.MapControllers();
        
        app.MapHealthChecks("/health");

        Console.WriteLine($"[T7] Philosopher {options.PhilosopherName} ({options.PhilosopherId}) starting...");
        Console.WriteLine($"  Left Fork: {options.LeftForkId}, Right Fork: {options.RightForkId}");
        Console.WriteLine($"  Table Service: {tableServiceUrl}");
        Console.WriteLine($"  RabbitMQ Host: {options.RabbitMqHost}");
        Console.WriteLine($"  Duration: {options.SimulationDurationMinutes} minutes");

        app.Run();
    }
}
