using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public static class IMGUIUtility
    {
        public static float LabelWidth
        {
            get => s_labelWidth;
            set
            {
                if(value <= 0)
                {
                    return;
                }
                
                s_labelWidth = value;
            }
        }
        private static float s_labelWidth;

        private static GUIStyle s_titleStyle = null;

        private static void InitStyles()
        {
            s_titleStyle = new GUIStyle(GUI.skin.label);
            s_titleStyle.alignment = TextAnchor.MiddleCenter;
        }

        public static void DrawTitle(string title)
        {
            if (s_titleStyle == null)
            {
                InitStyles();
            }

            GUILayout.Label($"<b>{title}</b>", s_titleStyle);
            GUI.enabled = false;
            GUILayout.HorizontalSlider(-1, 0, 0);
            GUI.enabled = true;
        }

        public static void DrawLabelValue(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: ", GUILayout.MinWidth(s_labelWidth), GUILayout.ExpandWidth(false));
            GUILayout.Label($"<b>{value}</b>", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
    }
}