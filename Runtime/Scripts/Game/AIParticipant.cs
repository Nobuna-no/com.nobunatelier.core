namespace NobunAtelier
{
    // The class is used to handle AI player interactions within the game mode.
    public class AIParticipant : GameModeParticipant
    {
        public override bool IsAI => true;

        private AIController m_aiController;

        public AIController AIController
        {
            get
            {
                if (m_aiController == null)
                {
                    m_aiController = Controller as AIController;
                }

                return m_aiController;
            }
        }
    }
}