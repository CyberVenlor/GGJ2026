public abstract class PlayerState : State
{
    protected readonly PlayerController Controller;

    protected PlayerState(PlayerController controller)
    {
        Controller = controller;
    }
}
