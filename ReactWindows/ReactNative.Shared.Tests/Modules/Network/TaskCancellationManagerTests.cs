﻿using NUnit.Framework;
using ReactNative.Modules.Network;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReactNative.Tests.Modules.Network
{
    [TestFixture]
    public class TaskCancellationManagerTests
    {
        [Test]
        public void TaskCancellationManager_ArgumentChecks()
        {
            AssertEx.Throws<ArgumentNullException>(
                () => new TaskCancellationManager<int>(null),
                ex => Assert.AreEqual("keyComparer", ex.ParamName));
        }

        [Test]
        public void TaskCancellationManager_CancelledAfterCompleted()
        {
            var mgr = new TaskCancellationManager<int>();
            mgr.Add(42, _ => Task.CompletedTask);
            mgr.Cancel(42);

            // Not throwing implies success
        }

        [Test]
        public void TaskCancellationManager_CancelTask()
        {
            var enter = new AutoResetEvent(false);
            var exit = new AutoResetEvent(false);
            var mgr = new TaskCancellationManager<int>();
            mgr.Add(42, async token =>
            {
                var tcs = new TaskCompletionSource<bool>();
                using (token.Register(() => tcs.SetResult(true)))
                {
                    enter.Set();
                    await tcs.Task;
                    exit.Set();
                }
            });

            Assert.IsTrue(enter.WaitOne());
            mgr.Cancel(42);
            Assert.IsTrue(exit.WaitOne());
        }

        [Test]
        public async Task TaskCancellationManager_CleanedUpAfterComplete()
        {
            var enter = new AutoResetEvent(false);
            var exit = new AutoResetEvent(false);
            var mgr = new TaskCancellationManager<int>();
            var t = default(Task);
            mgr.Add(42, token =>
            {
                return t = Task.Run(() =>
                {
                    enter.WaitOne();
                    return;
                });
            });

            Assert.IsNotNull(t);
            Assert.AreEqual(1, mgr.PendingOperationCount);
            enter.Set();
            await t;
            Assert.AreEqual(0, mgr.PendingOperationCount);
        }

        [Test]
        public async Task TaskCancellationManager_CleanedUpAfterError()
        {
            var enter = new AutoResetEvent(false);
            var mgr = new TaskCancellationManager<int>();
            var t = default(Task);
            mgr.Add(42, token =>
            {
                return t = Task.Run(() =>
                {
                    enter.WaitOne();
                    throw new InvalidOperationException();
                });
            });

            Assert.IsNotNull(t);
            Assert.AreEqual(1, mgr.PendingOperationCount);
            enter.Set();
            await AssertEx.ThrowsAsync<InvalidOperationException>(async () => await t);
            Assert.AreEqual(0, mgr.PendingOperationCount);
        }
    }
}
