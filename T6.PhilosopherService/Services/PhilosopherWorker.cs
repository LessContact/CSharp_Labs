using DiningPhilosophers.Contracts;
using Microsoft.Extensions.Options;

namespace PhilosopherService.Services;

public class PhilosopherWorker : BackgroundService {
    private readonly ITableServiceClient _tableClient;
    private readonly PhilosopherOptions _options;
    private readonly ILogger<PhilosopherWorker> _logger;
    private readonly IStrategy _strategy;
    private readonly Random _random;

    private PhilosopherState _state = PhilosopherState.Thinking;
    private bool _hasLeftFork;
    private bool _hasRightFork;
    private int _eatenCount;
    private DateTime _hungryStartTime;

    public PhilosopherWorker(
        ITableServiceClient tableClient,
        IOptions<PhilosopherOptions> options,
        IStrategy strategy,
        ILogger<PhilosopherWorker> logger) {
        _tableClient = tableClient;
        _options = options.Value;
        _strategy = strategy;
        _logger = logger;
        _random = new Random();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("Philosopher {Name} ({Id}) starting...", 
            _options.PhilosopherName, _options.PhilosopherId);
        
        var registered = false;
        while (!registered && !stoppingToken.IsCancellationRequested) {
            registered = await _tableClient.RegisterAsync(
                _options.PhilosopherId,
                _options.PhilosopherName,
                _options.LeftForkId,
                _options.RightForkId);

            if (!registered) {
                _logger.LogWarning("Failed to register, retrying in 1 second...");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Philosopher {Name} registered successfully", _options.PhilosopherName);
        
        var endTime = DateTime.UtcNow.AddMinutes(_options.SimulationDurationMinutes);

        try {
            while (!stoppingToken.IsCancellationRequested && DateTime.UtcNow < endTime) {
                await UpdateStateOnTableAsync();

                switch (_state) {
                    case PhilosopherState.Thinking:
                        await ThinkAsync(stoppingToken);
                        break;
                    case PhilosopherState.Hungry:
                        await TryToEatAsync(stoppingToken);
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
            // Освобождаем вилки при выходе
            await ReleaseForksAsync();

            // Уведомляем стол о выходе
            await _tableClient.NotifyExitAsync(_options.PhilosopherId, _options.PhilosopherName, _eatenCount);
            _logger.LogInformation("Philosopher {Name} exited with {Meals} meals", 
                _options.PhilosopherName, _eatenCount);
        }
    }

    private async Task ThinkAsync(CancellationToken stoppingToken) {
        var thinkingTime = _random.Next(_options.ThinkingTimeMinMs, _options.ThinkingTimeMaxMs + 1);
        await Task.Delay(thinkingTime, stoppingToken);

        _state = PhilosopherState.Hungry;
        _hungryStartTime = DateTime.UtcNow;
        _logger.LogDebug("Philosopher {Name} is now hungry", _options.PhilosopherName);
    }

    private async Task TryToEatAsync(CancellationToken stoppingToken) {
        var leftForkTask = _tableClient.GetForkStateAsync(_options.LeftForkId);
        var rightForkTask = _tableClient.GetForkStateAsync(_options.RightForkId);
        
        await Task.WhenAll(leftForkTask, rightForkTask);
        
        var leftForkInfo = leftForkTask.Result;
        var rightForkInfo = rightForkTask.Result;

        if (leftForkInfo == null || rightForkInfo == null) {
            await Task.Delay(_options.RetryDelayMs, stoppingToken);
            return;
        }
        
        var action = _strategy.DecideAction(leftForkInfo, rightForkInfo, _state, _hasLeftFork, _hasRightFork);
        
        switch (action) {
            case PhilosopherAction.TakeLeftFork:
                await Task.Delay(_options.ForkAcquisitionTimeMs, stoppingToken);
                var takeLeftResult = await _tableClient.TakeForkAsync(_options.PhilosopherId, _options.PhilosopherName, _options.LeftForkId);
                if (takeLeftResult.Success) {
                    _hasLeftFork = true;
                    _logger.LogDebug("Philosopher {Name} took left fork {ForkId}", _options.PhilosopherName, _options.LeftForkId);
                }
                break;

            case PhilosopherAction.TakeRightFork:
                await Task.Delay(_options.ForkAcquisitionTimeMs, stoppingToken);
                var takeRightResult = await _tableClient.TakeForkAsync(_options.PhilosopherId, _options.PhilosopherName, _options.RightForkId);
                if (takeRightResult.Success) {
                    _hasRightFork = true;
                    _logger.LogDebug("Philosopher {Name} took right fork {ForkId}", _options.PhilosopherName, _options.RightForkId);
                }
                break;

            case PhilosopherAction.ReleaseLeftFork:
                await _tableClient.ReleaseForkAsync(_options.PhilosopherId, _options.LeftForkId);
                _hasLeftFork = false;
                _logger.LogDebug("Philosopher {Name} released left fork {ForkId}", _options.PhilosopherName, _options.LeftForkId);
                break;

            case PhilosopherAction.ReleaseRightFork:
                await _tableClient.ReleaseForkAsync(_options.PhilosopherId, _options.RightForkId);
                _hasRightFork = false;
                _logger.LogDebug("Philosopher {Name} released right fork {ForkId}", _options.PhilosopherName, _options.RightForkId);
                break;
            
            case PhilosopherAction.ReleaseBothForks:
                await ReleaseForksAsync();
                _logger.LogDebug("Philosopher {Name} released both forks ({ForkId}, {ForkId2})", _options.PhilosopherName, _options.RightForkId, _options.LeftForkId);
                break;

            case PhilosopherAction.None:
                break;
        }
        
        if (_hasLeftFork && _hasRightFork) {
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
        else {
            await Task.Delay(_options.RetryDelayMs, stoppingToken);
        }
    }

    private async Task EatAsync(CancellationToken stoppingToken) {
        var eatingTime = _random.Next(_options.EatingTimeMinMs, _options.EatingTimeMaxMs + 1);
        await Task.Delay(eatingTime, stoppingToken);

        _eatenCount++;
        _logger.LogDebug("Philosopher {Name} finished eating (total: {Count})", _options.PhilosopherName, _eatenCount);

        await ReleaseForksAsync();
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

