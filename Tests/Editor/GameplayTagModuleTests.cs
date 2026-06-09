using NobunAtelier.Tests;
using NUnit.Framework;
using UnityEngine;

namespace NobunAtelier.Core.Editor.Tests
{
    [TestFixture]
    public class GameplayTagModuleTests
    {
        private TestScope m_Scope;
        private GameplayTagModule m_Module;

        [SetUp]
        public void SetUp()
        {
            m_Scope = new TestScope();
            var go = m_Scope.CreateGameObject("TagModuleTest");
            m_Module = go.AddComponent<GameplayTagModule>();
        }

        [TearDown]
        public void TearDown()
        {
            m_Scope.Dispose();
        }

        #region Core Storage

        [Test]
        public void GrantTag_IncreasesCountToOne()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("Invincible");

            m_Module.GrantTag(tag);

            Assert.IsTrue(m_Module.HasTag(tag));
            Assert.AreEqual(1, m_Module.GetTagCount(tag));
        }

        [Test]
        public void GrantTag_Twice_IncreasesCountToTwo()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("SuperArmor");

            m_Module.GrantTag(tag);
            m_Module.GrantTag(tag);

            Assert.AreEqual(2, m_Module.GetTagCount(tag));
        }

        [Test]
        public void RevokeTag_DecreasesCount()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("SuperArmor");
            m_Module.GrantTag(tag);
            m_Module.GrantTag(tag);

            m_Module.RevokeTag(tag);

            Assert.AreEqual(1, m_Module.GetTagCount(tag));
            Assert.IsTrue(m_Module.HasTag(tag));
        }

        [Test]
        public void RevokeTag_AtOne_RemovesTag()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("Invincible");
            m_Module.GrantTag(tag);

            m_Module.RevokeTag(tag);

            Assert.IsFalse(m_Module.HasTag(tag));
            Assert.AreEqual(0, m_Module.GetTagCount(tag));
        }

        [Test]
        public void HasTag_ReturnsFalse_WhenNotGranted()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("Invincible");

            Assert.IsFalse(m_Module.HasTag(tag));
        }

        #endregion

        #region Null Safety

        [Test]
        public void HasTag_NullTag_ReturnsFalse()
        {
            Assert.IsFalse(m_Module.HasTag(null));
        }

        [Test]
        public void GrantTag_NullTag_NoOp()
        {
            Assert.DoesNotThrow(() => m_Module.GrantTag(null));
        }

        [Test]
        public void RevokeTag_NotGranted_NoOp()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("Unknown");

            Assert.DoesNotThrow(() => m_Module.RevokeTag(tag));
        }

        #endregion

        #region Transition Events

        [Test]
        public void Register_FiresOnBegin_OnFirstGrant()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("Invincible");
            int beginCount = 0;
            m_Module.Register(tag, () => beginCount++, null);

            m_Module.GrantTag(tag);

            Assert.AreEqual(1, beginCount);
        }

        [Test]
        public void Register_DoesNotFireOnBegin_OnSecondGrant()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("SuperArmor");
            int beginCount = 0;
            m_Module.Register(tag, () => beginCount++, null);

            m_Module.GrantTag(tag);
            m_Module.GrantTag(tag);

            Assert.AreEqual(1, beginCount);
        }

        [Test]
        public void Register_FiresOnEnd_WhenCountReachesZero()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("Invincible");
            int endCount = 0;
            m_Module.Register(tag, null, () => endCount++);
            m_Module.GrantTag(tag);

            m_Module.RevokeTag(tag);

            Assert.AreEqual(1, endCount);
        }

        [Test]
        public void Register_DoesNotFireOnEnd_OnPartialRevoke()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("SuperArmor");
            int endCount = 0;
            m_Module.Register(tag, null, () => endCount++);
            m_Module.GrantTag(tag);
            m_Module.GrantTag(tag);

            m_Module.RevokeTag(tag);

            Assert.AreEqual(0, endCount);
        }

        [Test]
        public void Unregister_StopsCallbacks()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("Invincible");
            int beginCount = 0;
            System.Action onBegin = () => beginCount++;
            m_Module.Register(tag, onBegin, null);
            m_Module.Unregister(tag, onBegin, null);

            m_Module.GrantTag(tag);

            Assert.AreEqual(0, beginCount);
        }

        [Test]
        public void Register_MultipleObservers_AllFire()
        {
            var tag = m_Scope.CreateDefinition<GameplayTagDefinition>("TrackTarget");
            int observer1 = 0;
            int observer2 = 0;
            m_Module.Register(tag, () => observer1++, null);
            m_Module.Register(tag, () => observer2++, null);

            m_Module.GrantTag(tag);

            Assert.AreEqual(1, observer1);
            Assert.AreEqual(1, observer2);
        }

        #endregion
    }
}
