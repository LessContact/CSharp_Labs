namespace DiningPhilosophers.Contracts;

public interface IStrategy {
    PhilosopherAction DecideAction(IForkInfo leftFork, IForkInfo rightFork,
        PhilosopherState currentState, bool hasLeftFork, bool hasRightFork);
}