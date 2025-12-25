namespace T7.CoordinatorService.Services;

public sealed class SubscriptionsState {
    private int _subscribedCount;

    public const int ExpectedSubscriptions = 4;

    public void MarkSubscribed() {
        Interlocked.Increment(ref _subscribedCount);
    }

    public bool IsSubscribedToAll => Volatile.Read(ref _subscribedCount) == ExpectedSubscriptions;

    public int SubscribedCount => Volatile.Read(ref _subscribedCount);
}
