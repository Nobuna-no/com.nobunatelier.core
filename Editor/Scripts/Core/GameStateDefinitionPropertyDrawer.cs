using UnityEditor;

namespace NobunAtelier.Editor
{
    [CustomPropertyDrawer(typeof(GameStateDefinition))]
    public class GameStateDefinitionPropertyDrawer : StateDefinitionPropertyDrawer<GameStateDefinition, GameStateCollection>
    {
    }
}