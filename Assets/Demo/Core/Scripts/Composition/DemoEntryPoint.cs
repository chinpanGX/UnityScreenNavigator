using Cysharp.Threading.Tasks;
using Demo.Core.Scripts.Presentation.Top;
using UnityEngine;
using UnityScreenNavigator;
using VContainer.Unity;

namespace Demo.Core.Scripts.Composition
{
    public sealed class DemoEntryPoint : IStartable
    {
        private readonly IScreenNavigator screenNavigator;

        public DemoEntryPoint(IScreenNavigator screenNavigator)
        {
            this.screenNavigator = screenNavigator;
        }

        public void Start()
        {
            Application.targetFrameRate = 60;
            screenNavigator.PushPageAsync<TopPagePresenter>(playAnimation: false).Forget();
        }
    }
}
