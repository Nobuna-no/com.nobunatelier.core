using UnityEditor;

namespace NobunAtelier.Editor
{
    [CustomPropertyDrawer(typeof(GameModeStateDefinition))]
    public class GameModeStateDefinitionPropertyDrawer : StateDefinitionPropertyDrawer<GameModeStateDefinition, GameModeStateCollection>
    { }
}