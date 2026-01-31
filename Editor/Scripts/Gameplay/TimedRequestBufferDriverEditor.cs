using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Physarida.Editor
{
    [CustomEditor(typeof(TimedRequestBufferManager))]
    public class TimedRequestBufferDriverEditor : UnityEditor.Editor
    {
        private const int HistoryPreviewCount = 8;
        private readonly List<TimedRequestBuffer> m_Buffers = new List<TimedRequestBuffer>();
        private readonly List<TimedRequestBuffer.DebugInfo> m_DebugInfo = new List<TimedRequestBuffer.DebugInfo>();
        private readonly List<TimedRequestBuffer.HistoryEntry> m_History = new List<TimedRequestBuffer.HistoryEntry>();
        private VisualElement m_ListRoot;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var header = new Label("Timed Request Buffers (Runtime)");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(header);

            root.Add(new HelpBox("Registered buffers are visible in Play Mode.", HelpBoxMessageType.Info));

            m_ListRoot = new VisualElement();
            m_ListRoot.style.marginTop = 6;
            root.Add(m_ListRoot);

            root.schedule.Execute(RefreshList).Every(200);
            RefreshList();

            return root;
        }

        private void RefreshList()
        {
            if (m_ListRoot == null)
            {
                return;
            }

            m_ListRoot.Clear();

            if (!Application.isPlaying)
            {
                m_ListRoot.Add(new HelpBox("Enter Play Mode to view registered buffers.", HelpBoxMessageType.Info));
                return;
            }

            var driver = target as TimedRequestBufferManager;
            if (driver == null)
            {
                m_ListRoot.Add(new HelpBox("TimedRequestBufferDriver instance not found.", HelpBoxMessageType.Warning));
                return;
            }

            driver.GetBuffers(m_Buffers);
            if (m_Buffers.Count == 0)
            {
                m_ListRoot.Add(new HelpBox("No registered buffers.", HelpBoxMessageType.Info));
                return;
            }

            for (int i = 0; i < m_Buffers.Count; i++)
            {
                TimedRequestBuffer buffer = m_Buffers[i];

                var item = new VisualElement();
                item.style.marginBottom = 8;
                item.style.paddingLeft = 4;

                string bufferName = string.IsNullOrWhiteSpace(buffer.Name) ? "<Unnamed>" : buffer.Name;
                item.Add(new Label($"Buffer: {bufferName}"));

                buffer.GetDebugInfo(m_DebugInfo);
                if (m_DebugInfo.Count == 0)
                {
                    item.Add(new Label("Requests: <None>"));
                }
                else
                {
                    for (int j = 0; j < m_DebugInfo.Count; j++)
                    {
                        TimedRequestBuffer.DebugInfo info = m_DebugInfo[j];
                        item.Add(new Label($"{info.DebugLabel}  Owner: {info.OwnerName}"));
                        item.Add(new Label($"Has Request: {info.HasRequest}  Remaining: {info.RemainingTime:0.000}s"));
                        item.Add(new Label($"Buffer: {info.BufferDuration:0.000}s  Unscaled: {info.UseUnscaledTime}"));
                        item.Add(new Label($"Consume: {info.ConsumeMethod}"));
                    }
                }

                buffer.GetHistory(m_History);
                if (m_History.Count > 0)
                {
                    item.Add(new Label($"History (last {HistoryPreviewCount})"));
                    int startIndex = Mathf.Max(0, m_History.Count - HistoryPreviewCount);
                    for (int j = startIndex; j < m_History.Count; j++)
                    {
                        TimedRequestBuffer.HistoryEntry entry = m_History[j];
                        item.Add(new Label($"[{entry.Frame}] {entry.EventType} {entry.DebugLabel} ({entry.OwnerName})"));
                    }
                }

                m_ListRoot.Add(item);
            }
        }
    }
}
