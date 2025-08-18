using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

public class Bootstrap : MonoBehaviour
{
    [SerializeField]
    [FormerlySerializedAs("m_targetFramerate")]
    private int m_TargetFramerate = 30;

    [SerializeField]
    [FormerlySerializedAs("m_forceResolution")]
    private bool m_ForceResolution = false;

    [SerializeField, ShowIf("m_forceResolution")]
    private Vector2 screenResolution = new Vector2(640, 480);

    private void Start()
    {
        Application.targetFrameRate = m_TargetFramerate;

        if (m_ForceResolution)
        {
            Screen.SetResolution((int)screenResolution.x, (int)screenResolution.y, true);
        }
    }
}