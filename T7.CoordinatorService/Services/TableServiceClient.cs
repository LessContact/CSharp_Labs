using DiningPhilosophers.Contracts;

namespace T7.CoordinatorService.Services;

public interface ITableServiceClient {
    Task<List<ForkInfo>> GetAllForksAsync();
    Task<ForkInfo?> GetForkAsync(int forkId);
}

public class TableServiceClient : ITableServiceClient {
    private readonly HttpClient _httpClient;
    private readonly ILogger<TableServiceClient> _logger;

    public TableServiceClient(HttpClient httpClient, ILogger<TableServiceClient> logger) {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<ForkInfo>> GetAllForksAsync() {
        try {
            var response = await _httpClient.GetAsync("/api/table/forks");
            if (!response.IsSuccessStatusCode) {
                _logger.LogWarning("Failed to get forks: {StatusCode}", response.StatusCode);
                return new List<ForkInfo>();
            }

            return await response.Content.ReadFromJsonAsync<List<ForkInfo>>() ?? new List<ForkInfo>();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error getting all forks");
            return new List<ForkInfo>();
        }
    }

    public async Task<ForkInfo?> GetForkAsync(int forkId) {
        try {
            var response = await _httpClient.GetAsync($"/api/table/fork/{forkId}");
            if (!response.IsSuccessStatusCode) {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ForkInfo>();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error getting fork {ForkId}", forkId);
            return null;
        }
    }
}

