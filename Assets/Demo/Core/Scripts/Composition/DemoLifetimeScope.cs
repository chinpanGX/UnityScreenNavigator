using Demo.Core.Scripts.APIGateway.Setting;
using Demo.Core.Scripts.APIGateway.UnitShop;
using Demo.Core.Scripts.Domain.FeatureFlag.MasterRepository;
using Demo.Core.Scripts.Domain.Setting.Model;
using Demo.Core.Scripts.Domain.Unit.MasterRepository;
using Demo.Core.Scripts.Domain.UnitShop.MasterRepository;
using Demo.Core.Scripts.Domain.UnitShop.Model;
using Demo.Core.Scripts.MasterRepository.FeatureFlag;
using Demo.Core.Scripts.MasterRepository.Unit;
using Demo.Core.Scripts.MasterRepository.UnitShop;
using Demo.Core.Scripts.UseCase.Setting;
using Demo.Core.Scripts.UseCase.UnitShop;
using UnityEngine;
using UnityScreenNavigator.Runtime.Core.Modal;
using UnityScreenNavigator.Runtime.Core.Page;
using VContainer;
using VContainer.Unity;

namespace Demo.Core.Scripts.Composition
{
    public sealed class DemoLifetimeScope : LifetimeScope
    {
        [SerializeField] private PageContainer pageContainer;
        [SerializeField] private ModalContainer modalContainer;
        [SerializeField] private UnityScreenNavigator.OverlayContainer overlayContainer;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(pageContainer);
            builder.RegisterComponent(modalContainer);
            builder.RegisterComponent(overlayContainer);
            builder.Register<UnityScreenNavigator.IScreenNavigator, UnityScreenNavigator.ScreenNavigator>(
                Lifetime.Singleton);

            builder.Register<Settings>(Lifetime.Singleton);
            builder.Register<UnitShopItemSet>(Lifetime.Singleton);
            builder.Register<SettingsAPIGateway>(Lifetime.Singleton);
            builder.Register<UnitShopAPIGateway>(Lifetime.Singleton);
            builder.Register<FeatureFlagMasterRepository>(Lifetime.Singleton).As<IFeatureFlagMasterRepository>();
            builder.Register<UnitShopMasterRepository>(Lifetime.Singleton).As<IUnitShopMasterRepository>();
            builder.Register<UnitMasterRepository>(Lifetime.Singleton).As<IUnitMasterRepository>();
            builder.Register<SettingsUseCase>(Lifetime.Singleton);
            builder.Register<UnitShopUseCase>(Lifetime.Singleton);

            builder.RegisterEntryPoint<DemoEntryPoint>();
        }
    }
}
