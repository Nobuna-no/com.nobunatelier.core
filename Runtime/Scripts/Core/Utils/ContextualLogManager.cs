using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

namespace NobunAtelier
{
    public class ContextualLogManager : SingletonMonoBehaviour<ContextualLogManager>
    {
        [Header("Contextual Log")]
        [SerializeField] private bool m_ResetPartitionOnApplicationQuit = true;
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
            ContextState = 1 << 3, // Only if available by IStateProvider.
            ContextType = 1 << 4,
            Ticks = 1 << 5,
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
            string LogPartitionName { get; }

            string GetStateMessage();
        }

        public static LogPartition Register(UnityEngine.Object context, LogSettings settings = null, IStateProvider stateProvider = null)
        {
            if (!IsSingletonValid)
            {
                CreateAndInitialize();
            }

            // Implicit IStateProvider extraction.
            if (stateProvider == null && context is IStateProvider)
            {
                stateProvider = context as IStateProvider;
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
            if (section == null || !IsSingletonValid)
            {
                return;
            }

            section.Clear();

            if (Instance.m_LogPartitions.Contains(section))
            {
                Instance.m_LogPartitions.Remove(section);
            }
        }

        protected override void OnSingletonApplicationQuit()
        {
            if (m_ResetPartitionOnApplicationQuit)
            {
                Instance.m_LogPartitions.Clear();
            }
        }

        private static int GenerateHash(UnityEngine.Object context, IStateProvider stateProvider = null)
        {
            int hash = context.GetHashCode();
            if (stateProvider != null && stateProvider.LogPartitionName != null)
            {
                hash ^= stateProvider.LogPartitionName.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// It is a class to keep the reference to the original object (i.e. as user can change value at runtime in the inspector).
        /// </summary>
        [System.Serializable]
        public class LogSettings
        {
            public static LogSettings Default => new LogSettings();

            [SerializeField] private LogOutput m_Action = LogOutput.LogInConsole;
            [SerializeField] private LogEntryDetails m_Details =
                LogEntryDetails.FrameCount | LogEntryDetails.Context | LogEntryDetails.ContextType | LogEntryDetails.FuncName;
            [SerializeField] private LogTypeFilter m_Filter = LogTypeFilter.Warning | LogTypeFilter.Error;

            public LogEntryDetails Details => m_Details;
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

            public void Record(LogTypeFilter flags = LogTypeFilter.Info, [CallerMemberName] string funcName = null)
            {
                Record(string.Empty, flags, funcName);
            }

            public void Record(string message, LogTypeFilter flags = LogTypeFilter.Info, [CallerMemberName] string funcName = null)
            {
                if ((Settings.Filter & flags) == 0)
                {
                    return;
                }

                LogSubPartition partition = GetActiveSubPartition();
                StringBuilder sb = new StringBuilder();

                if ((Settings.Details & LogEntryDetails.FrameCount) != 0)
                {
                    sb.Append($"[{Time.frameCount}] ");
                }
                if ((Settings.Details & LogEntryDetails.Ticks) != 0)
                {
                    sb.Append($"[{System.DateTime.Now.Ticks}] ");
                }
                if ((Settings.Details & LogEntryDetails.Context) != 0)
                {
                    sb.Append($"<b>{(Context != null ? Context.name : "???")}</b> ");
                }
                if((Settings.Details & LogEntryDetails.ContextType) != 0)
                {
                    sb.Append($"({Context.GetType().Name}) ");
                }
                if ((Settings.Details & LogEntryDetails.FuncName) != 0)
                {
                    sb.Append($"<<i>{funcName}</i>> ");
                }

                sb.AppendLine(message);

                if (StateProvider != null && (Settings.Details & LogEntryDetails.ContextState) != 0)
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
                    Debug.LogError(message, Context);
                }
                else if ((flags & LogTypeFilter.Warning) != 0)
                {
                    Debug.LogWarning(message, Context);
                }
                else
                {
                    Debug.Log(message, Context);
                }
            }

            private LogSubPartition GetActiveSubPartition()
            {
                string partitionName = StateProvider?.LogPartitionName ?? "Default";
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