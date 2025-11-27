using DiningPhilosophers.Contracts;
using Moq;
using T1.SynchronousSimulation.Models;

namespace T4.UnitTesting;

public class PhilosopherTests {
    [Fact]
    public void Philosopher_ShouldTransition_FromThinkingToHungry() {
        var leftFork = new Fork(0);
        var rightFork = new Fork(1);
        var mockStrategy = new Mock<IStrategy>();
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                It.IsAny<PhilosopherState>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(PhilosopherAction.None);
        
        var random = new Random(42); // фиксированный сид для воспроизводимости
        var philosopher = new Philosopher(0, "Test", leftFork, rightFork, mockStrategy.Object, null, random);
        
        const int maxSteps = 11;
        for (int i = 0; i < maxSteps; i++) {
            philosopher.Step();
            if (philosopher.State == PhilosopherState.Hungry)
                break;
        }
        
        Assert.Equal(PhilosopherState.Hungry, philosopher.State);
    }
    
    [Fact]
    public void Philosopher_ShouldTransition_FromHungryToEating_WhenBothForksAcquired() {
        var leftFork = new Fork(0);
        var rightFork = new Fork(1);
        var mockStrategy = new Mock<IStrategy>();
        
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                PhilosopherState.Hungry, false, false))
            .Returns(PhilosopherAction.TakeLeftFork);
        
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                PhilosopherState.Hungry, true, false))
            .Returns(PhilosopherAction.TakeRightFork);
        
        var random = new Random(42);
        var philosopher = new Philosopher(0, "Test", leftFork, rightFork, mockStrategy.Object, null, random);
        
        const int maxSteps = 11;
        for (int i = 0; i < maxSteps; i++) {
            philosopher.Step();
            if (philosopher.State == PhilosopherState.Hungry)
                break;
        }
        
        philosopher.Step();
        philosopher.Step();
        philosopher.Step();
        Assert.True(philosopher.HasLeftFork);
        
        philosopher.Step();
        philosopher.Step();
        philosopher.Step();
        Assert.True(philosopher.HasRightFork);
        
        philosopher.Step();
        
        Assert.Equal(PhilosopherState.Eating, philosopher.State);
        Assert.True(philosopher.HasLeftFork);
        Assert.True(philosopher.HasRightFork);
        Assert.Equal(1, philosopher.EatenCount);
    }

    [Fact]
    public void Philosopher_ShouldTransition_FromEatingToThinking_AndReleaseForks() {
        var leftFork = new Fork(0);
        var rightFork = new Fork(1);
        var mockStrategy = new Mock<IStrategy>();
        
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                PhilosopherState.Hungry, false, false))
            .Returns(PhilosopherAction.TakeLeftFork);
        
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                PhilosopherState.Hungry, true, false))
            .Returns(PhilosopherAction.TakeRightFork);
        
        var random = new Random(42);
        var philosopher = new Philosopher(0, "Test", leftFork, rightFork, mockStrategy.Object, null, random);
        
        for (int i = 0; i < 11; i++) {
            philosopher.Step();
            if (philosopher.State == PhilosopherState.Hungry)
                break;
        }
        
        for (int i = 0; i < 10; i++) {
            philosopher.Step();
            if (philosopher.State == PhilosopherState.Eating)
                break;
        }
        
        Assert.Equal(PhilosopherState.Eating, philosopher.State);
        
        for (int i = 0; i < 6; i++) {
            philosopher.Step();
        }
        
        Assert.Equal(PhilosopherState.Thinking, philosopher.State);
        Assert.False(philosopher.HasLeftFork);
        Assert.False(philosopher.HasRightFork);
        Assert.Equal(ForkState.Available, leftFork.State);
        Assert.Equal(ForkState.Available, rightFork.State);
        Assert.Equal(1, philosopher.EatenCount);
    }
    
    [Fact]
    public void Philosopher_CannotTake_OccupiedFork() {
        var leftFork = new Fork(0);
        var rightFork = new Fork(1);
        
        rightFork.Take(322);
        
        var mockStrategy = new Mock<IStrategy>();
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                PhilosopherState.Hungry, false, false))
            .Returns(PhilosopherAction.TakeLeftFork);
        
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                PhilosopherState.Hungry, true, false))
            .Returns(PhilosopherAction.TakeRightFork);
        
        var random = new Random(42);
        var philosopher = new Philosopher(0, "Test", leftFork, rightFork, mockStrategy.Object, null, random);
        
        for (int i = 0; i < 11; i++) {
            philosopher.Step();
            if (philosopher.State == PhilosopherState.Hungry)
                break;
        }
        
        philosopher.Step();
        philosopher.Step();
        
        philosopher.Step();
        philosopher.Step();
        philosopher.Step();
        
        Assert.Equal(PhilosopherState.Hungry, philosopher.State);
        Assert.True(philosopher.HasLeftFork);
        Assert.False(philosopher.HasRightFork);
        Assert.Equal(ForkState.InUse, rightFork.State);
        Assert.Equal(322, rightFork.UsedByPhilosopher);
    }

    [Fact]
    public void Philosopher_ShouldNotify_CoordinatorWhenHungry() {
        var leftFork = new Fork(0);
        var rightFork = new Fork(1);
        var mockStrategy = new Mock<IStrategy>();
        var mockCoordinator = new Mock<ICoordinator>();
        
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                It.IsAny<PhilosopherState>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(PhilosopherAction.None);
        
        var random = new Random(42);
        var philosopher = new Philosopher(0, "Test", leftFork, rightFork, 
            mockStrategy.Object, mockCoordinator.Object, random);
        
        for (int i = 0; i < 11; i++) {
            philosopher.Step();
            if (philosopher.State == PhilosopherState.Hungry)
                break;
        }
        
        mockCoordinator.Verify(c => c.RequestToEat(0), Times.Once);
    }

    [Fact]
    public void Philosopher_ShouldNotify_CoordinatorWhenFinishedEating() {
        var leftFork = new Fork(0);
        var rightFork = new Fork(1);
        var mockStrategy = new Mock<IStrategy>();
        var mockCoordinator = new Mock<ICoordinator>();
        
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                PhilosopherState.Hungry, false, false))
            .Returns(PhilosopherAction.TakeLeftFork);
        
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                PhilosopherState.Hungry, true, false))
            .Returns(PhilosopherAction.TakeRightFork);
        
        var random = new Random(42);
        var philosopher = new Philosopher(0, "Test", leftFork, rightFork, 
            mockStrategy.Object, mockCoordinator.Object, random);
        
        for (int i = 0; i < 11; i++) {
            philosopher.Step();
            if (philosopher.State == PhilosopherState.Hungry)
                break;
        }
        philosopher.Step();
        philosopher.Step();
        philosopher.Step();
        philosopher.Step();
        philosopher.Step();
        
        for (int i = 0; i < 6; i++) {
            philosopher.Step();
        }
        
        Assert.Equal(PhilosopherState.Thinking, philosopher.State);
        mockCoordinator.Verify(c => c.NotifyFinishedEating(0), Times.Once);
    }

    [Fact]
    public void Philosopher_ShouldTrack_TotalHungrySteps() {
        var leftFork = new Fork(0);
        var rightFork = new Fork(1);
        var mockStrategy = new Mock<IStrategy>();
        
        const int hungryStepsBeforeTaking = 5;
        int stepCounter = 0;
        
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                PhilosopherState.Hungry, It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(() => {
                stepCounter++;
                if (stepCounter <= hungryStepsBeforeTaking)
                    return PhilosopherAction.None;
                
                return PhilosopherAction.TakeLeftFork;
            });
        
        mockStrategy
            .Setup(s => s.DecideAction(It.IsAny<IForkInfo>(), It.IsAny<IForkInfo>(),
                PhilosopherState.Hungry, true, false))
            .Returns(PhilosopherAction.TakeRightFork);
        
        var random = new Random(42);
        var philosopher = new Philosopher(0, "Test", leftFork, rightFork, mockStrategy.Object, null, random);
        
        for (int i = 0; i < 11; i++) {
            philosopher.Step();
            if (philosopher.State == PhilosopherState.Hungry)
                break;
        }
        
        for (int i = 0; i < 20; i++) {
            philosopher.Step();
            if (philosopher.State == PhilosopherState.Eating)
                break;
        }
        
        Assert.Equal(PhilosopherState.Eating, philosopher.State);
        Assert.True(philosopher.TotalHungrySteps >= hungryStepsBeforeTaking);
    }
}

