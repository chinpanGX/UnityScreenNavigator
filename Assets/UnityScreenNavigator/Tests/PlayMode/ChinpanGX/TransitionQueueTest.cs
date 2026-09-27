using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace UnityScreenNavigator.Tests.PlayMode.ChinpanGX
{
    internal sealed class TransitionQueueTest
    {
        private GameObject containerObject;
        private TransitionQueue queue;

        [SetUp]
        public void SetUp()
        {
            containerObject = new GameObject("Container");
            queue = new TransitionQueue(containerObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (containerObject != null)
                UnityEngine.Object.DestroyImmediate(containerObject);
        }

        [Test]
        public async Task NextTransition_StartsAfterPreviousOneFinishes()
        {
            var log = new List<string>();
            var first = new UniTaskCompletionSource();

            var firstTask = queue.EnqueueAsync(async () =>
            {
                log.Add("first start");
                await first.Task;
                log.Add("first end");
            });
            var secondTask = queue.EnqueueAsync(() =>
            {
                log.Add("second");
                return UniTask.CompletedTask;
            });

            CollectionAssert.AreEqual(new[] { "first start" }, log);

            first.TrySetResult();
            await firstTask;
            await secondTask;

            CollectionAssert.AreEqual(new[] { "first start", "first end", "second" }, log);
        }

        [Test]
        public async Task FailedTransition_DoesNotBlockNextOne()
        {
            var secondRan = false;

            var firstTask = queue.EnqueueAsync(() =>
                UniTask.FromException(new InvalidOperationException("load failed")));
            var secondTask = queue.EnqueueAsync(() =>
            {
                secondRan = true;
                return UniTask.CompletedTask;
            });

            Assert.ThrowsAsync<InvalidOperationException>(async () => await firstTask);
            await secondTask;
            Assert.IsTrue(secondRan);
        }

        [Test]
        public async Task Transition_IsCanceledWhenContainerIsDestroyedWhileWaiting()
        {
            var first = new UniTaskCompletionSource();
            var secondRan = false;

            var firstTask = queue.EnqueueAsync(() => first.Task);
            var secondTask = queue.EnqueueAsync(() =>
            {
                secondRan = true;
                return UniTask.CompletedTask;
            });

            UnityEngine.Object.DestroyImmediate(containerObject);
            first.TrySetResult();
            await firstTask;

            Assert.CatchAsync<OperationCanceledException>(async () => await secondTask);
            Assert.IsFalse(secondRan);
        }
    }
}
