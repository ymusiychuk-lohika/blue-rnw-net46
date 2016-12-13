﻿using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using ReactNative.Bridge.Queue;
using System;
using System.Threading;
using System.Threading.Tasks;
using static ReactNative.Tests.DispatcherHelpers;

namespace ReactNative.Tests.Bridge.Queue
{
    [TestClass]
    public class MessageQueueThreadTests
    {
        [TestMethod]
        public void MessageQueueThread_ArgumentChecks()
        {
            AssertEx.Throws<ArgumentNullException>(
                () => MessageQueueThread.Create(null, ex => { }),
                ex => Assert.AreEqual("spec", ex.ParamName));

            AssertEx.Throws<ArgumentNullException>(
                () => MessageQueueThread.Create(MessageQueueThreadSpec.DispatcherThreadSpec, null),
                ex => Assert.AreEqual("handler", ex.ParamName));
        }

        [TestMethod]
        public void MessageQueueThread_CreateUiThread_ThrowsNotSupported()
        {
            AssertEx.Throws<NotSupportedException>(() => MessageQueueThreadSpec.Create("ui", MessageQueueThreadKind.DispatcherThread));
        }

        [TestMethod]
        public async Task MessageQueueThread_IsOnThread()
        {
            var thrown = 0;
            var uiThread = await CallOnDispatcherAsync(() => MessageQueueThread.Create(MessageQueueThreadSpec.DispatcherThreadSpec, ex => thrown++));
            var backgroundThread = MessageQueueThread.Create(MessageQueueThreadSpec.Create("background", MessageQueueThreadKind.BackgroundSingleThread), ex => thrown++);
            var taskPoolThread = MessageQueueThread.Create(MessageQueueThreadSpec.Create("any", MessageQueueThreadKind.BackgroundAnyThread), ex => thrown++);

            var queueThreads = new[]
            {
                uiThread,
                backgroundThread,
                taskPoolThread
            };

            var countdown = new CountdownEvent(queueThreads.Length);
            foreach (var queueThread in queueThreads)
            {
                queueThread.RunOnQueue(() =>
                {
                    Assert.IsTrue(queueThread.IsOnThread());
                    countdown.Signal();
                });
            }

            Assert.IsTrue(countdown.Wait(5000));
            Assert.AreEqual(0, thrown);
        }

        [TestMethod]
        public async Task MessageQueueThread_HandlesException()
        {
            var exception = new Exception();
            var countdown = new CountdownEvent(1);
            var handler = new Action<Exception>(ex =>
            {
                Assert.AreSame(exception, ex);
                countdown.Signal();
            });

            var uiThread = await CallOnDispatcherAsync(() => MessageQueueThread.Create(MessageQueueThreadSpec.DispatcherThreadSpec, handler));
            var backgroundThread = MessageQueueThread.Create(MessageQueueThreadSpec.Create("background", MessageQueueThreadKind.BackgroundSingleThread), handler);
            var taskPoolThread = MessageQueueThread.Create(MessageQueueThreadSpec.Create("any", MessageQueueThreadKind.BackgroundAnyThread), handler);

            var queueThreads = new[]
            {
                uiThread,
                backgroundThread,
                taskPoolThread
            };

            countdown.AddCount(queueThreads.Length - 1);

            foreach (var queueThread in queueThreads)
            {
                queueThread.RunOnQueue(() => { throw exception; });
            }

            Assert.IsTrue(countdown.Wait(5000));
        }

        [TestMethod]
        public async Task MessageQueueThread_OneAtATime()
        {
            var uiThread = await CallOnDispatcherAsync(() => MessageQueueThread.Create(MessageQueueThreadSpec.DispatcherThreadSpec, ex => { Assert.Fail(); }));
            var backgroundThread = MessageQueueThread.Create(MessageQueueThreadSpec.Create("background", MessageQueueThreadKind.BackgroundSingleThread), ex => { Assert.Fail(); });
            var taskPoolThread = MessageQueueThread.Create(MessageQueueThreadSpec.Create("any", MessageQueueThreadKind.BackgroundAnyThread), ex => { Assert.Fail(); });

            var enter = new AutoResetEvent(false);
            var exit = new AutoResetEvent(false);

            var queueThreads = new[] 
            {
                uiThread,
                backgroundThread,
                taskPoolThread
            };

            foreach (var queueThread in queueThreads)
            {
                var count = 10;
                for (var i = 0; i < count; ++i)
                {
                    queueThread.RunOnQueue(() => { enter.Set(); exit.WaitOne(); });
                }

                for (var i = 0; i < count; ++i)
                {
                    Assert.IsTrue(enter.WaitOne());
                    Assert.IsFalse(enter.WaitOne(100));
                    exit.Set();
                }
            }
        }

        [TestMethod]
        public async Task MessageQueueThread_Dispose()
        {
            var uiThread = await CallOnDispatcherAsync(() => MessageQueueThread.Create(MessageQueueThreadSpec.DispatcherThreadSpec, ex => { Assert.Fail(); }));
            var backgroundThread = MessageQueueThread.Create(MessageQueueThreadSpec.Create("background", MessageQueueThreadKind.BackgroundSingleThread), ex => { Assert.Fail(); });
            var taskPoolThread = MessageQueueThread.Create(MessageQueueThreadSpec.Create("any", MessageQueueThreadKind.BackgroundAnyThread), ex => { Assert.Fail(); });

            var queueThreads = new[]
            {
                uiThread,
                backgroundThread,
                taskPoolThread
            };

            var waitHandle = new AutoResetEvent(false);
            foreach (var queueThread in queueThreads)
            {
                queueThread.Dispose();
                queueThread.RunOnQueue(() =>
                {
                    waitHandle.Set();
                });

                Assert.IsFalse(waitHandle.WaitOne(500));
            }
        }
    }
}
