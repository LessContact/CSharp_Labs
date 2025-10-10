namespace DiningPhilosophers.Contracts;

public class PhilosopherCommandEventArgs : EventArgs {
    public int PhilosopherId { get; init; }
    public PhilosopherAction Action { get; init; }
}