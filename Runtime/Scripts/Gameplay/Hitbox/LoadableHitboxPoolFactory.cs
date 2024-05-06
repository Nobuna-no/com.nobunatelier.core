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
    {
        public override void Release(Hitbox obj)
        {
            obj.OnHit.RemoveAllListeners();
            obj.OnHitboxDisabled.RemoveAllListeners();
            obj.OnHitboxEnabled.RemoveAllListeners();
            obj.HitEnd();
            base.Release(obj);
        }
    }
}