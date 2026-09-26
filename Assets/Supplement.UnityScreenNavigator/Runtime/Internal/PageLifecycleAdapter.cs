using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityScreenNavigator.Runtime.Core.Page;

namespace UnityScreenNavigator
{
    /// <summary>
    /// USNのPageライフサイクルイベントを<see cref="ILifecycleHandler"/>に橋渡しするアダプタ。
    /// </summary>
    internal sealed class PageLifecycleAdapter : IPageLifecycleEvent
    {
        private readonly LifecycleAdapterCore core;

        public PageLifecycleAdapter(IPresenter presenter, Action<IPresenter> onCleanup)
        {
            core = new LifecycleAdapterCore(presenter, onCleanup);
        }

        public CancellationToken ExitCancellationToken => core.ExitCancellationToken;
        public CancellationToken DisposeCancellationToken => core.DisposeCancellationToken;

        public UniTask Initialize() => core.Initialize();
        public UniTask WillPushEnter() => core.WillPushEnter();
        public void DidPushEnter() => core.DidPushEnter();
        public UniTask WillPushExit() => core.WillPushExit();
        public void DidPushExit() => core.DidPushExit();
        public UniTask WillPopEnter() => core.WillPopEnter();
        public void DidPopEnter() => core.DidPopEnter();
        public UniTask WillPopExit() => core.WillPopExit();
        public void DidPopExit() => core.DidPopExit();
        public UniTask Cleanup() => core.Cleanup();
    }
}
