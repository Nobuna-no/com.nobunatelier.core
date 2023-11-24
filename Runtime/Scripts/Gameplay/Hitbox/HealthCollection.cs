using UnityEngine;

namespace NobunAtelier.Gameplay
{
    [CreateAssetMenu(fileName = "[Stats: Health]", menuName = "NobunAtelier/Gameplay/Data Collection/Health")]
    public class HealthCollection : DataCollection<HealthDefinition>
    {
    }
}