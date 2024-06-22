using UnityEngine;

namespace NobunAtelier
{
    [CreateAssetMenu(fileName = "[Factory Products]", menuName = "NobunAtelier/Factory/Products")]
    public class FactoryProductCollection : DataCollection<FactoryProductDefinition>
    { }
}