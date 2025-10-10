namespace App.Models;
using DiningPhilosophers.Contracts;

public class Metrics {
    private readonly List<Philosopher> _philosophers;
    private readonly List<Fork> _forks;
    private readonly int _totalSteps;
    private readonly Dictionary<int, int> _forkUsageSteps = new();
    private readonly Dictionary<int, int> _forkEatingSteps = new();

    public Metrics(List<Philosopher> philosophers, List<Fork> forks, int totalSteps) {
        _philosophers = philosophers;
        _forks = forks;
        _totalSteps = totalSteps;

        foreach (var fork in forks) {
            _forkUsageSteps[fork.Id] = 0;
            _forkEatingSteps[fork.Id] = 0;
        }
    }

    public void RecordForkUsage() {
        foreach (var fork in _forks) {
            if (fork.State != ForkState.InUse) continue;
            
            _forkUsageSteps[fork.Id]++;

            if (!fork.UsedByPhilosopher.HasValue) continue;
            
            int philosopherId = fork.UsedByPhilosopher.Value;
            
            if (_philosophers[philosopherId].State is PhilosopherState.Eating) {
                _forkEatingSteps[fork.Id]++;
            }
        }
    }

    public void PrintMetrics() {
        Console.WriteLine("\n========== МЕТРИКИ ==========");

        int totalEaten = _philosophers.Sum(p => p.EatenCount);
        Console.WriteLine($"\nSCORE (общее количество съеденного): {totalEaten}");
        
        Console.WriteLine("\n--- Пропускная способность (за 1000 шагов) ---");
        foreach (var philosopher in _philosophers) {
            double throughput = (philosopher.EatenCount * 1000.0) / _totalSteps;
            Console.WriteLine($"  {philosopher.Name}: {throughput:F2}");
        }

        double avgThroughput = (totalEaten * 1000.0) / _totalSteps;
        Console.WriteLine($"  Среднее: {avgThroughput:F2}");

        Console.WriteLine("\n--- Время ожидания (в шагах) ---");
        int maxWaitSteps = 0;
        string? maxWaitPhilosopher = null;
        double totalWaitSteps = 0;

        foreach (var philosopher in _philosophers) {
            int waitSteps = philosopher.TotalHungrySteps;
            totalWaitSteps += waitSteps;
            Console.WriteLine($"  {philosopher.Name}: {waitSteps}");

            if (waitSteps > maxWaitSteps) {
                maxWaitSteps = waitSteps;
                maxWaitPhilosopher = philosopher.Name;
            }
        }

        double avgWaitSteps = totalWaitSteps / _philosophers.Count;
        Console.WriteLine($"  Среднее: {avgWaitSteps:F2}");
        Console.WriteLine($"  Максимальное: {maxWaitSteps} (философ: {maxWaitPhilosopher})");
        
        Console.WriteLine("\n--- Коэффициент утилизации вилок (%) ---");
        foreach (var fork in _forks) {
            double usagePercent = (_forkUsageSteps[fork.Id] * 100.0) / _totalSteps;
            double eatingPercent = (_forkEatingSteps[fork.Id] * 100.0) / _totalSteps;
            double availablePercent = 100.0 - usagePercent;
            double blockedPercent = usagePercent - eatingPercent;

            Console.WriteLine($"  Fork-{fork.Id + 1}:");
            Console.WriteLine($"    Свободна: {availablePercent:F2}%");
            Console.WriteLine($"    Заблокирована (взятие): {blockedPercent:F2}%");
            Console.WriteLine($"    Используется для еды: {eatingPercent:F2}%");
        }
    }
}