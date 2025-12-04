using DiningPhilosophers.Contracts;

namespace DatabaseSimulation.Services;

public interface IMetricsCollector {
    void RecordWaitingTime(int philosopherId, long waitingTimeMs);
    void RecordMeal(int philosopherId);
    PhilosopherMetrics GetMetrics(int philosopherId);
    void PrintFinalMetrics(ITableManager tableManager, long totalDurationMs);
}

public class PhilosopherMetrics {
    public int PhilosopherId { get; set; }
    public string PhilosopherName { get; set; } = "";
    public int EatenCount { get; set; }
    public long TotalWaitingMs { get; set; }
    public PhilosopherState CurrentState { get; set; }
    public bool HasLeftFork { get; set; }
    public bool HasRightFork { get; set; }
    public PhilosopherAction? LastAction { get; set; }
}

public class MetricsCollector : IMetricsCollector {
    private readonly Dictionary<int, PhilosopherMetrics> _metrics = new();
    private readonly Lock _lock = new();

    public void RecordWaitingTime(int philosopherId, long waitingTimeMs) {
        lock (_lock) {
            if (!_metrics.ContainsKey(philosopherId))
                _metrics[philosopherId] = new PhilosopherMetrics { PhilosopherId = philosopherId };

            _metrics[philosopherId].TotalWaitingMs += waitingTimeMs;
        }
    }

    public void RecordMeal(int philosopherId) {
        lock (_lock) {
            if (!_metrics.ContainsKey(philosopherId))
                _metrics[philosopherId] = new PhilosopherMetrics { PhilosopherId = philosopherId };

            _metrics[philosopherId].EatenCount++;
        }
    }

    public PhilosopherMetrics GetMetrics(int philosopherId) {
        lock (_lock) {
            if (!_metrics.ContainsKey(philosopherId))
                _metrics[philosopherId] = new PhilosopherMetrics { PhilosopherId = philosopherId };

            return _metrics[philosopherId];
        }
    }

    public void PrintFinalMetrics(ITableManager tableManager, long totalDurationMs) {
        lock (_lock) {
            Console.WriteLine("\n========== МЕТРИКИ ==========");

            var sortedMetrics = _metrics.Values.OrderBy(m => m.PhilosopherId).ToList();
            var totalEaten = sortedMetrics.Sum(m => m.EatenCount);
            Console.WriteLine($"\nSCORE (общее количество съеденного): {totalEaten}");

            Console.WriteLine("\n--- Пропускная способность (еда/миллисекунда) ---");
            foreach (var metric in sortedMetrics) {
                var throughput = metric.EatenCount / (double)totalDurationMs;
                Console.WriteLine($"  {metric.PhilosopherName}: {throughput:F6}");
            }

            var avgThroughput = totalEaten / (double)totalDurationMs;
            Console.WriteLine($"  Среднее: {avgThroughput:F6}");

            Console.WriteLine("\n--- Среднее время ожидания (в миллисекундах) ---");
            long maxWaitMs = 0;
            string? maxWaitPhilosopher = null;
            long totalWaitMs = 0;

            foreach (var metric in sortedMetrics) {
                var waitMs = metric.TotalWaitingMs;
                var avgWait = metric.EatenCount > 0 ? waitMs / (double)metric.EatenCount : 0;
                totalWaitMs += waitMs;

                Console.WriteLine($"  {metric.PhilosopherName}: {avgWait:F2} мс (общее: {waitMs} мс)");

                if (waitMs > maxWaitMs) {
                    maxWaitMs = waitMs;
                    maxWaitPhilosopher = metric.PhilosopherName;
                }
            }

            var overallAvgWait = totalEaten > 0 ? totalWaitMs / (double)totalEaten : 0;
            Console.WriteLine($"  Среднее по всем: {overallAvgWait:F2} мс");
            Console.WriteLine($"  Максимальное общее: {maxWaitMs} мс (философ: {maxWaitPhilosopher})");

            Console.WriteLine("\n--- Коэффициент утилизации вилок (% по времени) ---");
            var forks = tableManager.GetAllForks();
            foreach (var fork in forks) {
                var (available, blocked, eating) = fork.GetUtilizationPercent(totalDurationMs);

                Console.WriteLine($"  Fork-{fork.Id + 1}:");
                Console.WriteLine($"    Свободна: {available:F2}%");
                Console.WriteLine($"    Заблокирована (взятие): {blocked:F2}%");
                Console.WriteLine($"    Используется для еды: {eating:F2}%");
            }
        }
    }
}

