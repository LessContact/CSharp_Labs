namespace DiningPhilosophers.Contracts;

public interface ICoordinator {
    
    event EventHandler<PhilosopherCommandEventArgs>? CommandToPhilosopher;

    void RequestToEat(int philosopherId);

    void NotifyFinishedEating(int philosopherId);

    void Step();
}