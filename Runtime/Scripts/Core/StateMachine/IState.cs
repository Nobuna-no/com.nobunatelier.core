namespace NobunAtelier
{
    public interface IState<T>
        where T : StateDefinition
    {
        T GetStateDefinition();

        void Enter();

        void Tick(float deltaTime);

        void Exit();

        void SetState(T newState);
    }
}