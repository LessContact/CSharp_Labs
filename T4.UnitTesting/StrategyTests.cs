using DiningPhilosophers.Contracts;
using DiningPhilosophers.Strategies;
using Moq;

namespace T4.UnitTesting;

public class StrategyTests {
    [Fact]
    public void NaiveStrategy_HungryPhilosopher_ShouldTakeLeftForkFirst() {
        var strategy = new NaiveStrategy();
        var mockLeftFork = new Mock<IForkInfo>();
        var mockRightFork = new Mock<IForkInfo>();
        
        mockLeftFork.Setup(f => f.State).Returns(ForkState.Available);
        mockRightFork.Setup(f => f.State).Returns(ForkState.Available);
        
        var action = strategy.DecideAction(
            mockLeftFork.Object,
            mockRightFork.Object,
            PhilosopherState.Hungry,
            hasLeftFork: false,
            hasRightFork: false
        );
        
        Assert.Equal(PhilosopherAction.TakeLeftFork, action);
    }

    [Fact]
    public void NaiveStrategy_WithLeftFork_ShouldTakeRightFork() {
        var strategy = new NaiveStrategy();
        var mockLeftFork = new Mock<IForkInfo>();
        var mockRightFork = new Mock<IForkInfo>();
        
        mockLeftFork.Setup(f => f.State).Returns(ForkState.InUse);
        mockRightFork.Setup(f => f.State).Returns(ForkState.Available);
        
        var action = strategy.DecideAction(
            mockLeftFork.Object,
            mockRightFork.Object,
            PhilosopherState.Hungry,
            hasLeftFork: true,
            hasRightFork: false
        );
        
        Assert.Equal(PhilosopherAction.TakeRightFork, action);
    }
    
    [Fact]
    public void NaiveStrategy_WhenLeftForkOccupied_ShouldWait() {
        var strategy = new NaiveStrategy();
        var mockLeftFork = new Mock<IForkInfo>();
        var mockRightFork = new Mock<IForkInfo>();
        
        mockLeftFork.Setup(f => f.State).Returns(ForkState.InUse);
        mockRightFork.Setup(f => f.State).Returns(ForkState.Available);
        
        var action = strategy.DecideAction(
            mockLeftFork.Object,
            mockRightFork.Object,
            PhilosopherState.Hungry,
            hasLeftFork: false,
            hasRightFork: false
        );
        
        Assert.Equal(PhilosopherAction.None, action);
    }
    
    [Fact]
    public void NaiveStrategy_WithBothForks_ShouldDoNothing() {
        var strategy = new NaiveStrategy();
        var mockLeftFork = new Mock<IForkInfo>();
        var mockRightFork = new Mock<IForkInfo>();
        
        mockLeftFork.Setup(f => f.State).Returns(ForkState.InUse);
        mockRightFork.Setup(f => f.State).Returns(ForkState.InUse);
        
        var action = strategy.DecideAction(
            mockLeftFork.Object,
            mockRightFork.Object,
            PhilosopherState.Hungry,
            hasLeftFork: true,
            hasRightFork: true
        );
            
        Assert.Equal(PhilosopherAction.None, action);
    }

    [Fact]
    public void NaiveStrategy_ThinkingPhilosopher_ShouldDoNothing() {
        var strategy = new NaiveStrategy();
        var mockLeftFork = new Mock<IForkInfo>();
        var mockRightFork = new Mock<IForkInfo>();
        
        mockLeftFork.Setup(f => f.State).Returns(ForkState.Available);
        mockRightFork.Setup(f => f.State).Returns(ForkState.Available);
        
        var action = strategy.DecideAction(
            mockLeftFork.Object,
            mockRightFork.Object,
            PhilosopherState.Thinking,
            hasLeftFork: false,
            hasRightFork: false
        );
        
        Assert.Equal(PhilosopherAction.None, action);
    }

    [Fact]
    public void NaiveStrategy_EatingPhilosopher_ShouldDoNothing() {
        var strategy = new NaiveStrategy();
        var mockLeftFork = new Mock<IForkInfo>();
        var mockRightFork = new Mock<IForkInfo>();
        
        var action = strategy.DecideAction(
            mockLeftFork.Object,
            mockRightFork.Object,
            PhilosopherState.Eating,
            hasLeftFork: true,
            hasRightFork: true
        );
        
        Assert.Equal(PhilosopherAction.None, action);
    }

    [Fact]
    public void CoordinatorStrategy_ShouldExecute_PendingAction() {
        var strategy = new CoordinatorStrategy();
        var mockLeftFork = new Mock<IForkInfo>();
        var mockRightFork = new Mock<IForkInfo>();
        
        mockLeftFork.Setup(f => f.State).Returns(ForkState.Available);
        mockRightFork.Setup(f => f.State).Returns(ForkState.Available);
        
        strategy.SetPendingAction(PhilosopherAction.TakeLeftFork);
        var action = strategy.DecideAction(
            mockLeftFork.Object,
            mockRightFork.Object,
            PhilosopherState.Hungry,
            hasLeftFork: false,
            hasRightFork: false
        );
        
        Assert.Equal(PhilosopherAction.TakeLeftFork, action);
    }
    
    [Fact]
    public void CoordinatorStrategy_ShouldClear_PendingActionAfterExecution() {
        var strategy = new CoordinatorStrategy();
        var mockLeftFork = new Mock<IForkInfo>();
        var mockRightFork = new Mock<IForkInfo>();
        
        mockLeftFork.Setup(f => f.State).Returns(ForkState.InUse);
        mockRightFork.Setup(f => f.State).Returns(ForkState.Available);
        
        strategy.SetPendingAction(PhilosopherAction.TakeLeftFork);
        var action1 = strategy.DecideAction(
            mockLeftFork.Object,
            mockRightFork.Object,
            PhilosopherState.Hungry,
            hasLeftFork: false,
            hasRightFork: false
        );
        
        var action2 = strategy.DecideAction(
            mockLeftFork.Object,
            mockRightFork.Object,
            PhilosopherState.Hungry,
            hasLeftFork: true,
            hasRightFork: false
        );
        
        Assert.Equal(PhilosopherAction.TakeLeftFork, action1);
        Assert.Equal(PhilosopherAction.TakeRightFork, action2);
    }
    
    [Fact]
    public void CoordinatorStrategy_WithLeftFork_ShouldTakeRightFork() {
        var strategy = new CoordinatorStrategy();
        var mockLeftFork = new Mock<IForkInfo>();
        var mockRightFork = new Mock<IForkInfo>();
        
        mockLeftFork.Setup(f => f.State).Returns(ForkState.InUse);
        mockRightFork.Setup(f => f.State).Returns(ForkState.Available);

        var action = strategy.DecideAction(
            mockLeftFork.Object,
            mockRightFork.Object,
            PhilosopherState.Hungry,
            hasLeftFork: true,
            hasRightFork: false
        );
        
        Assert.Equal(PhilosopherAction.TakeRightFork, action);
    }
    
    [Fact]
    public void CoordinatorStrategy_WithoutPendingAction_ShouldWait() {
        var strategy = new CoordinatorStrategy();
        var mockLeftFork = new Mock<IForkInfo>();
        var mockRightFork = new Mock<IForkInfo>();
        
        mockLeftFork.Setup(f => f.State).Returns(ForkState.Available);
        mockRightFork.Setup(f => f.State).Returns(ForkState.Available);
        
        var action = strategy.DecideAction(
            mockLeftFork.Object,
            mockRightFork.Object,
            PhilosopherState.Hungry,
            hasLeftFork: false,
            hasRightFork: false
        );
        
        Assert.Equal(PhilosopherAction.None, action);
    }
}

