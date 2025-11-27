using DiningPhilosophers.Contracts;

namespace DiningPhilosophers.Strategies;

public class HierarchyStrategy : IStrategy {
    public PhilosopherAction DecideAction(IForkInfo leftFork, IForkInfo rightFork,
        PhilosopherState currentState, bool hasLeftFork, bool hasRightFork) {
        
        if (currentState is not PhilosopherState.Hungry || (hasLeftFork && hasRightFork))
            return PhilosopherAction.None;
        
        bool leftForkIsLower = leftFork.Id < rightFork.Id;
        
        if (!hasLeftFork && !hasRightFork) {
            if (leftForkIsLower && leftFork.State is ForkState.Available)
                return PhilosopherAction.TakeLeftFork;
            
            if (!leftForkIsLower && rightFork.State is ForkState.Available)
                return PhilosopherAction.TakeRightFork;
            
            return PhilosopherAction.None;
        }
        
        if (leftForkIsLower && hasLeftFork && !hasRightFork) {
            if (rightFork.State is ForkState.Available)
                return PhilosopherAction.TakeRightFork;
            return PhilosopherAction.None;
        }

        if (!leftForkIsLower && hasRightFork && !hasLeftFork) {
            if (leftFork.State is ForkState.Available)
                return PhilosopherAction.TakeLeftFork;
            return PhilosopherAction.None;
        }
        
        if (leftForkIsLower && !hasLeftFork && hasRightFork)
            return PhilosopherAction.ReleaseRightFork;
        
        if (!leftForkIsLower && hasLeftFork && !hasRightFork)
            return PhilosopherAction.ReleaseLeftFork;

        return PhilosopherAction.None;
    }
}

