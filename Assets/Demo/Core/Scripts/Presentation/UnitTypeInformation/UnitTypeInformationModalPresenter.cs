using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Demo.Core.Scripts.Domain.Unit.MasterRepository;
using Demo.Core.Scripts.Foundation.Common;
using Demo.Core.Scripts.Presentation.UnitPortraitViewer;
using Demo.Core.Scripts.View.UnitTypeInformation;
using Demo.Subsystem.Misc;
using R3;
using UnityScreenNavigator;

namespace Demo.Core.Scripts.Presentation.UnitTypeInformation
{
    [AssetAddress(ResourceKey.Prefabs.UnitTypeInformationModal)]
    public sealed class UnitTypeInformationModalPresenter
        : IPresenter, IScreenWithArgs<UnitTypeInformationModalArgs>, ILifecycleHandler, IDisposableCollectionHolder
    {
        private readonly UnitTypeInformationModal view;
        private readonly IScreenNavigator screenNavigator;
        private readonly IUnitMasterRepository unitMasterRepository;
        private readonly UnitTypeInformationModalArgs args;
        private readonly List<IDisposable> disposables = new();
        private UnitTypeInformationViewState viewState;

        public UnitTypeInformationModalPresenter(UnitTypeInformationModal view, IScreenNavigator screenNavigator,
            IUnitMasterRepository unitMasterRepository, UnitTypeInformationModalArgs args)
        {
            this.view = view;
            this.screenNavigator = screenNavigator;
            this.unitMasterRepository = unitMasterRepository;
            this.args = args;
        }

        ICollection<IDisposable> IDisposableCollectionHolder.GetDisposableCollection() => disposables;

        public async UniTask InitializeAsync()
        {
            viewState = new UnitTypeInformationViewState();
            disposables.Add(viewState);

            var unitTypeMasterId = args.UnitTypeMasterId;
            var unitTypeTable = await unitMasterRepository.FetchUnitTypeTableAsync();
            var unitTypeMaster = unitTypeTable.FindById(unitTypeMasterId);

            viewState.Title.Value = unitTypeMaster.Name;
            viewState.Rank1Thumbnail.ImageResourceKey.Value =
                ResourceKey.Textures.GetUnitThumbnail(unitTypeMasterId, 1);
            viewState.Rank2Thumbnail.ImageResourceKey.Value =
                ResourceKey.Textures.GetUnitThumbnail(unitTypeMasterId, 2);
            viewState.Rank3Thumbnail.ImageResourceKey.Value =
                ResourceKey.Textures.GetUnitThumbnail(unitTypeMasterId, 3);
            viewState.Rank1Description.Value = unitTypeMaster.Rank1Description;
            viewState.Rank2Description.Value = unitTypeMaster.Rank2Description;
            viewState.Rank3Description.Value = unitTypeMaster.Rank3Description;
            viewState.Rank1Portrait.ImageResourceKey.Value = ResourceKey.Textures.GetUnitPortrait(unitTypeMasterId, 1);
            viewState.Rank2Portrait.ImageResourceKey.Value = ResourceKey.Textures.GetUnitPortrait(unitTypeMasterId, 2);
            viewState.Rank3Portrait.ImageResourceKey.Value = ResourceKey.Textures.GetUnitPortrait(unitTypeMasterId, 3);

            viewState.OnCloseButtonClicked
                .Subscribe(_ => screenNavigator.PopModalAsync(this).Forget())
                .AddTo(this);
            viewState.OnExpandButtonClicked
                .Subscribe(_ =>
                {
                    var unitRank = viewState.TabIndex.Value + 1;
                    screenNavigator
                        .PushModalAsync<UnitPortraitViewerModalPresenter, UnitPortraitViewerModalArgs>(
                            new UnitPortraitViewerModalArgs(unitTypeMasterId, unitRank))
                        .Forget();
                })
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
