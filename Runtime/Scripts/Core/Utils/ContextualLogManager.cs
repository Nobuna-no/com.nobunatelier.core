using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace NobunAtelier
{
    public class ContextualLogManager : SingletonMonoBehaviour<ContextualLogManager>
    {
        [Header("Contextual Log")]
        [SerializeField] private LogSettings m_defaultLogSettings;
        [SerializeField]
        private List<LogPartition> m_LogPartitions = new List<LogPartition>();

        [System.Flags]
        public enum LogOutput
        {
            LogInConsole = 1 << 0,
            SerializeLog = 1 << 1,
        }

        [System.Flags]
        public enum LogEntryDetails
        {
            FrameCount = 1 << 0,
            Context = 1 << 1,
            FuncName = 1 << 2,
            ContextState = 1 << 3,
        }

        [System.Flags]
        public enum LogTypeFilter
        {
            Info = 1 << 0,
            Update = 1 << 1,
            Warning = 1 << 2,
            Error = 1 << 3,
        }

        public interface IStateProvider
        {
            string LogSectionName { get; }

            string GetStateMessage();
        }

        public static LogPartition Register(UnityEngine.Object context, LogSettings settings = null, IStateProvider stateProvider = null)
        {
            if (!IsSingletonValid)
            {
                CreateAndInitialize();
            }

            var hash = GenerateHash(context, stateProvider);

            var section = Instance.m_LogPartitions.Find((s) => s.Hash == hash);
            if (section == null)
            {
                section = new LogPartition(hash, context, stateProvider, settings != null ? settings : Instance.m_defaultLogSettings);
                Instance.m_LogPartitions.Add(section);
            }

            return section;
        }

        public static void Unregister(LogPartition section)
        {
            if (section == null)
            {
                return;
            }

            section.Clear();

            if (Instance.m_LogPartitions.Contains(section))
            {
                Instance.m_LogPartitions.Remove(section);
            }
        }

        private static int GenerateHash(UnityEngine.Object context, IStateProvider stateProvider = null)
        {
            string sectionName = (context != null ? context.name : "Unknown");
            if (stateProvider != null)
            {
                sectionName += stateProvider.LogSectionName;
            }

            return sectionName.GetHashCode();
        }

        [System.Serializable]
        public class LogSettings
        {
            [SerializeField] private LogOutput m_Action = LogOutput.LogInConsole;
            [SerializeField]
            private LogEntryDetails m_EntryCustomization
                = LogEntryDetails.FrameCount | LogEntryDetails.Context | LogEntryDetails.FuncName;
            [SerializeField] private LogTypeFilter m_Filter = LogTypeFilter.Error;

            public LogEntryDetails EntryCustomization => m_EntryCustomization;
            public LogTypeFilter Filter => m_Filter;
            public LogOutput Options => m_Action;
        }

        [System.Serializable]
        public class LogPartition
        {
            public readonly int Hash;
            public readonly Object Context;
            public readonly IStateProvider StateProvider;
            public readonly LogSettings Settings;

            [SerializeField]
            private List<LogSubPartition> m_Partitions;

            public LogPartition(int hash, Object context, IStateProvider stateProvider, LogSettings settings)
            {
                Hash = hash;
                Context = context;
                StateProvider = stateProvider;
                Settings = settings;
                m_Partitions = new List<LogSubPartition>();
            }

            public void Add(LogTypeFilter flags = LogTypeFilter.Info, [CallerMemberName] string funcName = null)
            {
                Add(string.Empty, flags, funcName);
            }

            public void Add(string message, LogTypeFilter flags = LogTypeFilter.Info, [CallerMemberName] string funcName = null)
            {
                if ((Settings.Filter & flags) == 0)
                {
                    return;
                }

                LogSubPartition partition = GetActiveSubPartition();
                StringBuilder sb = new StringBuilder();

                if ((Settings.EntryCustomization & LogEntryDetails.FrameCount) != 0)
                {
                    sb.Append($"[{Time.frameCount}] ");
                }
                if ((Settings.EntryCustomization & LogEntryDetails.Context) != 0)
                {
                    sb.Append($"<b>{(Context != null ? Context.name : "???")}</b> ");
                }
                if ((Settings.EntryCustomization & LogEntryDetails.FuncName) != 0)
                {
                    sb.Append($"<<i>{funcName}</i>> ");
                }

                sb.AppendLine(message);

                if (StateProvider != null && (Settings.EntryCustomization & LogEntryDetails.ContextState) != 0)
                {
                    string data = StateProvider.GetStateMessage();
                    sb.Append(data);
                }

                message = sb.ToString();

                if ((Settings.Options & LogOutput.SerializeLog) != 0)
                {
                    partition.Entries.Add(message);
                }

                if ((Settings.Options & LogOutput.LogInConsole) != 0)
                {
                    LogToConsole(message, flags);
                }
            }

            public void Clear()
            {
                m_Partitions.Clear();
            }

            private void LogToConsole(string message, LogTypeFilter flags)
            {
                if ((flags & LogTypeFilter.Error) != 0)
                {
                    if (Context != null)
                    {
                        Debug.LogError(message, Context);
                    }
                    else
                    {
                        Debug.LogError(message);
                    }
                }
                else if ((flags & LogTypeFilter.Warning) != 0)
                {
                    if (Context != null)
                    {
                        Debug.LogWarning(message, Context);
                    }
                    else
                    {
                        Debug.LogWarning(message);
                    }
                }
                else
                {
                    if (Context != null)
                    {
                        Debug.Log(message, Context);
                    }
                    else
                    {
                        Debug.Log(message);
                    }
                }
            }

            private LogSubPartition GetActiveSubPartition()
            {
                string partitionName = StateProvider?.LogSectionName ?? "Default";
                LogSubPartition partition = m_Partitions.Find(ss => ss.Name == partitionName) ?? new LogSubPartition { Name = partitionName, Entries = new List<string>() };

                if (!m_Partitions.Contains(partition))
                {
                    m_Partitions.Add(partition);
                }

                return partition;
            }
        }

        [System.Serializable]
        private class LogSubPartition
        {
            public string Name;
            public List<string> Entries = new List<string>();
        }
    }
}