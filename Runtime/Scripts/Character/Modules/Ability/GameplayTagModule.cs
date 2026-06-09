using System;
using System.Collections.Generic;

namespace NobunAtelier
{
    /// <summary>
    /// Ref-counted gameplay tag storage with transition-based observer events.
    /// Dumb container — doesn't decide what tags mean. Consumers query and react.
    /// </summary>
    public class GameplayTagModule : CharacterAbilityModuleBase
    {
        private readonly Dictionary<GameplayTagDefinition, int> m_Tags = new();
        private readonly Dictionary<GameplayTagDefinition, List<(Action onBegin, Action onEnd)>> m_Observers = new();

        public void GrantTag(GameplayTagDefinition tag)
        {
            if (tag == null)
                return;

            m_Tags.TryGetValue(tag, out int count);
            m_Tags[tag] = count + 1;

            if (count == 0)
                FireBeginCallbacks(tag);
        }

        public void RevokeTag(GameplayTagDefinition tag)
        {
            if (tag == null)
                return;

            if (!m_Tags.TryGetValue(tag, out int count) || count <= 0)
                return;

            count--;
            if (count <= 0)
            {
                m_Tags.Remove(tag);
                FireEndCallbacks(tag);
            }
            else
            {
                m_Tags[tag] = count;
            }
        }

        public bool HasTag(GameplayTagDefinition tag)
        {
            return tag != null && m_Tags.TryGetValue(tag, out int count) && count > 0;
        }

        public int GetTagCount(GameplayTagDefinition tag)
        {
            if (tag == null)
                return 0;
            return m_Tags.TryGetValue(tag, out int count) ? count : 0;
        }

        public void Register(GameplayTagDefinition tag, Action onBegin, Action onEnd)
        {
            if (tag == null)
                return;

            if (!m_Observers.TryGetValue(tag, out var list))
            {
                list = new List<(Action, Action)>();
                m_Observers[tag] = list;
            }
            list.Add((onBegin, onEnd));
        }

        public void Unregister(GameplayTagDefinition tag, Action onBegin, Action onEnd)
        {
            if (tag == null || !m_Observers.TryGetValue(tag, out var list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].onBegin == onBegin && list[i].onEnd == onEnd)
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }

        private void FireBeginCallbacks(GameplayTagDefinition tag)
        {
            if (!m_Observers.TryGetValue(tag, out var list))
                return;
            for (int i = 0; i < list.Count; i++)
                list[i].onBegin?.Invoke();
        }

        private void FireEndCallbacks(GameplayTagDefinition tag)
        {
            if (!m_Observers.TryGetValue(tag, out var list))
                return;
            for (int i = 0; i < list.Count; i++)
                list[i].onEnd?.Invoke();
        }
    }
}
