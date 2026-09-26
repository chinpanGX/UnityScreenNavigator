using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Demo.Core.Scripts.Domain.FeatureFlag.MasterRepository;
using Demo.Core.Scripts.Foundation.Common;
using Demo.Core.Scripts.Presentation.Setting;
using Demo.Core.Scripts.Presentation.UnitShop;
using Demo.Core.Scripts.View.Home;
using Demo.Subsystem.Misc;
using R3;
using UnityScreenNavigator;
using UnityScreenNavigator.Runtime.Core.Page;

namespace Demo.Core.Scripts.Presentation.Home
{
    [AssetAddress(ResourceKey.Prefabs.HomePage)]
    public sealed class HomePagePresenter : IPresenter, ILifecycleHandler, IDisposableCollectionHolder
    {
        private readonly HomePage view;
        private readonly IScreenNavigator screenNavigator;
        private readonly PageContainer pageContainer;
        private readonly IFeatureFlagMasterRepository featureFlagMasterRepository;
        private readonly List<IDisposable> disposables = new();

        public HomePagePresenter(HomePage view, IScreenNavigator screenNavigator, PageContainer pageContainer,
            IFeatureFlagMasterRepository featureFlagMasterRepository)
        {
            this.view = view;
            this.screenNavigator = screenNavigator;
            this.pageContainer = pageContainer;
            this.featureFlagMasterRepository = featureFlagMasterRepository;
        }

        ICollection<IDisposable> IDisposableCollectionHolder.GetDisposableCollection() => disposables;

        public async UniTask InitializeAsync()
        {
            var viewState = new HomeViewState();
            disposables.Add(viewState);

            var featureFlagMasterTable = await featureFlagMasterRepository.FetchTableAsync();

            var isUnitShopEnabled = featureFlagMasterTable.FindById("unit_shop")?.Enabled ?? false;
            var isSettingsEnabled = featureFlagMasterTable.FindById("settings")?.Enabled ?? false;
            var isMainQuestEnabled = featureFlagMasterTable.FindById("main_quest")?.Enabled ?? false;
            var isEventQuestEnabled = featureFlagMasterTable.FindById("event_quest")?.Enabled ?? false;
            var isMissionEnabled = featureFlagMasterTable.FindById("mission")?.Enabled ?? false;

            viewState.UnitShopButton.IsLocked.Value = !isUnitShopEnabled;
            viewState.SettingsButton.IsLocked.Value = !isSettingsEnabled;
            viewState.MainQuestButton.IsLocked.Value = !isMainQuestEnabled;
            viewState.EventQuestButton.IsLocked.Value = !isEventQuestEnabled;
            viewState.MissionButton.IsLocked.Value = !isMissionEnabled;

            viewState.UnitShopButton
                .OnClicked
                .Subscribe(_ => screenNavigator.PushPageAsync<UnitShopPagePresenter>().Forget())
                .AddTo(this);
            viewState.SettingsButton
                .OnClicked
                .Subscribe(_ => screenNavigator.PushModalAsync<SettingsModalPresenter>().Forget())
                .AddTo(this);
            viewState.MainQuestButton
                .OnClicked
                .Subscribe(_ => throw new NotImplementedException())
                .AddTo(this);
            viewState.EventQuestButton
                .OnClicked
                .Subscribe(_ => throw new NotImplementedException())
                .AddTo(this);
            viewState.MissionButton
                .OnClicked
                .Subscribe(_ => throw new NotImplementedException())
                .AddTo(this);
            viewState.OnBackButtonClicked
                .Subscribe(_ => screenNavigator.PopPageAsync(this).Forget())
                .AddTo(this);

            await view.root.InitializeAsync(viewState);
        }

        public async UniTask WillPushEnterAsync()
        {
            // Preload the "Shop" page prefab.
            await pageContainer.Preload(ResourceKey.Prefabs.UnitShopPage);
            // Simulate loading time.
            await UniTask.Delay(1000);
        }

        public UniTask WillPopExitAsync()
        {
            pageContainer.ReleasePreloaded(ResourceKey.Prefabs.UnitShopPage);
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
                disposable.Dispose();
        }
    }
}
