using DiningPhilosophers.Contracts;

namespace App.Models;

public class Fork(int id) : IForkInfo {
    public int Id { get; } = id;
    public ForkState State { get; private set; } = ForkState.Available;
    public int? UsedByPhilosopher { get; private set; } = null;

    public bool Take(int philosopherId) {
        if (State != ForkState.Available) return false;
        
        State = ForkState.InUse;
        UsedByPhilosopher = philosopherId;
        return true;
    }

    public void Release() {
        State = ForkState.Available;
        UsedByPhilosopher = null;
    }
}