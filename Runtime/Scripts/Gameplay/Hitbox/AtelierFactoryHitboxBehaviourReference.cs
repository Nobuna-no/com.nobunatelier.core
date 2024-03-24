namespace NobunAtelier.Gameplay
{
    [System.Serializable]
    public class AssetReferenceHitboxBehaviour : LoadableGameObjectComponent<HitboxBehaviour>
    {
        public AssetReferenceHitboxBehaviour(string guid) : base(guid)
        { }
    }

    public class AtelierFactoryHitboxBehaviourReference
        : LoadableComponentPoolFactory<HitboxBehaviour, AssetReferenceHitboxBehaviour, AtelierFactoryHitboxBehaviourReference>
    { }
}