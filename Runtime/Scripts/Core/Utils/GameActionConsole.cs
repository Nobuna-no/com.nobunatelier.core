using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class GameActionConsole : MonoBehaviourService<GameActionConsole>
    {
        [System.Serializable]
        public struct ActionDefinition
        {
            public string MenuName;
            public string ButtonName;
            public UnityEvent Action;

            public static ActionDefinition Create(string menu, string actionName, UnityAction method)
            {
                var action = new ActionDefinition();
                action.ButtonName = actionName;
                action.MenuName = menu;
                action.Action = new UnityEvent();
                action.Action.AddListener(method);
                return action;
            }
        }

        [SerializeField]
        private string m_consoleName = "Game Console";

        [SerializeField]
        private KeyCode m_toggleKey = KeyCode.F2;

        [SerializeField]
        private ActionDefinition[] m_data;

        [SerializeField]
        private Vector2 m_size = new Vector2(150, 0);

        [SerializeField]
        private Rect m_currentRect;

        // Improve the system by adding a way to parse Menu name + depth system.
        // i.e. "Debug/AI/Boids", "Debug/AI/StateMachine"
        // Isn't a bit overkill? I mean, for a small game I don't think that much menu would be relevant.
        private readonly Dictionary<string, List<ActionDefinition>> m_hierarchy = new Dictionary<string, List<ActionDefinition>>();

        private Vector2 m_scrollPosition = Vector2.zero;
        private string m_selectedMenu = null;
        private float m_minWidht = 150;
        private bool m_needScrollbar = false;
        private bool m_toggleDisplay = false;

        public void AddDebugAction(ActionDefinition definition)
        {
            if (!m_hierarchy.ContainsKey(definition.MenuName))
            {
                m_hierarchy.Add(definition.MenuName, new List<ActionDefinition> { definition });
            }
            else
            {
                if (!m_hierarchy[definition.MenuName].Contains(definition))
                {
                    m_hierarchy[definition.MenuName].Add(definition);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var menu in m_hierarchy.Values)
            {
                foreach (var action in menu)
                {
                    action.Action.RemoveAllListeners();
                }
                menu.Clear();
            }

            m_hierarchy.Clear();
        }

        private void Start()
        {
            for (int i = 0; i < m_data.Length; ++i)
            {
                if (m_data[i].MenuName.Length == 0)
                {
                    m_hierarchy.Add(m_data[i].ButtonName, new List<ActionDefinition>());
                    m_hierarchy[m_data[i].ButtonName].Add(m_data[i]);
                    continue;
                }

                if (!m_hierarchy.ContainsKey(m_data[i].MenuName))
                {
                    m_hierarchy.Add(m_data[i].MenuName, new List<ActionDefinition>());
                }
                m_hierarchy[m_data[i].MenuName].Add(m_data[i]);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(m_toggleKey))
            {
                m_toggleDisplay = !m_toggleDisplay;
            }
        }

        private void OnGUI()
        {
            if (!m_toggleDisplay)
            {
                return;
            }

            m_needScrollbar = m_size.y >= Screen.height;

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(m_minWidht));
            {
                GUILayout.Label($"<b>{m_consoleName}</b>");

                if (m_needScrollbar)
                {
                    m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, GUILayout.MinWidth(m_size.x + 50f));
                }

                OnWindow(0);

                if (m_needScrollbar)
                {
                    GUILayout.EndScrollView();
                }
            }
            GUILayout.EndVertical();

            if (GUILayoutUtility.GetLastRect().size.y > 1f)
            {
                if (!m_needScrollbar)
                {
                    m_size.x = GUILayoutUtility.GetLastRect().size.x;
                }
                m_size.y = GUILayoutUtility.GetLastRect().size.y;
            }
        }

        private void OnWindow(int id)
        {
            // GUILayout.BeginHorizontal(GUI.skin.box);

            foreach (var d in m_hierarchy)
            {
                if (d.Key == d.Value[0].ButtonName)
                {
                    if (GUILayout.Button(d.Key))
                    {
                        d.Value[0].Action?.Invoke();
                    }
                }
                else
                {
                    GUILayout.BeginVertical();
                    bool toggleValue = GUILayout.Toggle(m_selectedMenu == d.Key, $"<b>{d.Key}</b>");
                    if (toggleValue)
                    {
                        // if toggle and that selected is already d.Key
                        m_selectedMenu = d.Key;

                        // GUILayout.BeginHorizontal(/*GUI.skin.box*/);
                        {
                            // GUILayout.VerticalSlider(-1, 0, 0, GUILayout.MinHeight(0));
                            GUILayout.BeginVertical();
                            {
                                foreach (var a in d.Value)
                                {
                                    GUILayout.BeginHorizontal();

                                    GUILayout.Space(15);
                                    if (GUILayout.Button(a.ButtonName))
                                    {
                                        a.Action?.Invoke();
                                    }

                                    GUILayout.EndHorizontal();
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        // GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    if (!toggleValue && m_selectedMenu == d.Key)
                    {
                        m_selectedMenu = null;
                    }
                }
            }

            // GUILayout.EndHorizontal();
        }
    }
}