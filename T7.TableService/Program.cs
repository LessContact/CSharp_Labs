using T7.TableService.Services;

namespace T7.TableService;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        
        builder.Services.AddHealthChecks();
        
        var philosophersCount = builder.Configuration.GetValue<int>("PHILOSOPHERS_COUNT", 5);
        
        builder.Services.AddSingleton<ITableManager>(sp => 
            new TableManager(philosophersCount, sp.GetRequiredService<ILogger<TableManager>>()));
        
        builder.Services.AddHostedService<DisplayService>();

        var app = builder.Build();

        app.UseRouting();
        app.MapControllers();
        
        app.MapHealthChecks("/health");

        Console.WriteLine($"[T7] Table Service starting. Expected philosophers: {philosophersCount}");

        app.Run();
    }
}
