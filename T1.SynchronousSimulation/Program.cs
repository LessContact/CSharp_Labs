using DiningPhilosophers.Contracts;
using DiningPhilosophers.Strategies;
using Microsoft.Extensions.Configuration;

namespace T1.SynchronousSimulation;

class Program {
    static void Main(string[] args) {
        Console.WriteLine("===== СИМУЛЯЦИЯ ОБЕДАЮЩИХ ФИЛОСОФОВ =====\n");
        
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

        Console.WriteLine($"Загружено философов: {philosopherNames.Count}");
        Console.WriteLine($"Параметры симуляции: {simulationSettings.TotalSteps:N0} шагов, отображение каждые {simulationSettings.DisplayInterval:N0} шагов");
        Console.WriteLine();
        
        Console.WriteLine("Выберите стратегию:");
        Console.WriteLine("1 - Наивная стратегия без координатора");
        Console.WriteLine("2 - Стратегия с координатором");
        Console.WriteLine();

        var choice = Console.ReadLine();

        ICoordinator? coordinator = null;
        Func<int, IStrategy> strategyFactory;

        switch (choice) {
            case "1":
                Console.WriteLine("\nИспользуется наивная стратегия\n");
                strategyFactory = _ => new NaiveStrategy();
                break;
            case "2":
                Console.WriteLine("\nИспользуется стратегия с координатором\n");
                coordinator = new Coordinator(philosopherNames.Count);
                strategyFactory = _ => new CoordinatorStrategy();
                break;
            default:
                Console.WriteLine("\nВыбрана неизвестная стратегия. Exiting...");
                return;
        }
        
        var simulation = new Simulation(philosopherNames, strategyFactory, coordinator,
            totalSteps: simulationSettings.TotalSteps, displayInterval: simulationSettings.DisplayInterval);

        simulation.Run();

        Console.WriteLine("\nСимуляция завершена. Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
}