using UnityEngine;

namespace NobunAtelier.Gameplay
{
    [System.Serializable]
    public class AssetReferenceHitboxBehaviour : AssetReferenceGameObjectComponentT<HitboxBehaviour>
    {
        public AssetReferenceHitboxBehaviour(string guid) : base(guid)
        { }
    }


    public class AtelierFactoryHitboxBehaviourReference
        : AtelierFactoryGameObjectReferenceT<HitboxBehaviour, AssetReferenceHitboxBehaviour, AtelierFactoryHitboxBehaviourReference>
    { }
}