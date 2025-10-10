namespace DiningPhilosophers.Contracts;

public enum PhilosopherAction {
    None,
    TakeLeftFork,
    TakeRightFork,
    ReleaseLeftFork,
    ReleaseRightFork,
    ReleaseBothForks
}