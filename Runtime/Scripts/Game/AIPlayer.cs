namespace NobunAtelier
{
    // The class is used to handle AI player interactions within the game mode.
    public class AIPlayer : GameModeParticipant
    {
        private LegacyAIControllerBase m_aiController;

        public LegacyAIControllerBase AIController
        {
            get
            {
                if (m_aiController == null)
                {
                    m_aiController = Controller as LegacyAIControllerBase;
                }

                return m_aiController;
            }
        }
    }
}