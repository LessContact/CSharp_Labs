using DatabaseSimulation.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DatabaseSimulation.HostedServices;

public class SimulationLifetimeService : IHostedService {
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly SimulationOptions _options;
    private readonly DisplayService _displayService;
    private readonly IStateLogger _stateLogger;
    private CancellationTokenSource? _cts;

    public SimulationLifetimeService(
        IHostApplicationLifetime appLifetime,
        IOptions<SimulationOptions> options,
        DisplayService displayService,
        IStateLogger stateLogger) {
        _appLifetime = appLifetime;
        _options = options.Value;
        _displayService = displayService;
        _stateLogger = stateLogger;
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        Console.WriteLine("===== СИМУЛЯЦИЯ С СОХРАНЕНИЕМ В БД =====\n");
        Console.WriteLine($"Длительность симуляции: {_options.DurationSeconds} секунд");
        Console.WriteLine($"Интервал обновления: {_options.DisplayUpdateInterval} мс\n");

        _cts = new CancellationTokenSource();
        
        _ = Task.Run(async () => {
            try {
                await Task.Delay(_options.DurationSeconds * 1000, _cts.Token);
                Console.WriteLine("\n\nВремя симуляции истекло. Завершение работы...\n");
                _appLifetime.StopApplication();
            }
            catch (OperationCanceledException) {
                // expected
            }
        });

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        _cts?.Cancel();
        _displayService.DisplayFinalState();
        await _stateLogger.CompleteAsync();
        Console.WriteLine("\nСимуляция завершена. Нажмите любую клавишу для выхода...");
    }
}

