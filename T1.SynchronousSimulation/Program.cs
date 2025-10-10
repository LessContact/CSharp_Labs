using DiningPhilosophers.Contracts;
using DiningPhilosophers.Strategies;

namespace App;

class Program {
    static void Main(string[] args) {
        Console.WriteLine("===== СИМУЛЯЦИЯ ОБЕДАЮЩИХ ФИЛОСОФОВ =====\n");
        
        var philosopherNames = ReadPhilosopherNames("philosophers.txt");

        if (philosopherNames.Count == 0) {
            Console.WriteLine("Ошибка: не удалось прочитать имена философов из файла.");
            return;
        }

        Console.WriteLine($"Загружено философов: {philosopherNames.Count}");
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
            totalSteps: 1000000, displayInterval: 100000);

        simulation.Run();

        Console.WriteLine("\nСимуляция завершена. Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }

    static List<string> ReadPhilosopherNames(string filename) {
        var names = new List<string>();

        try {
            if (File.Exists(filename)) {
                var lines = File.ReadAllLines(filename);
                foreach (var line in lines) {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed)) {
                        names.Add(trimmed);
                    }
                }
            }
        }
        catch (Exception e) {
            Console.WriteLine($"Ошибка при чтении файла: {e.Message}");
        }

        return names;
    }
}