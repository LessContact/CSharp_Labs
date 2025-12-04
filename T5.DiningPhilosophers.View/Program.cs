using System.CommandLine;
using DatabaseSimulation.Data;
using DatabaseSimulation.Data.Entities;
using DiningPhilosophers.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DiningPhilosophers.View;

class Program {
    static async Task<int> Main(string[] args) {
        var runIdOption = new Option<int>(
            name: "--runId",
            description: "Уникальный идентификатор запуска симуляции") {
            IsRequired = true
        };

        var delayOption = new Option<double>(
            name: "--delay",
            description: "Смещение в секундах от начала симуляции") {
            IsRequired = true
        };

        var rootCommand = new RootCommand("Просмотр состояния симуляции 'Обедающие философы'");
        rootCommand.AddOption(runIdOption);
        rootCommand.AddOption(delayOption);

        rootCommand.SetHandler(async (runId, delay) => {
            await DisplaySimulationState(runId, delay);
        }, runIdOption, delayOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task DisplaySimulationState(int runId, double delaySeconds) {
        try {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            var optionsBuilder = new DbContextOptionsBuilder<SimulationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            await using var context = new SimulationDbContext(optionsBuilder.Options);
            
            var simulationRun = await context.SimulationRuns
                .FirstOrDefaultAsync(sr => sr.Id == runId);

            if (simulationRun == null) {
                Console.WriteLine($"Ошибка: Симуляция с runId = {runId} не найдена.");
                return;
            }
            
            var targetTimestampMs = (long)(delaySeconds * 1000);

            Console.WriteLine($"===== Состояние симуляции (runId: {runId}, время: {delaySeconds:F2}s) =====");
            Console.WriteLine($"Стратегия: {simulationRun.StrategyName}");
            Console.WriteLine($"Начало симуляции: {simulationRun.StartTime:yyyy-MM-dd HH:mm:ss}");
            if (simulationRun.EndTime.HasValue) {
                Console.WriteLine($"Окончание симуляции: {simulationRun.EndTime:yyyy-MM-dd HH:mm:ss}");
            }
            Console.WriteLine();
            
            await DisplayPhilosophersState(context, runId, targetTimestampMs, simulationRun.PhilosopherCount);
            
            Console.WriteLine();

            await DisplayForksState(context, runId, targetTimestampMs, simulationRun.PhilosopherCount);
        }
        catch (Exception ex) {
            Console.WriteLine($"Ошибка при получении данных: {ex.Message}");
            Console.Out.Flush();
        }
    }

    private static async Task DisplayPhilosophersState(
        SimulationDbContext context, 
        int runId, 
        long targetTimestampMs,
        int philosopherCount) {
        
        Console.WriteLine("Философы:");
        
        for (int philosopherId = 0; philosopherId < philosopherCount; philosopherId++) {
            var latestState = await context.PhilosopherStateLogs
                .Where(p => p.SimulationRunId == runId 
                            && p.PhilosopherId == philosopherId 
                            && p.TimestampMs <= targetTimestampMs)
                .OrderByDescending(p => p.TimestampMs)
                .FirstOrDefaultAsync();

            if (latestState == null) {
                Console.WriteLine($"  Философ {philosopherId}: Нет данных");
                continue;
            }

            var stateStr = FormatPhilosopherState(latestState);
            var forksInfo = GetForksInfo(latestState);
            var actionStr = latestState.Action.HasValue && latestState.Action != PhilosopherAction.None 
                ? $" (Action = {latestState.Action})" 
                : "";

            Console.WriteLine($"  {latestState.PhilosopherName}: {stateStr}{actionStr}{forksInfo}, съедено: {latestState.EatenCount}");
        }
    }

    private static string FormatPhilosopherState(PhilosopherStateLog state) {
        return state.State switch {
            PhilosopherState.Thinking => "Thinking",
            PhilosopherState.Hungry => "Hungry",
            PhilosopherState.Eating => "Eating",
            _ => state.State.ToString()
        };
    }

    private static string GetForksInfo(PhilosopherStateLog state) {
        var forks = new List<string>();
        if (state.HasLeftFork) forks.Add("левая вилка");
        if (state.HasRightFork) forks.Add("правая вилка");
        
        return forks.Count > 0 ? $" [{string.Join(", ", forks)}]" : "";
    }

    private static async Task DisplayForksState(
        SimulationDbContext context, 
        int runId, 
        long targetTimestampMs,
        int forkCount) {
        
        Console.WriteLine("Вилки:");
        
        for (int forkId = 0; forkId < forkCount; forkId++) {
            var latestState = await context.ForkStateLogs
                .Where(f => f.SimulationRunId == runId 
                            && f.ForkId == forkId 
                            && f.TimestampMs <= targetTimestampMs)
                .OrderByDescending(f => f.TimestampMs)
                .FirstOrDefaultAsync();

            if (latestState == null) {
                Console.WriteLine($"  Fork-{forkId + 1}: Нет данных");
                continue;
            }

            var stateStr = latestState.State switch {
                ForkState.Available => "Available",
                ForkState.InUse => $"InUse (используется {latestState.UsedByPhilosopherName ?? "неизвестно"})",
                _ => latestState.State.ToString()
            };

            Console.WriteLine($"  Fork-{forkId + 1}: {stateStr}");
        }
    }
}

