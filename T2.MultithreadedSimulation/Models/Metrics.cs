using DiningPhilosophers.Contracts;

namespace T2.MultithreadedSimulation.Models;

public class Metrics {
    private readonly List<Philosopher> _philosophers;
    private readonly List<Fork> _forks;
    private readonly long _totalDurationMs;

    public Metrics(List<Philosopher> philosophers, List<Fork> forks, long totalDurationMs) {
        _philosophers = philosophers;
        _forks = forks;
        _totalDurationMs = totalDurationMs;
    }

    public void PrintMetrics() {
        Console.WriteLine("\n========== МЕТРИКИ ==========");

        int totalEaten = _philosophers.Sum(p => p.EatenCount);
        Console.WriteLine($"\nSCORE (общее количество съеденного): {totalEaten}");

        Console.WriteLine("\n--- Пропускная способность (еда/миллисекунда) ---");
        foreach (var philosopher in _philosophers) {
            double throughput = philosopher.EatenCount / (double)_totalDurationMs;
            Console.WriteLine($"  {philosopher.Name}: {throughput:F6}");
        }

        double avgThroughput = totalEaten / (double)_totalDurationMs;
        Console.WriteLine($"  Среднее: {avgThroughput:F6}");

        Console.WriteLine("\n--- Среднее время ожидания (в миллисекундах) ---");
        long maxWaitMs = 0;
        string? maxWaitPhilosopher = null;
        long totalWaitMs = 0;

        foreach (var philosopher in _philosophers) {
            long waitMs = philosopher.TotalWaitingMs;
            double avgWait = philosopher.EatenCount > 0 ? waitMs / (double)philosopher.EatenCount : 0;
            totalWaitMs += waitMs;
            
            Console.WriteLine($"  {philosopher.Name}: {avgWait:F2} мс (общее: {waitMs} мс)");

            if (waitMs > maxWaitMs) {
                maxWaitMs = waitMs;
                maxWaitPhilosopher = philosopher.Name;
            }
        }

        double overallAvgWait = totalEaten > 0 ? totalWaitMs / (double)totalEaten : 0;
        Console.WriteLine($"  Среднее по всем: {overallAvgWait:F2} мс");
        Console.WriteLine($"  Максимальное общее: {maxWaitMs} мс (философ: {maxWaitPhilosopher})");

        Console.WriteLine("\n--- Коэффициент утилизации вилок (% по времени) ---");
        foreach (var fork in _forks) {
            var (available, blocked, eating) = fork.GetUtilizationPercent(_totalDurationMs);

            Console.WriteLine($"  Fork-{fork.Id + 1}:");
            Console.WriteLine($"    Свободна: {available:F2}%");
            Console.WriteLine($"    Заблокирована (взятие): {blocked:F2}%");
            Console.WriteLine($"    Используется для еды: {eating:F2}%");
        }
    }
}

