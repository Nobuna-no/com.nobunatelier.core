namespace NobunAtelier
{
    // The class is used to handle AI player interactions within the game mode.
    public class LegacyAIParticipant : LegacyGameModeParticipant
    {
        public override bool IsAI => true;

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