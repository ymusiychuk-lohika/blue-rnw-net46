﻿using NUnit.Framework;
using Newtonsoft.Json.Linq;
using ReactNative.Bridge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReactNative.Tests.Bridge
{
    [TestFixture]
    public class NativeModuleBaseTests
    {
        [Test]
        public void NativeModuleBase_ReactMethod_ThrowsNotSupported()
        {
            var actions = new Action[]
            {
                () => new MethodOverloadNotSupportedNativeModule(),
                () => new ReturnTypeNotSupportedNativeModule(),
                () => new CallbackNotSupportedNativeModule(),
                () => new CallbackNotSupportedNativeModule2(),
                () => new PromiseNotSupportedNativeModule(),
                () => new AsyncCallbackNotSupportedNativeModule(),
                () => new AsyncPromiseNotSupportedNativeModule(),
            };

            foreach (var action in actions)
            {
                AssertEx.Throws<NotSupportedException>(action);
            }
        }

        [Test]
        public void NativeModuleBase_ReactMethod_Async_ThrowsNotImplemented()
        {
            AssertEx.Throws<NotImplementedException>(() => new AsyncNotImplementedNativeModule());
        }

        [Test]
        public void NativeModuleBase_Invocation_ArgumentNull()
        {
            var testModule = new TestNativeModule();

            testModule.Initialize();

            var reactInstance = new MockReactInstance();
            AssertEx.Throws<ArgumentNullException>(
                () => testModule.Methods[nameof(TestNativeModule.Foo)].Invoke(null, new JArray()),
                ex => Assert.AreEqual("reactInstance", ex.ParamName));
            AssertEx.Throws<ArgumentNullException>(
                () => testModule.Methods[nameof(TestNativeModule.Foo)].Invoke(reactInstance, null),
                ex => Assert.AreEqual("jsArguments", ex.ParamName));
        }

        [Test]
        public void NativeModuleBase_Invocation_ArgumentInvalidCount()
        {
            var testModule = new TestNativeModule();

            testModule.Initialize();

            var reactInstance = new MockReactInstance();
            AssertEx.Throws<NativeArgumentsParseException>(
                () => testModule.Methods[nameof(TestNativeModule.Bar)].Invoke(reactInstance, new JArray()),
                ex => Assert.AreEqual("jsArguments", ex.ParamName));
        }

        [Test]
        public void NativeModuleBase_Invocation_ArgumentConversionException()
        {
            var testModule = new TestNativeModule();

            testModule.Initialize();

            var reactInstance = new MockReactInstance();
            AssertEx.Throws<NativeArgumentsParseException>(
                () => testModule.Methods[nameof(TestNativeModule.Bar)].Invoke(reactInstance, JArray.FromObject(new[] { default(object) })),
                ex => Assert.AreEqual("arguments", ex.ParamName));
        }

        [Test]
        public void NativeModuleBase_Invocation()
        {
            var fooCount = 0;
            var barSum = 0;
            var testModule = new TestNativeModule(() => fooCount++, x => barSum += x);

            testModule.Initialize();

            Assert.AreEqual(2, testModule.Methods.Count);

            var reactInstance = new MockReactInstance();
            testModule.Methods[nameof(TestNativeModule.Foo)].Invoke(reactInstance, new JArray());
            testModule.Methods[nameof(TestNativeModule.Foo)].Invoke(reactInstance, new JArray());
            Assert.AreEqual(2, fooCount);

            testModule.Methods[nameof(TestNativeModule.Bar)].Invoke(reactInstance, JArray.FromObject(new[] { 42 }));
            testModule.Methods[nameof(TestNativeModule.Bar)].Invoke(reactInstance, JArray.FromObject(new[] { 17 }));
            Assert.AreEqual(59, barSum);
        }

        [Test]
        public void NativeModuleBase_Invocation_Callbacks()
        {
            var callbackArgs = new object[] { 1, 2, 3 };
            var module = new CallbackNativeModule(callbackArgs);
            module.Initialize();

            var id = default(int);
            var args = default(List<int>);

            var reactInstance = new MockReactInstance((i, a) =>
            {
                id = i;
                args = a.ToObject<List<int>>();
            });

            module.Methods[nameof(CallbackNativeModule.Foo)].Invoke(reactInstance, JArray.FromObject(new[] { 42 }));
            Assert.AreEqual(42, id);
            Assert.IsTrue(args.Cast<object>().SequenceEqual(callbackArgs));
        }

        [Test]
        public void NativeModuleBase_Invocation_Callbacks_InvalidArgumentThrows()
        {
            var callbackArgs = new object[] { 1, 2, 3 };
            var module = new CallbackNativeModule(callbackArgs);
            module.Initialize();

            var id = default(int);
            var args = default(List<int>);

            var reactInstance = new MockReactInstance((i, a) =>
            {
                id = i;
                args = a.ToObject<List<int>>();
            });

            AssertEx.Throws<NativeArgumentsParseException>(
                () => module.Methods[nameof(CallbackNativeModule.Foo)].Invoke(reactInstance, JArray.FromObject(new[] { default(object) })),
                ex => Assert.AreEqual("arguments", ex.ParamName));
        }

        [Test]
        public void NativeModuleBase_Invocation_Callbacks_NullCallback()
        {
            var module = new CallbackNativeModule(null);
            module.Initialize();

            var id = default(int);
            var args = default(List<int>);

            var reactInstance = new MockReactInstance((i, a) =>
            {
                id = i;
                args = a.ToObject<List<int>>();
            });

            module.Methods[nameof(CallbackNativeModule.Foo)].Invoke(reactInstance, JArray.FromObject(new[] { 42 }));
            Assert.AreEqual(0, args.Count);
        }

        [Test]
        public void NativeModuleBase_Invocation_Promises_Resolve()
        {
            var module = new PromiseNativeModule(() => 17);
            module.Initialize();

            var id = default(int);
            var args = default(List<int>);

            var reactInstance = new MockReactInstance((i, a) =>
            {
                id = i;
                args = a.ToObject<List<int>>();
            });

            module.Methods[nameof(PromiseNativeModule.Foo)].Invoke(reactInstance, JArray.FromObject(new[] { 42, 43 }));
            Assert.AreEqual(42, id);
            Assert.IsTrue(args.SequenceEqual(new[] { 17 }));
        }

        [Test]
        public void NativeModuleBase_CompiledDelegateFactory_Perf()
        {
            var module = new PerfNativeModule(CompiledReactDelegateFactory.Instance);
            var reactInstance = new MockReactInstance();
            var args = JArray.FromObject(new[] { 42 });

            module.Initialize();

            var n = 100000;
            for (var i = 0; i < n; ++i)
            {
                module.Methods[nameof(PerfNativeModule.Foo)].Invoke(reactInstance, args);
            }
        }

        [Test]
        public void NativeModuleBase_Invocation_Promises_InvalidArgumentThrows()
        {
            var module = new PromiseNativeModule(() => 17);
            module.Initialize();

            var id = default(int);
            var args = default(List<int>);

            var reactInstance = new MockReactInstance((i, a) =>
            {
                id = i;
                args = a.ToObject<List<int>>();
            });

            AssertEx.Throws<NativeArgumentsParseException>(
                () => module.Methods[nameof(PromiseNativeModule.Foo)].Invoke(reactInstance, JArray.FromObject(new[] { default(object), 43 })),
                ex => Assert.AreEqual("arguments", ex.ParamName));

            AssertEx.Throws<NativeArgumentsParseException>(
                () => module.Methods[nameof(PromiseNativeModule.Foo)].Invoke(reactInstance, JArray.FromObject(new[] { 42, default(object) })),
                ex => Assert.AreEqual("arguments", ex.ParamName));
        }

        [Test]
        public void NativeModuleBase_Invocation_Promises_IncorrectArgumentCount()
        {
            var module = new PromiseNativeModule(() => null);
            module.Initialize();

            var id = default(int);
            var args = default(List<object>);

            var reactInstance = new MockReactInstance((i, a) =>
            {
                id = i;
                args = a.ToObject<List<object>>();
            });

            AssertEx.Throws<NativeArgumentsParseException>(
                () => module.Methods[nameof(PromiseNativeModule.Foo)].Invoke(reactInstance, JArray.FromObject(new[] { 42 })),
                ex => Assert.AreEqual("jsArguments", ex.ParamName));
        }

        [Test]
        public void NativeModuleBase_Invocation_Promises_Reject()
        {
            var expectedMessage = "Foo bar baz";
            var exception = new Exception(expectedMessage);
            var module = new PromiseNativeModule(() => { throw exception; });
            module.Initialize();

            var id = default(int);
            var args = default(JArray);

            var reactInstance = new MockReactInstance((i, a) =>
            {
                id = i;
                args = a;
            });

            module.Methods[nameof(CallbackNativeModule.Foo)].Invoke(reactInstance, JArray.FromObject(new[] { 42, 43 }));
            Assert.AreEqual(43, id);
            Assert.AreEqual(1, args.Count);
            var error = args[0] as JObject;
            Assert.IsNotNull(error);
            Assert.AreEqual(4, error.Count);
            var actualMessage = error.Value<string>("message");
            Assert.AreEqual(expectedMessage, actualMessage);
        }

        [Test]
        public void NativeModuleBase_Invocation_Promises_NullCallback()
        {
            var module = new PromiseNativeModule(() => null);
            module.Initialize();

            var id = default(int);
            var args = default(List<object>);

            var reactInstance = new MockReactInstance((i, a) =>
            {
                id = i;
                args = a.ToObject<List<object>>();
            });

            module.Methods[nameof(PromiseNativeModule.Foo)].Invoke(reactInstance, JArray.FromObject(new[] { 42, 43 }));
            Assert.AreEqual(1, args.Count);
            Assert.IsNull(args[0]);
        }

        [Test]
        public void NativeModuleBase_ReflectionDelegateFactory_Perf()
        {
            var module = new PerfNativeModule(ReflectionReactDelegateFactory.Instance);
            var reactInstance = new MockReactInstance();
            var args = JArray.FromObject(new[] { 42 });

            module.Initialize();

            var n = 100000;
            for (var i = 0; i < n; ++i)
            {
                module.Methods[nameof(PerfNativeModule.Foo)].Invoke(reactInstance, args);
            }
        }

        class MethodOverloadNotSupportedNativeModule : NativeModuleBase
        {
            public override string Name
            {
                get
                {
                    return "Test";
                }
            }

            [ReactMethod]
            public void Foo()
            {
            }

            [ReactMethod]
            public void Foo(int x)
            {
            }
        }

        class ReturnTypeNotSupportedNativeModule : NativeModuleBase
        {
            public override string Name
            {
                get
                {
                    return "Test";
                }
            }

            [ReactMethod]
            public int Foo() { return 0; }
        }
        
        class CallbackNotSupportedNativeModule : NativeModuleBase
        {
            public override string Name
            {
                get
                {
                    return "Test";
                }
            }

            [ReactMethod]
            public void Foo(ICallback foo, int bar, string qux) { }
        }

        class CallbackNotSupportedNativeModule2 : NativeModuleBase
        {
            public override string Name
            {
                get
                {
                    return "Test";
                }
            }

            [ReactMethod]
            public void Foo(ICallback bar, int foo) { }
        }

        class PromiseNotSupportedNativeModule : NativeModuleBase
        {
            public override string Name
            {
                get
                {
                    return "Test";
                }
            }

            [ReactMethod]
            public void Foo(IPromise promise, int foo) { }
        }

        class AsyncCallbackNotSupportedNativeModule : NativeModuleBase
        {
            public override string Name
            {
                get
                {
                    return "Test";
                }
            }

            [ReactMethod]
            public Task Foo(ICallback callback)
            {
                return Task.CompletedTask;
            }
        }

        class AsyncPromiseNotSupportedNativeModule : NativeModuleBase
        {
            public override string Name
            {
                get
                {
                    return "Test";
                }
            }

            [ReactMethod]
            public Task Foo(IPromise promise)
            {
                return Task.CompletedTask;
            }
        }

        class AsyncNotImplementedNativeModule : NativeModuleBase
        {
            public override string Name
            {
                get
                {
                    return "Test";
                }
            }

            [ReactMethod]
            public Task Foo()
            {
                return Task.CompletedTask;
            }
        }

        class TestNativeModule : NativeModuleBase
        {
            private readonly Action _onFoo;
            private readonly Action<int> _onBar;

            public TestNativeModule()
                : this(() => { }, _ => { })
            {
            }

            public TestNativeModule(Action onFoo, Action<int> onBar)
            {
                _onFoo = onFoo;
                _onBar = onBar;
            }

            public override string Name
            {
                get
                {
                    return "Foo";
                }
            }

            [ReactMethod]
            public void Foo()
            {
                _onFoo();
            }

            [ReactMethod]
            public void Bar(int x)
            {
                _onBar(x);
            }
        }

        class CallbackNativeModule : NativeModuleBase
        {
            private readonly object[] _callbackArgs;

            public CallbackNativeModule()
                : this(null)
            {
            }

            public CallbackNativeModule(object[] callbackArgs)
            {
                _callbackArgs = callbackArgs;
            }

            public override string Name
            {
                get
                {
                    return "Test";
                }
            }

            [ReactMethod]
            public void Foo(ICallback callback)
            {
                callback.Invoke(_callbackArgs);
            }
        }

        class PromiseNativeModule : NativeModuleBase
        {
            private readonly Func<object> _resolveFactory;

            public PromiseNativeModule()
                : this(() => null)
            {
            }

            public PromiseNativeModule(Func<object> resolveFactory)
            {
                _resolveFactory = resolveFactory;
            }

            public override string Name
            {
                get
                {
                    return "Test";
                }
            }

            [ReactMethod]
            public void Foo(IPromise promise)
            {
                try
                {
                    promise.Resolve(_resolveFactory());
                }
                catch (Exception ex)
                {
                    promise.Reject(ex);
                }
            }
        }

        class PerfNativeModule : NativeModuleBase
        {
            public PerfNativeModule(IReactDelegateFactory delegateFactory)
                : base(delegateFactory)
            {
            }

            public override string Name
            {
                get
                {
                    return "Perf";
                }
            }

            [ReactMethod]
            public void Foo(int x) { }
        }
    }
}
