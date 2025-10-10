namespace DiningPhilosophers.Contracts;

public interface IForkInfo {
    int Id { get; }
    ForkState State { get; }
}