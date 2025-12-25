using DiningPhilosophers.Contracts;

namespace T7.PhilosopherService.Services;

public interface ITableServiceClient {
    Task<bool> RegisterAsync(string philosopherId, string philosopherName, int leftForkId, int rightForkId);
    Task<TakeForkResponse> TakeForkAsync(string philosopherId, string philosopherName, int forkId);
    Task<bool> ReleaseForkAsync(string philosopherId, int forkId);
    Task<ForkInfo?> GetForkStateAsync(int forkId);
    Task NotifyStartEatingAsync(string philosopherId, string philosopherName, int leftForkId, int rightForkId);
    Task RecordMealAsync(string philosopherId, string philosopherName, long waitingTimeMs);
    Task UpdateStateAsync(string philosopherId, PhilosopherState state, bool hasLeftFork, bool hasRightFork, int eatenCount);
    Task NotifyExitAsync(string philosopherId, string philosopherName, int totalMeals);
}

public class TableServiceClient : ITableServiceClient {
    private readonly HttpClient _httpClient;
    private readonly ILogger<TableServiceClient> _logger;

    public TableServiceClient(HttpClient httpClient, ILogger<TableServiceClient> logger) {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> RegisterAsync(string philosopherId, string philosopherName, int leftForkId, int rightForkId) {
        try {
            var request = new RegisterPhilosopherRequest {
                PhilosopherId = philosopherId,
                PhilosopherName = philosopherName,
                LeftForkId = leftForkId,
                RightForkId = rightForkId
            };

            var response = await _httpClient.PostAsJsonAsync("/api/table/register", request);
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<RegisterPhilosopherResponse>();
            return result?.Success ?? false;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error registering philosopher {Id}", philosopherId);
            return false;
        }
    }

    public async Task<TakeForkResponse> TakeForkAsync(string philosopherId, string philosopherName, int forkId) {
        try {
            var request = new TakeForkRequest {
                PhilosopherId = philosopherId,
                PhilosopherName = philosopherName,
                ForkId = forkId
            };

            var response = await _httpClient.PostAsJsonAsync("/api/table/fork/take", request);
            if (!response.IsSuccessStatusCode) {
                return new TakeForkResponse { Success = false, ForkId = forkId };
            }

            return await response.Content.ReadFromJsonAsync<TakeForkResponse>() 
                   ?? new TakeForkResponse { Success = false, ForkId = forkId };
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error taking fork {ForkId} for philosopher {Id}", forkId, philosopherId);
            return new TakeForkResponse { Success = false, ForkId = forkId };
        }
    }

    public async Task<bool> ReleaseForkAsync(string philosopherId, int forkId) {
        try {
            var request = new ReleaseForkRequest {
                PhilosopherId = philosopherId,
                ForkId = forkId
            };

            var response = await _httpClient.PostAsJsonAsync("/api/table/fork/release", request);
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<ReleaseForkResponse>();
            return result?.Success ?? false;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error releasing fork {ForkId} for philosopher {Id}", forkId, philosopherId);
            return false;
        }
    }

    public async Task<ForkInfo?> GetForkStateAsync(int forkId) {
        try {
            var response = await _httpClient.GetAsync($"/api/table/fork/{forkId}");
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<ForkInfo>();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error getting fork {ForkId} state", forkId);
            return null;
        }
    }

    public async Task NotifyStartEatingAsync(string philosopherId, string philosopherName, int leftForkId, int rightForkId) {
        try {
            var request = new StartEatingRequest {
                PhilosopherId = philosopherId,
                PhilosopherName = philosopherName,
                LeftForkId = leftForkId,
                RightForkId = rightForkId
            };

            await _httpClient.PostAsJsonAsync("/api/table/eating/start", request);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error notifying start eating for philosopher {Id}", philosopherId);
        }
    }

    public async Task RecordMealAsync(string philosopherId, string philosopherName, long waitingTimeMs) {
        try {
            var request = new RecordMealRequest {
                PhilosopherId = philosopherId,
                PhilosopherName = philosopherName,
                WaitingTimeMs = waitingTimeMs
            };

            await _httpClient.PostAsJsonAsync("/api/table/meal/record", request);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error recording meal for philosopher {Id}", philosopherId);
        }
    }

    public async Task UpdateStateAsync(string philosopherId, PhilosopherState state, bool hasLeftFork, bool hasRightFork, int eatenCount) {
        try {
            var request = new UpdatePhilosopherStateRequest {
                PhilosopherId = philosopherId,
                State = state,
                HasLeftFork = hasLeftFork,
                HasRightFork = hasRightFork,
                EatenCount = eatenCount
            };

            await _httpClient.PostAsJsonAsync("/api/table/philosopher/state", request);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error updating state for philosopher {Id}", philosopherId);
        }
    }

    public async Task NotifyExitAsync(string philosopherId, string philosopherName, int totalMeals) {
        try {
            var request = new PhilosopherExitRequest {
                PhilosopherId = philosopherId,
                PhilosopherName = philosopherName,
                TotalMeals = totalMeals
            };

            await _httpClient.PostAsJsonAsync("/api/table/philosopher/exit", request);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error notifying exit for philosopher {Id}", philosopherId);
        }
    }
}

