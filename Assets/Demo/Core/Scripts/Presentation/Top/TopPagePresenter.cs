using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Demo.Core.Scripts.Foundation.Common;
using Demo.Core.Scripts.Presentation.Loading;
using Demo.Core.Scripts.View.Top;
using Demo.Subsystem.Misc;
using R3;
using UnityScreenNavigator;

namespace Demo.Core.Scripts.Presentation.Top
{
    [AssetAddress(ResourceKey.Prefabs.TopPage)]
    public sealed class TopPagePresenter : IPresenter, ILifecycleHandler, IDisposableCollectionHolder
    {
        private readonly TopPage view;
        private readonly IScreenNavigator screenNavigator;
        private readonly List<IDisposable> disposables = new();

        public TopPagePresenter(TopPage view, IScreenNavigator screenNavigator)
        {
            this.view = view;
            this.screenNavigator = screenNavigator;
        }

        ICollection<IDisposable> IDisposableCollectionHolder.GetDisposableCollection() => disposables;

        public async UniTask InitializeAsync()
        {
            var viewState = new TopViewState();
            disposables.Add(viewState);

            viewState.OnClicked
                .Subscribe(_ => screenNavigator.PushPageAsync<LoadingPagePresenter>(stack: false).Forget())
                .AddTo(this);

            await view.root.InitializeAsync(viewState);
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
                disposable.Dispose();
        }
    }
}
