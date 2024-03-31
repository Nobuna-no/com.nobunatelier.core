namespace NobunAtelier.Gameplay
{
    [System.Serializable]
    public class LoadableHitbox : LoadableComponent<Hitbox>
    {
        public LoadableHitbox(string guid) : base(guid)
        { }
    }

    public class LoadableHitboxPoolFactory
        : LoadableComponentPoolFactory<Hitbox, LoadableHitbox, LoadableHitboxPoolFactory>
    { }
}