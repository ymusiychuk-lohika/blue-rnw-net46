﻿using Newtonsoft.Json.Linq;
using ReactNative.Bridge;
using System;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;

namespace ReactNative.Modules.Image
{
    class ImageLoaderModule : NativeModuleBase
    {
        private const string ErrorInvalidUri = "E_INVALID_URI";
        private const string ErrorPrefetchFailure = "E_PREFETCH_FAILURE";
        private const string ErrorGetSizeFailure = "E_GET_SIZE_FAILURE";

        public override string Name
        {
            get
            {
                return "ImageLoader";
            }
        }

        [ReactMethod]
        public void prefetchImage(string uriString, IPromise promise)
        {
            promise.Reject(ErrorPrefetchFailure, "Prefetch is not yet implemented.");
        }

        [ReactMethod]
        public void getSize(string uriString, IPromise promise)
        {
            if (string.IsNullOrEmpty(uriString))
            {
                promise.Reject(ErrorInvalidUri, "Cannot get the size of an image for an empty URI.");
                return;
            }

            DispatcherHelpers.RunOnDispatcher(async () =>
            {
                try
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    var loadQuery = bitmapImage.GetStreamLoadObservable()
                        .Where(status => status.LoadStatus == ImageLoadStatus.OnLoadEnd)
                        .FirstAsync()
                        .Replay(1);

                    using (loadQuery.Connect())
                    {
                        using (var stream = BitmapImageHelpers.GetStreamAsync(uriString))
                        {
                            bitmapImage.StreamSource = stream;
                        }

                        await loadQuery;
                        bitmapImage.EndInit();

                        promise.Resolve(new JObject
                        {
                            { "width", bitmapImage.PixelWidth },
                            { "height", bitmapImage.PixelHeight },
                        });
                    }
                }
                catch (Exception ex)
                {
                    promise.Reject(ErrorGetSizeFailure, ex.Message);
                }
            });
        }
    }
}
