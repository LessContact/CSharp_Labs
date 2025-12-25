using DiningPhilosophers.Contracts;
using DiningPhilosophers.Contracts.Messages;
using EasyNetQ;
using Microsoft.Extensions.Options;

namespace T7.PhilosopherService.Services;

public class PhilosopherWorker : BackgroundService {
    private readonly ITableServiceClient _tableClient;
    private readonly IBus _bus;
    private readonly PhilosopherOptions _options;
    private readonly ILogger<PhilosopherWorker> _logger;
    private readonly Random _random;

    private PhilosopherState _state = PhilosopherState.Thinking;
    private bool _hasLeftFork;
    private bool _hasRightFork;
    private int _eatenCount;
    private DateTime _hungryStartTime;

    private bool _canTakeForks;
    private readonly SemaphoreSlim _coordinatorSignal = new(0);

    public PhilosopherWorker(
        ITableServiceClient tableClient,
        IBus bus,
        IOptions<PhilosopherOptions> options,
        ILogger<PhilosopherWorker> logger) {
        _tableClient = tableClient;
        _bus = bus;
        _options = options.Value;
        _logger = logger;
        _random = new Random();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("Philosopher {Name} ({Id}) starting...", 
            _options.PhilosopherName, _options.PhilosopherId);

        await _bus.PubSub.SubscribeAsync<TakeForksCommand>(
            $"philosopher_{_options.PhilosopherId}",
            HandleTakeForksCommand);

        var registered = false;
        while (!registered && !stoppingToken.IsCancellationRequested) {
            registered = await _tableClient.RegisterAsync(
                _options.PhilosopherId,
                _options.PhilosopherName,
                _options.LeftForkId,
                _options.RightForkId);

            if (!registered) {
                _logger.LogWarning("Failed to register on table, retrying in 1 second...");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Philosopher {Name} registered on table", _options.PhilosopherName);

        await _bus.PubSub.PublishAsync(new PhilosopherRegisteredMessage {
            PhilosopherId = _options.PhilosopherId,
            PhilosopherName = _options.PhilosopherName,
            LeftForkId = _options.LeftForkId,
            RightForkId = _options.RightForkId
        }, cancellationToken: stoppingToken);

        _logger.LogInformation("Philosopher {Name} notified coordinator", _options.PhilosopherName);

        var endTime = DateTime.UtcNow.AddMinutes(_options.SimulationDurationMinutes);

        try {
            while (!stoppingToken.IsCancellationRequested && DateTime.UtcNow < endTime) {
                await UpdateStateOnTableAsync();

                switch (_state) {
                    case PhilosopherState.Thinking:
                        await ThinkAsync(stoppingToken);
                        break;
                    case PhilosopherState.Hungry:
                        await WaitForCoordinatorAndStartEatingAsync(stoppingToken);
                        break;
                    case PhilosopherState.Eating:
                        await EatAsync(stoppingToken);
                        break;
                }
            }
        }
        catch (OperationCanceledException) {
            // Expected when stopping
        }
        finally {
            await ReleaseForksAsync();

            await _bus.PubSub.PublishAsync(new PhilosopherExitedMessage {
                PhilosopherId = _options.PhilosopherId,
                PhilosopherName = _options.PhilosopherName,
                TotalMeals = _eatenCount
            });

            await _tableClient.NotifyExitAsync(_options.PhilosopherId, _options.PhilosopherName, _eatenCount);
            
            _logger.LogInformation("Philosopher {Name} exited with {Meals} meals", 
                _options.PhilosopherName, _eatenCount);
        }
    }

    private Task HandleTakeForksCommand(TakeForksCommand command) {
        if (command.PhilosopherId != _options.PhilosopherId) {
            return Task.CompletedTask;
        }

        _logger.LogDebug("Philosopher {Name} received permission to take forks", _options.PhilosopherName);
        _canTakeForks = true;
        _coordinatorSignal.Release();
        return Task.CompletedTask;
    }

    private async Task ThinkAsync(CancellationToken stoppingToken) {
        var thinkingTime = _random.Next(_options.ThinkingTimeMinMs, _options.ThinkingTimeMaxMs + 1);
        await Task.Delay(thinkingTime, stoppingToken);

        _state = PhilosopherState.Hungry;
        _hungryStartTime = DateTime.UtcNow;
        _canTakeForks = false;
        
        _logger.LogDebug("Philosopher {Name} is now hungry", _options.PhilosopherName);

        await _bus.PubSub.PublishAsync(new HungryMessage {
            PhilosopherId = _options.PhilosopherId,
            PhilosopherName = _options.PhilosopherName,
            LeftForkId = _options.LeftForkId,
            RightForkId = _options.RightForkId
        }, cancellationToken: stoppingToken);
    }

    private async Task WaitForCoordinatorAndStartEatingAsync(CancellationToken stoppingToken) {
        while (!_canTakeForks && !stoppingToken.IsCancellationRequested) {
            try {
                await _coordinatorSignal.WaitAsync(TimeSpan.FromMilliseconds(100), stoppingToken);
            }
            catch (OperationCanceledException) {
                return;
            }
        }

        if (stoppingToken.IsCancellationRequested) return;

        _canTakeForks = false;

        await Task.Delay(_options.ForkAcquisitionTimeMs, stoppingToken);
        var leftResult = await _tableClient.TakeForkAsync(_options.PhilosopherId, _options.PhilosopherName, _options.LeftForkId);
        if (leftResult.Success) {
            _hasLeftFork = true;
            _logger.LogDebug("Philosopher {Name} took left fork {ForkId}", _options.PhilosopherName, _options.LeftForkId);
        }

        await Task.Delay(_options.ForkAcquisitionTimeMs, stoppingToken);
        var rightResult = await _tableClient.TakeForkAsync(_options.PhilosopherId, _options.PhilosopherName, _options.RightForkId);
        if (rightResult.Success) {
            _hasRightFork = true;
            _logger.LogDebug("Philosopher {Name} took right fork {ForkId}", _options.PhilosopherName, _options.RightForkId);
        }

        _state = PhilosopherState.Eating;
        var waitingTime = (long)(DateTime.UtcNow - _hungryStartTime).TotalMilliseconds;

        await _tableClient.NotifyStartEatingAsync(
            _options.PhilosopherId,
            _options.PhilosopherName,
            _options.LeftForkId,
            _options.RightForkId);

        await _tableClient.RecordMealAsync(_options.PhilosopherId, _options.PhilosopherName, waitingTime);
        _logger.LogDebug("Philosopher {Name} started eating after {WaitMs}ms", _options.PhilosopherName, waitingTime);
    }

    private async Task EatAsync(CancellationToken stoppingToken) {
        var eatingTime = _random.Next(_options.EatingTimeMinMs, _options.EatingTimeMaxMs + 1);
        await Task.Delay(eatingTime, stoppingToken);

        _eatenCount++;
        _logger.LogDebug("Philosopher {Name} finished eating (total: {Count})", _options.PhilosopherName, _eatenCount);

        await ReleaseForksAsync();

        await _bus.PubSub.PublishAsync(new FinishedEatingMessage {
            PhilosopherId = _options.PhilosopherId,
            PhilosopherName = _options.PhilosopherName,
            LeftForkId = _options.LeftForkId,
            RightForkId = _options.RightForkId
        }, cancellationToken: stoppingToken);

        _state = PhilosopherState.Thinking;
    }

    private async Task ReleaseForksAsync() {
        if (_hasLeftFork) {
            await _tableClient.ReleaseForkAsync(_options.PhilosopherId, _options.LeftForkId);
            _hasLeftFork = false;
        }

        if (_hasRightFork) {
            await _tableClient.ReleaseForkAsync(_options.PhilosopherId, _options.RightForkId);
            _hasRightFork = false;
        }
    }

    private async Task UpdateStateOnTableAsync() {
        await _tableClient.UpdateStateAsync(
            _options.PhilosopherId,
            _state,
            _hasLeftFork,
            _hasRightFork,
            _eatenCount);
    }
}
