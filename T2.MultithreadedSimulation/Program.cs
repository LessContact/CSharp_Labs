using DiningPhilosophers.Strategies;
using Microsoft.Extensions.Configuration;

namespace T2.MultithreadedSimulation;

class Program {
    static async Task Main(string[] args) {
        Console.WriteLine("===== МНОГОПОТОЧНАЯ СИМУЛЯЦИЯ ОБЕДАЮЩИХ ФИЛОСОФОВ =====\n");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var philosopherNames = configuration.GetSection("PhilosopherNames").Get<List<string>>();
        var simulationSettings = configuration.GetSection("SimulationSettings").Get<SimulationSettings>() ?? new SimulationSettings();

        if (philosopherNames == null || philosopherNames.Count == 0) {
            Console.WriteLine("Ошибка: не удалось прочитать имена философов из конфигурации.");
            return;
        }

        Console.WriteLine($"Количество философов: {philosopherNames.Count}");
        Console.WriteLine("Используется наивная стратегия (без координатора)\n");

        Console.WriteLine("Параметры симуляции:");
        Console.WriteLine($"  - Длительность симуляции: {simulationSettings.DurationMs} мс ({simulationSettings.DurationMs / 1000.0:F1} сек)");
        Console.WriteLine($"  - Отображение состояния: каждые {simulationSettings.DisplayIntervalMs} мс\n");

        var simulation = new Simulation(
            philosopherNames,
            _ => new NaiveStrategy(),
            durationMs: simulationSettings.DurationMs,
            displayIntervalMs: simulationSettings.DisplayIntervalMs
        );

        await simulation.RunAsync();
        
        Console.WriteLine("\nСимуляция завершена. Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
}
