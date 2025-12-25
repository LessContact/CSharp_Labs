using System.Text;

namespace T7.TableService.Services;

public class DisplayService : BackgroundService {
    private readonly ITableManager _tableManager;
    private readonly ILogger<DisplayService> _logger;
    private readonly int _displayIntervalMs;

    public DisplayService(
        ITableManager tableManager,
        IConfiguration configuration,
        ILogger<DisplayService> logger) {
        _tableManager = tableManager;
        _logger = logger;
        _displayIntervalMs = configuration.GetValue<int>("DisplayIntervalMs", 500);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested && 
               _tableManager.GetPhilosophersCount() < _tableManager.GetExpectedPhilosophersCount()) {
            await Task.Delay(100, stoppingToken);
        }
        
        _logger.LogInformation("All philosophers registered, starting display");

        try {
            while (!stoppingToken.IsCancellationRequested && !_tableManager.AllPhilosophersExited()) {
                await Task.Delay(_displayIntervalMs, stoppingToken);
                DisplayState();
            }
        }
        catch (OperationCanceledException) {
            // Expected
        }

        DisplayFinalMetrics();
    }

    private void DisplayState() {
        var status = _tableManager.GetStatus();
        var sb = new StringBuilder();

        sb.AppendLine($"\n===== ВРЕМЯ: {status.ElapsedMs} мс =====");
        sb.AppendLine("\nФилософы:");

        foreach (var philosopher in status.Philosophers) {
            sb.Append($"  {philosopher.PhilosopherName}: {philosopher.State}");

            if (philosopher.HasLeftFork || philosopher.HasRightFork) {
                sb.Append(" (");
                if (philosopher.HasLeftFork) sb.Append('L');
                if (philosopher.HasRightFork) sb.Append('R');
                sb.Append(')');
            }

            sb.AppendLine($", съедено: {philosopher.EatenCount}");
        }

        sb.AppendLine("\nВилки:");
        foreach (var fork in status.Forks) {
            sb.Append($"  Fork-{fork.Id + 1}: {fork.State}");
            if (fork.HeldByPhilosopher != null) {
                sb.Append($" (используется {fork.HeldByPhilosopher})");
            }
            sb.AppendLine();
        }

        Console.WriteLine(sb.ToString());
    }

    private void DisplayFinalMetrics() {
        var metrics = _tableManager.GetFinalMetrics();
        var sb = new StringBuilder();

        sb.AppendLine("\n========== ИТОГОВЫЕ МЕТРИКИ ==========");
        sb.AppendLine($"\nSCORE (общее количество съеденного): {metrics.TotalMeals}");
        sb.AppendLine($"Общее время симуляции: {metrics.TotalDurationMs} мс");

        sb.AppendLine("\n--- Пропускная способность (еда/миллисекунда) ---");
        foreach (var pm in metrics.PhilosopherMetrics) {
            sb.AppendLine($"  {pm.PhilosopherName}: {pm.Throughput:F6}");
        }
        sb.AppendLine($"  Среднее: {metrics.AverageThroughput:F6}");

        sb.AppendLine("\n--- Среднее время ожидания (в миллисекундах) ---");
        foreach (var pm in metrics.PhilosopherMetrics) {
            sb.AppendLine($"  {pm.PhilosopherName}: {pm.AverageWaitingMs:F2} мс (общее: {pm.TotalWaitingMs} мс)");
        }
        sb.AppendLine($"  Среднее по всем: {metrics.AverageWaitingTimeMs:F2} мс");
        sb.AppendLine($"  Максимальное общее: {metrics.MaxWaitingTimeMs} мс (философ: {metrics.MaxWaitingPhilosopher})");

        sb.AppendLine("\n--- Коэффициент утилизации вилок (% по времени) ---");
        foreach (var fu in metrics.ForkUtilizations) {
            sb.AppendLine($"  Fork-{fu.ForkId + 1}:");
            sb.AppendLine($"    Свободна: {fu.AvailablePercent:F2}%");
            sb.AppendLine($"    Заблокирована: {fu.BlockedPercent:F2}%");
            sb.AppendLine($"    Используется для еды: {fu.EatingPercent:F2}%");
        }

        sb.AppendLine("\n=== СИМУЛЯЦИЯ ЗАВЕРШЕНА ===");
        
        Console.WriteLine(sb.ToString());
    }
}
