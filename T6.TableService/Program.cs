using TableService.Services;

namespace TableService;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        
        // Добавляем Health Checks
        builder.Services.AddHealthChecks();

        // Получаем количество философов из переменных окружения или конфигурации
        var philosophersCount = builder.Configuration.GetValue<int>("PHILOSOPHERS_COUNT", 5);

        // Регистрируем TableManager как singleton
        builder.Services.AddSingleton<ITableManager>(sp => 
            new TableManager(philosophersCount, sp.GetRequiredService<ILogger<TableManager>>()));

        // Регистрируем DisplayService как hosted service
        builder.Services.AddHostedService<DisplayService>();

        var app = builder.Build();

        app.UseRouting();
        app.MapControllers();
        
        // Регистрируем health check endpoint
        app.MapHealthChecks("/health");

        Console.WriteLine($"Table Service starting. Expected philosophers: {philosophersCount}");

        app.Run();
    }
}

