﻿using Newtonsoft.Json.Linq;
using ReactNative.UIManager.Events;
using System;
using System.Web;

namespace ReactNative.Views.Web.Events
{
    class WebViewLoadingErrorEvent : Event
    {
        private readonly string _description;

        public WebViewLoadingErrorEvent(int viewTag, string error, string description)
            : base(viewTag, TimeSpan.FromTicks(Environment.TickCount))
        {
            if (description == null)
            {
                _description = error;
            }
            else
            {
                _description = description;
            }
        }

        public override string EventName
        {
            get
            {
                return "topLoadingError";
            }
        }

        public override void Dispatch(RCTEventEmitter eventEmitter)
        {
            var eventData = new JObject
            {
                { "target", ViewTag },
                { "description", _description },
            };

            eventEmitter.receiveEvent(ViewTag, EventName, eventData);
        }
    }
}
