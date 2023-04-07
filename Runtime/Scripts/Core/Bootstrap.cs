using NaughtyAttributes;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    [SerializeField]
    private int m_targetFramerate = 30;

    [SerializeField]
    private bool m_forceResolution = false;
    [SerializeField, ShowIf("m_forceResolution")]
    private Vector2 screenResolution = new Vector2(640, 480);

    private void Start()
    {
        Application.targetFrameRate = m_targetFramerate;

        if (m_forceResolution)
        {
            Screen.SetResolution((int)screenResolution.x, (int)screenResolution.y, true);
        }
    }
}