public abstract class State
{
    public virtual void Awake() { }
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}

public sealed class StateMachine<TState> where TState : State
{
    public TState CurrentState { get; private set; }

    public void ChangeState(TState nextState)
    {
        if (nextState == null || ReferenceEquals(CurrentState, nextState))
        {
            return;
        }

        if (CurrentState != null)
        {
            CurrentState.Exit();
        }

        CurrentState = nextState;
        CurrentState.Enter();
    }
}
