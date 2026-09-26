using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Demo.Core.Scripts.Foundation.Common;
using Demo.Core.Scripts.View.Confirmation;
using Demo.Subsystem.Misc;
using R3;
using UnityScreenNavigator;

namespace Demo.Core.Scripts.Presentation.LockConfirmation
{
    [AssetAddress(ResourceKey.Prefabs.ConfirmationModal)]
    public sealed class LockConfirmationModalPresenter : IPresenter, ILifecycleHandler, IDisposableCollectionHolder
    {
        private readonly ConfirmationModal view;
        private readonly IScreenNavigator screenNavigator;
        private readonly List<IDisposable> disposables = new();

        public LockConfirmationModalPresenter(ConfirmationModal view, IScreenNavigator screenNavigator)
        {
            this.view = view;
            this.screenNavigator = screenNavigator;
        }

        ICollection<IDisposable> IDisposableCollectionHolder.GetDisposableCollection() => disposables;

        public async UniTask InitializeAsync()
        {
            var viewState = new ConfirmationViewState();
            disposables.Add(viewState);

            // Set view state with initial values.
            viewState.Message.Value = "This feature is locked.";

            // Observe changes of view state.
            viewState.CloseButtonClicked
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
