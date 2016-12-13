﻿using NUnit.Framework;
using ReactNative.UIManager;

namespace ReactNative.Tests.UIManager
{
    [TestFixture]
    public class RootViewHelperTests
    {
        [Test]
        public void RootViewHelper_Null()
        {
            Assert.IsNull(RootViewHelper.GetRootView(null));
        }
    }
}
