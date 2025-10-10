using DiningPhilosophers.Contracts;

namespace DiningPhilosophers.Strategies;

public class CoordinatorStrategy : IStrategy {
    private PhilosopherAction? _pendingAction;

    public void SetPendingAction(PhilosopherAction action) {
        _pendingAction = action;
    }

    public PhilosopherAction DecideAction(IForkInfo leftFork, IForkInfo rightFork,
        PhilosopherState currentState, bool hasLeftFork, bool hasRightFork) {
        
        if (currentState is not PhilosopherState.Hungry)
            return PhilosopherAction.None;
        
        if (_pendingAction.HasValue) {
            var action = _pendingAction.Value;
            _pendingAction = null;
            return action;
        }

        if (hasLeftFork) {
            var action = PhilosopherAction.TakeRightFork;
            return action;
        }

        return PhilosopherAction.None;
    }
}