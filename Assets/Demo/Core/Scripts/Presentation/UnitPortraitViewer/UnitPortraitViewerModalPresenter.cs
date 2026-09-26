using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Demo.Core.Scripts.Foundation.Common;
using Demo.Core.Scripts.View.UnitPortraitViewer;
using Demo.Subsystem.Misc;
using R3;
using UnityScreenNavigator;

namespace Demo.Core.Scripts.Presentation.UnitPortraitViewer
{
    [AssetAddress(ResourceKey.Prefabs.UnitPortraitViewerModal)]
    public sealed class UnitPortraitViewerModalPresenter
        : IPresenter, IScreenWithArgs<UnitPortraitViewerModalArgs>, ILifecycleHandler, IDisposableCollectionHolder
    {
        private readonly UnitPortraitViewerModal view;
        private readonly IScreenNavigator screenNavigator;
        private readonly UnitPortraitViewerModalArgs args;
        private readonly List<IDisposable> disposables = new();

        public UnitPortraitViewerModalPresenter(UnitPortraitViewerModal view, IScreenNavigator screenNavigator,
            UnitPortraitViewerModalArgs args)
        {
            this.view = view;
            this.screenNavigator = screenNavigator;
            this.args = args;
        }

        ICollection<IDisposable> IDisposableCollectionHolder.GetDisposableCollection() => disposables;

        public async UniTask InitializeAsync()
        {
            var viewState = new UnitPortraitViewerViewState();
            disposables.Add(viewState);

            viewState.Portrait.ImageResourceKey.Value =
                ResourceKey.Textures.GetUnitPortrait(args.UnitTypeMasterId, args.UnitRank);
            viewState.OnCloseButtonClicked
                .Subscribe(_ => screenNavigator.PopModalAsync(this).Forget())
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
