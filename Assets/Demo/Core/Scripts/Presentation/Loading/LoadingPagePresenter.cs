using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Demo.Core.Scripts.Foundation.Common;
using Demo.Core.Scripts.Presentation.Home;
using Demo.Core.Scripts.View.Loading;
using Demo.Subsystem.Misc;
using UnityScreenNavigator;

namespace Demo.Core.Scripts.Presentation.Loading
{
    [AssetAddress(ResourceKey.Prefabs.LoadingPage)]
    public sealed class LoadingPagePresenter : IPresenter, ILifecycleHandler, IDisposableCollectionHolder
    {
        private readonly LoadingPage view;
        private readonly IScreenNavigator screenNavigator;
        private readonly List<IDisposable> disposables = new();

        public LoadingPagePresenter(LoadingPage view, IScreenNavigator screenNavigator)
        {
            this.view = view;
            this.screenNavigator = screenNavigator;
        }

        ICollection<IDisposable> IDisposableCollectionHolder.GetDisposableCollection() => disposables;

        public async UniTask InitializeAsync()
        {
            var viewState = new LoadingViewState();
            disposables.Add(viewState);
            await view.root.InitializeAsync(viewState);
        }

        public void DidPushEnter()
        {
            ShowHomeAsync().Forget();
        }

        private async UniTaskVoid ShowHomeAsync()
        {
            // Wait a frame, matching the original transition timing.
            await UniTask.Yield();
            await screenNavigator.PushPageAsync<HomePagePresenter>();
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
                disposable.Dispose();
        }
    }
}
