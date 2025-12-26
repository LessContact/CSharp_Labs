using DiningPhilosophers.Contracts;
using DiningPhilosophers.Contracts.Messages;
using EasyNetQ;
using Microsoft.Extensions.Options;

namespace T7.CoordinatorService.Services;

public class CoordinatorWorker : BackgroundService {
    private readonly IBus _bus;
    private readonly ITableServiceClient _tableClient;
    private readonly ILogger<CoordinatorWorker> _logger;
    private readonly CoordinatorOptions _options;
    private readonly SubscriptionsState _subscriptions;

    private readonly Dictionary<string, PhilosopherCoordinatorState> _philosophers = new();
    private readonly Queue<string> _hungryQueue = new();
    private readonly Lock _lock = new();

    private readonly SemaphoreSlim _workSignal = new(0, int.MaxValue);
    private int _isDraining;

    private int _registeredCount;
    private bool _started;

    public CoordinatorWorker(
        IBus bus,
        ITableServiceClient tableClient,
        IOptions<CoordinatorOptions> options,
        ILogger<CoordinatorWorker> logger,
        SubscriptionsState subscriptions) {
        _bus = bus;
        _tableClient = tableClient;
        _options = options.Value;
        _logger = logger;
        _subscriptions = subscriptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("Coordinator starting, waiting for {Count} philosophers...", _options.PhilosophersCount);

        try {
            await SubscribeAllAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested) {
                await _workSignal.WaitAsync(stoppingToken);
                await StartDrainingIfNeeded(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
        }
        catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested) {
        }
    }

    private async Task SubscribeAllAsync(CancellationToken stoppingToken) {
        const int maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++) {
            stoppingToken.ThrowIfCancellationRequested();
            _subscriptions.Reset();

            try {
                await _bus.PubSub.SubscribeAsync<PhilosopherRegisteredMessage>(
                    "coordinator",
                    HandlePhilosopherRegistered,
                    stoppingToken);
                _subscriptions.MarkSubscribed();

                await _bus.PubSub.SubscribeAsync<HungryMessage>(
                    "coordinator",
                    HandleHungryMessage,
                    stoppingToken);
                _subscriptions.MarkSubscribed();

                await _bus.PubSub.SubscribeAsync<FinishedEatingMessage>(
                    "coordinator",
                    HandleFinishedEating,
                    stoppingToken);
                _subscriptions.MarkSubscribed();

                await _bus.PubSub.SubscribeAsync<PhilosopherExitedMessage>(
                    "coordinator",
                    HandlePhilosopherExited,
                    stoppingToken);
                _subscriptions.MarkSubscribed();

                _logger.LogInformation("Coordinator subscribed to messages");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                throw;
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested) {
                throw;
            }
            catch (Exception ex) {
                if (attempt >= maxAttempts) {
                    _logger.LogError(ex, "Failed to subscribe to coordinator messages after {Attempts} attempts", attempt);
                    throw;
                }

                _logger.LogWarning(ex, "Failed to subscribe to coordinator messages (attempt {Attempt}/{Max}). Retrying...",
                    attempt, maxAttempts);

                try {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                    throw;
                }
            }
        }
    }

    private async Task StartDrainingIfNeeded(CancellationToken stoppingToken) {
        if (Interlocked.CompareExchange(ref _isDraining, 1, 0) != 0) {
            return;
        }

        try {
            while (!stoppingToken.IsCancellationRequested) {
                bool hasWork;
                lock (_lock) {
                    hasWork = _started && _hungryQueue.Count > 0;
                }

                if (!hasWork) {
                    return;
                }

                await ProcessHungryQueueAsync();
            }
        }
        finally {
            Interlocked.Exchange(ref _isDraining, 0);

            bool hasWork;
            lock (_lock) {
                hasWork = _started && _hungryQueue.Count > 0;
            }

            if (hasWork) {
                SignalWork();
            }
        }
    }

    private void SignalWork() {
        _workSignal.Release();
    }

    private Task HandlePhilosopherRegistered(PhilosopherRegisteredMessage message) {
        var startedNow = false;

        lock (_lock) {
            if (_philosophers.ContainsKey(message.PhilosopherId)) {
                _logger.LogWarning("Philosopher {Id} already registered", message.PhilosopherId);
                return Task.CompletedTask;
            }

            _philosophers[message.PhilosopherId] = new PhilosopherCoordinatorState {
                PhilosopherId = message.PhilosopherId,
                PhilosopherName = message.PhilosopherName,
                LeftForkId = message.LeftForkId,
                RightForkId = message.RightForkId
            };

            _registeredCount++;
            _logger.LogInformation("Philosopher {Name} registered ({Count}/{Expected})",
                message.PhilosopherName, _registeredCount, _options.PhilosophersCount);

            if (!_started && _registeredCount >= _options.PhilosophersCount) {
                _started = true;
                startedNow = true;
                _logger.LogInformation("All philosophers registered. Coordinator started!");
            }
        }

        if (startedNow) {
            SignalWork();
        }

        return Task.CompletedTask;
    }

    private Task HandleHungryMessage(HungryMessage message) {
        lock (_lock) {
            if (!_philosophers.TryGetValue(message.PhilosopherId, out var state)) {
                _logger.LogWarning("Unknown philosopher {Id} sent hungry message", message.PhilosopherId);
                return Task.CompletedTask;
            }

            if (state.IsWaitingForForks || state.IsEating) {
                return Task.CompletedTask;
            }

            state.IsWaitingForForks = true;
            _hungryQueue.Enqueue(message.PhilosopherId);
            _logger.LogDebug("Philosopher {Name} is hungry and added to queue", message.PhilosopherName);
        }

        SignalWork();
        return Task.CompletedTask;
    }

    private Task HandleFinishedEating(FinishedEatingMessage message) {
        lock (_lock) {
            if (!_philosophers.TryGetValue(message.PhilosopherId, out var state)) {
                _logger.LogWarning("Unknown philosopher {Id} sent finished eating message", message.PhilosopherId);
                return Task.CompletedTask;
            }

            state.IsEating = false;
            state.IsWaitingForForks = false;

            _logger.LogDebug("Philosopher {Name} finished eating",
                message.PhilosopherName);
        }

        SignalWork();
        return Task.CompletedTask;
    }

    private Task HandlePhilosopherExited(PhilosopherExitedMessage message) {
        lock (_lock) {
            if (_philosophers.TryGetValue(message.PhilosopherId, out var state)) {
                state.HasExited = true;
                _logger.LogInformation("Philosopher {Name} exited with {Meals} meals",
                    message.PhilosopherName, message.TotalMeals);
            }
        }

        SignalWork();
        return Task.CompletedTask;
    }

    private async Task ProcessHungryQueueAsync() {
        var forks = await _tableClient.GetAllForksAsync();
        var availableForkIds = forks
            .Where(f => f.State == ForkState.Available)
            .Select(f => f.Id)
            .ToHashSet();

        List<(string PhilosopherId, int LeftForkId, int RightForkId)> toFeed = new();

        lock (_lock) {
            var tempQueue = new Queue<string>();

            while (_hungryQueue.Count > 0) {
                var philosopherId = _hungryQueue.Dequeue();

                if (!_philosophers.TryGetValue(philosopherId, out var state)) {
                    continue;
                }

                if (state.HasExited || state.IsEating) {
                    state.IsWaitingForForks = false;
                    continue;
                }

                if (availableForkIds.Contains(state.LeftForkId) && availableForkIds.Contains(state.RightForkId)) {
                    availableForkIds.Remove(state.LeftForkId);
                    availableForkIds.Remove(state.RightForkId);
                    state.IsEating = true;
                    state.IsWaitingForForks = false;

                    toFeed.Add((philosopherId, state.LeftForkId, state.RightForkId));
                    _logger.LogDebug("Coordinator allows {Name} to eat", state.PhilosopherName);
                }
                else {
                    tempQueue.Enqueue(philosopherId);
                }
            }

            while (tempQueue.Count > 0) {
                _hungryQueue.Enqueue(tempQueue.Dequeue());
            }
        }

        foreach (var (philosopherId, leftForkId, rightForkId) in toFeed) {
            var command = new TakeForksCommand {
                PhilosopherId = philosopherId,
                LeftForkId = leftForkId,
                RightForkId = rightForkId,
                Timestamp = DateTime.UtcNow
            };

            await _bus.PubSub.PublishAsync(command);
        }
    }

    private class PhilosopherCoordinatorState {
        public string PhilosopherId { get; set; } = "";
        public string PhilosopherName { get; set; } = "";
        public int LeftForkId { get; set; }
        public int RightForkId { get; set; }
        public bool IsWaitingForForks { get; set; }
        public bool IsEating { get; set; }
        public bool HasExited { get; set; }
    }
}
