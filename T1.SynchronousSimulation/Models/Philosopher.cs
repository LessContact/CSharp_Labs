using System.ComponentModel;
using DiningPhilosophers.Contracts;

namespace App.Models;

public class Philosopher {
    public int Id { get; }
    public string Name { get; }
    public PhilosopherState State { get; private set; }
    public int EatenCount { get; private set; }

    private int _stepsInCurrentState;
    private int _stepsNeededForCurrentState;
    private readonly Random _random;

    private Fork _leftFork;
    private Fork _rightFork;

    private PhilosopherAction _currentAction;
    private int _actionStepsRemaining;

    private readonly IStrategy _strategy;
    private readonly ICoordinator? _coordinator;

    public Philosopher(int id, string name, Fork leftFork, Fork rightFork,
        IStrategy strategy, ICoordinator? coordinator, Random random) {
        Id = id;
        Name = name;
        State = PhilosopherState.Thinking;
        _leftFork = leftFork;
        _rightFork = rightFork;
        _strategy = strategy;
        _coordinator = coordinator;
        _random = random;

        _stepsInCurrentState = 0;
        _stepsNeededForCurrentState = _random.Next(3, 11);
        _currentAction = PhilosopherAction.None;
        _actionStepsRemaining = 0;
    }

    public void Step() {
        _stepsInCurrentState++;
        
        if (_actionStepsRemaining > 0) {
            _actionStepsRemaining--;
            
            if (_actionStepsRemaining == 0) {
                FinishAction();
            }

            return;
        }
        
        switch (State) {
            case PhilosopherState.Thinking:
                if (_stepsInCurrentState >= _stepsNeededForCurrentState) {
                    TransitionToHungry();
                }
                break;

            case PhilosopherState.Hungry:
                HandleHungryState();
                break;

            case PhilosopherState.Eating:
                if (_stepsInCurrentState >= _stepsNeededForCurrentState) {
                    TransitionToThinking();
                }
                break;
        }
    }

    private void TransitionToHungry() {
        State = PhilosopherState.Hungry;
        _stepsInCurrentState = 0;
        
        _coordinator?.RequestToEat(Id);
    }

    private void HandleHungryState() {
        var action = _strategy.DecideAction(_leftFork, _rightFork, State, HasLeftFork, HasRightFork);
        
        ExecuteAction(action);
        
        if (HasLeftFork && HasRightFork && _currentAction == PhilosopherAction.None) {
            TransitionToEating();
        }
    }

    private void ExecuteAction(PhilosopherAction action) {
        if (_actionStepsRemaining > 0)
            return;

        _currentAction = action;

        switch (action) {
            case PhilosopherAction.TakeLeftFork:
                if (!HasLeftFork && _leftFork.State == ForkState.Available) {
                    _actionStepsRemaining = 2;
                }
                else {
                    _currentAction = PhilosopherAction.None;
                }

                break;

            case PhilosopherAction.TakeRightFork:
                if (!HasRightFork && _rightFork.State == ForkState.Available) {
                    _actionStepsRemaining = 2;
                }
                else {
                    _currentAction = PhilosopherAction.None;
                }

                break;

            case PhilosopherAction.ReleaseLeftFork:
                if (HasLeftFork) {
                    _leftFork.Release();
                    HasLeftFork = false;
                }

                _currentAction = PhilosopherAction.None;
                break;

            case PhilosopherAction.ReleaseRightFork:
                if (HasRightFork) {
                    _rightFork.Release();
                    HasRightFork = false;
                }

                _currentAction = PhilosopherAction.None;
                break;

            case PhilosopherAction.ReleaseBothForks:
                if (HasLeftFork) {
                    _leftFork.Release();
                    HasLeftFork = false;
                }

                if (HasRightFork) {
                    _rightFork.Release();
                    HasRightFork = false;
                }

                _currentAction = PhilosopherAction.None;
                break;

            case PhilosopherAction.None:
                break;
        }
    }

    private void FinishAction() {
        switch (_currentAction) {
            case PhilosopherAction.TakeLeftFork:
                HasLeftFork = _leftFork.Take(Id);
                break;

            case PhilosopherAction.TakeRightFork:
                HasRightFork = _rightFork.Take(Id);
                break;
        }

        _currentAction = PhilosopherAction.None;
    }

    private void TransitionToEating() {
        State = PhilosopherState.Eating;
        
        TotalHungrySteps += _stepsInCurrentState;

        _stepsInCurrentState = 0;
        _stepsNeededForCurrentState = _random.Next(4, 6);
        EatenCount++;
    }

    private void TransitionToThinking() {
        if (HasLeftFork) {
            _leftFork.Release();
            HasLeftFork = false;
        }

        if (HasRightFork) {
            _rightFork.Release();
            HasRightFork = false;
        }

        State = PhilosopherState.Thinking;
        _stepsInCurrentState = 0;
        _stepsNeededForCurrentState = _random.Next(3, 11);
        
        _coordinator?.NotifyFinishedEating(Id);
    }

    public int GetStepsLeftInState() {
        if (_actionStepsRemaining > 0)
            return _actionStepsRemaining;

        return _stepsNeededForCurrentState - _stepsInCurrentState;
    }

    public string GetCurrentActionString() {
        return _currentAction != PhilosopherAction.None ? _currentAction.ToString() : string.Empty;
    }

    public bool HasLeftFork { get; private set; }

    public bool HasRightFork { get; private set; }

    public int TotalHungrySteps { get; private set; }
}