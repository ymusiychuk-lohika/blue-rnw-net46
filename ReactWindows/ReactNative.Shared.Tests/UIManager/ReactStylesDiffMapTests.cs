﻿using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ReactNative.UIManager;
using System;
using System.Linq;

namespace ReactNative.Tests.UIManager
{
    [TestFixture]
    public class ReactStylesDiffMapTests
    {
        [Test]
        public void ReactStylesDiffMap_ArgumentChecks()
        {
            AssertEx.Throws<ArgumentNullException>(
                () => new ReactStylesDiffMap(null),
                ex => Assert.AreEqual("props", ex.ParamName));
        }

        [Test]
        public void ReactStylesDiffMap_ContainsKey()
        {
            var json = new JObject
            {
                { "foo", 42 },
            };

            var props = new ReactStylesDiffMap(json);
            Assert.IsTrue(props.ContainsKey("foo"));
            Assert.IsFalse(props.ContainsKey("FOO"));
            Assert.IsFalse(props.ContainsKey("bar"));
        }

        [Test]
        public void ReactStylesDiffMap_Behavior()
        {
            var json = new JObject
            {
                { "foo", 42 },
                { "bar", "qux" },
            };

            var props = new ReactStylesDiffMap(json);
            Assert.AreEqual(2, props.Keys.Count);
            Assert.IsTrue(new[] { "bar", "foo" }.SequenceEqual(props.Keys.OrderBy(k => k)));
            Assert.IsNotNull(props.GetProperty("foo"));
            Assert.IsNull(props.GetProperty("FOO"));
            Assert.AreEqual((short)42, props.GetProperty("foo").ToObject(typeof(short)));
        }
    }
}
