namespace NobunAtelier
{
    /// <summary>
    /// Identity token for scene-level objects registered via <see cref="SceneObjectProvider"/>.
    /// Create assets for common scene anchors: "MainCamera", "Player", "PostProcess", etc.
    /// Looked up at runtime via <see cref="SceneObjectRegistry"/>.
    /// </summary>
    public class SceneObjectID : DataDefinition
    {
    }
}
