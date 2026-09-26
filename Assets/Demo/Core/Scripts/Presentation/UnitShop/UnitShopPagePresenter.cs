using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Demo.Core.Scripts.Domain.UnitShop.MasterRepository;
using Demo.Core.Scripts.Domain.UnitShop.Model;
using Demo.Core.Scripts.Foundation.Common;
using Demo.Core.Scripts.Presentation.UnitTypeInformation;
using Demo.Core.Scripts.UseCase.UnitShop;
using Demo.Core.Scripts.View.Overlay;
using Demo.Core.Scripts.View.UnitShop;
using Demo.Subsystem.Misc;
using R3;
using UnityScreenNavigator;

namespace Demo.Core.Scripts.Presentation.UnitShop
{
    [AssetAddress(ResourceKey.Prefabs.UnitShopPage)]
    public sealed class UnitShopPagePresenter : IPresenter, ILifecycleHandler, IDisposableCollectionHolder
    {
        private readonly UnitShopPage view;
        private readonly IScreenNavigator screenNavigator;
        private readonly IUnitShopMasterRepository unitShopMasterRepository;
        private readonly UnitShopUseCase unitShopUseCase;
        private readonly ConnectingView connectingView;
        private readonly List<IDisposable> disposables = new();

        public UnitShopPagePresenter(UnitShopPage view, IScreenNavigator screenNavigator,
            UnitShopUseCase unitShopUseCase, IUnitShopMasterRepository unitShopMasterRepository,
            ConnectingView connectingView)
        {
            this.view = view;
            this.screenNavigator = screenNavigator;
            this.unitShopUseCase = unitShopUseCase;
            this.unitShopMasterRepository = unitShopMasterRepository;
            this.connectingView = connectingView;
        }

        ICollection<IDisposable> IDisposableCollectionHolder.GetDisposableCollection() => disposables;

        public async UniTask InitializeAsync()
        {
            var viewState = new UnitShopViewState();
            disposables.Add(viewState);

            IUnitShopItemMasterTable masterTable = null;
            await UniTask.WhenAll(
                UniTask.Create(async () => masterTable = await unitShopMasterRepository.FetchItemTableAsync()),
                unitShopUseCase.FetchItemSetAsync()
            );
            var model = unitShopUseCase.Model;

            SetupShopItemSetView(viewState.RegularItems, model.RegularItems, masterTable);
            SetupShopItemSetView(viewState.SpecialItems, model.SpecialItems, masterTable);
            SetupShopItemSetView(viewState.SaleItems, model.SaleItems, masterTable);

            viewState.OnBackButtonClicked
                .Subscribe(_ => screenNavigator.PopPageAsync(this).Forget())
                .AddTo(this);

            // Viewへのバインドは、ViewStateの初期値がすべて揃ってから行う
            // (UnitShopItemViewのようにImageResourceKeyを一度だけ同期的に読むViewがあるため)
            await view.root.InitializeAsync(viewState);
        }

        private void SetupShopItemSetView(UnitShopItemSetViewState viewState,
            IReadOnlyList<UnitShopItem> shopItemModels, IUnitShopItemMasterTable masterTable)
        {
            var shopItemViewStates = new List<UnitShopItemViewState>
            {
                viewState.Item1,
                viewState.Item2,
                viewState.Item3,
                viewState.Item4,
                viewState.Item5,
                viewState.Item6,
                viewState.Item7,
                viewState.Item8
            };
            for (var i = 0; i < shopItemViewStates.Count; i++)
            {
                var shopItemViewState = shopItemViewStates[i];
                if (shopItemModels.Count <= i)
                {
                    SetupLockedUnitShopItemView(shopItemViewState);
                }
                else
                {
                    var shopItemModel = shopItemModels[i];
                    var shopItemMaster = masterTable.FindById(shopItemModel.MasterId);
                    SetupUnlockedUnitShopItemView(shopItemViewState, shopItemModel, shopItemMaster);
                }
            }
        }

        private void SetupUnlockedUnitShopItemView(UnitShopItemViewState viewState, UnitShopItem model,
            UnitShopItemMaster master)
        {
            var unitTypeMasterId = master.UnitTypeMasterId;
            viewState.IsLocked.Value = false;
            viewState.Thumbnail.ImageResourceKey.Value = ResourceKey.Textures.GetUnitThumbnail(unitTypeMasterId, 1);
            viewState.Thumbnail.OnClicked
                .Subscribe(_ => screenNavigator
                    .PushModalAsync<UnitTypeInformationModalPresenter, UnitTypeInformationModalArgs>(
                        new UnitTypeInformationModalArgs(unitTypeMasterId))
                    .Forget())
                .AddTo(this);
            viewState.Cost.Value = master.Cost;
            viewState.CostIconImageResourceKey.Value = ResourceKey.Textures.CoinIcon;
            viewState.IsSoldOut.Value = model.IsSoldOut;

            viewState.OnBuyButtonClicked
                .Subscribe(_ => BuyAsync().Forget())
                .AddTo(this);

            model.ValueChanged
                .Subscribe(_ => viewState.IsSoldOut.Value = model.IsSoldOut)
                .AddTo(this);

            async UniTask BuyAsync()
            {
                await connectingView.ShowAsync();
                var request = new UnitShopUseCase.PurchaseItemRequest(model.Id);
                await unitShopUseCase.PurchaseItemAsync(request);
                viewState.IsSoldOut.Value = true;
                await connectingView.HideAsync();
            }
        }

        private void SetupLockedUnitShopItemView(UnitShopItemViewState viewState)
        {
            viewState.IsLocked.Value = true;
            viewState.CostIconImageResourceKey.Value = ResourceKey.Textures.CoinIcon;
            viewState.IsSoldOut.Value = false;
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
                disposable.Dispose();
        }
    }
}
