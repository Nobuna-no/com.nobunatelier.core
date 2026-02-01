using System.Collections;
using NUnit.Framework;
using Physarida;
using UnityEngine.TestTools;

namespace NobunAtelier.Core.Tests
{
    public class TimedRequestBufferTests
    {
        [UnityTest]
        public IEnumerator TimedRequestBuffer_ConsumesWhenConditionBecomesTrue()
        {
            var buffer = TimedRequestBufferFactory.Get();
            bool canConsume = false;
            bool consumed = false;

            var handle = buffer.Register()
                .WithBufferDuration(0.5f)
                .When(() => canConsume)
                .OnConsume(() => consumed = true)
                .WithDebugLabel("ChainBufferTest")
                .Build();

            handle.Request();
            yield return null;

            Assert.IsFalse(consumed, "Request should not consume before condition is true.");

            canConsume = true;
            yield return null;
            yield return null;

            Assert.IsTrue(consumed, "Request should consume after condition becomes true.");

            handle.Unregister();
            TimedRequestBufferFactory.Release(buffer);
        }
    }
}
