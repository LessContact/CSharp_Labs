using DiningPhilosophers.Contracts;

namespace DiningPhilosophers.Strategies;

public class NaiveStrategy : IStrategy {
    public PhilosopherAction DecideAction(IForkInfo leftFork, IForkInfo rightFork,
        PhilosopherState currentState, bool hasLeftFork, bool hasRightFork) {

        if (currentState is not PhilosopherState.Hungry || (hasLeftFork && hasRightFork))
            return PhilosopherAction.None;

        if (!hasLeftFork && leftFork.State is ForkState.Available)
            return PhilosopherAction.TakeLeftFork;
        
        if (hasLeftFork && !hasRightFork && rightFork.State is ForkState.Available)
            return PhilosopherAction.TakeRightFork;
        
        return PhilosopherAction.None;
    }
}