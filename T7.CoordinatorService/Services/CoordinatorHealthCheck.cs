using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace T7.CoordinatorService.Services;

public class CoordinatorHealthCheck : IHealthCheck {
    private readonly SubscriptionsState _state;

    public CoordinatorHealthCheck(SubscriptionsState state) {
        _state = state;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        if (_state.IsSubscribedToAll) {
            return Task.FromResult(HealthCheckResult.Healthy(
                $"Subscribed to all coordinator messages ({_state.SubscribedCount}/{SubscriptionsState.ExpectedSubscriptions})."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy(
            $"Not subscribed to all coordinator messages yet ({_state.SubscribedCount}/{SubscriptionsState.ExpectedSubscriptions})."));
    }
}