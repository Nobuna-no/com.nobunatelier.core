using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace NobunAtelier.Tests
{
    public class PlayModeTestLogger : MonoBehaviourService<PlayModeTestLogger>
    {
        [Header("PlayModeTest Logger")]
        [SerializeField, TextArea()]
        private string m_expectedOutput;

        private bool m_isTestRunning = false;
        private StringBuilder m_output = new StringBuilder();

        [SerializeField, TextArea()]
        private string m_outputString;

        public bool IsTestRunning => m_isTestRunning;

        public static bool IsTestSuccessful()
        {
            return string.Compare(Instance.m_output.ToString(), Instance.m_expectedOutput) != 0;
        }

        public static void LogCommand(string log)
        {
            Instance.m_output.AppendLine(log);
            Instance.m_outputString = Instance.m_output.ToString();
        }

        public void EndTest()
        {
            m_isTestRunning = false;
        }

        public void StartTest()
        {
            Instance.m_isTestRunning = true;
        }
    }
}