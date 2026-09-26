using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace UnityScreenNavigator
{
    /// <summary>
    /// PageLifecycleAdapter/ModalLifecycleAdapterが共通で使う本体。
    /// USN側のライフサイクルイベント呼び出しを、Presenterが実装する ILifecycleHandler に橋渡しする。
    /// </summary>
    internal sealed class LifecycleAdapterCore
    {
        private readonly IPresenter presenter;
        private readonly ILifecycleHandler handler;
        private readonly Action<IPresenter> onCleanup;
        private readonly CancellationTokenSource exitCts = new();
        private readonly CancellationTokenSource disposeCts = new();

        public LifecycleAdapterCore(IPresenter presenter, Action<IPresenter> onCleanup)
        {
            this.presenter = presenter;
            handler = presenter as ILifecycleHandler;
            this.onCleanup = onCleanup;
        }

        public CancellationToken ExitCancellationToken => exitCts.Token;
        public CancellationToken DisposeCancellationToken => disposeCts.Token;

        public UniTask Initialize()
        {
            return handler?.InitializeAsync() ?? UniTask.CompletedTask;
        }

        public UniTask WillPushEnter()
        {
            return handler?.WillPushEnterAsync() ?? UniTask.CompletedTask;
        }

        public void DidPushEnter()
        {
            handler?.DidPushEnter();
        }

        public UniTask WillPushExit()
        {
            exitCts.Cancel();
            return handler?.WillPushExitAsync() ?? UniTask.CompletedTask;
        }

        public void DidPushExit()
        {
            handler?.DidPushExit();
        }

        public UniTask WillPopEnter()
        {
            return handler?.WillPopEnterAsync() ?? UniTask.CompletedTask;
        }

        public void DidPopEnter()
        {
            handler?.DidPopEnter();
        }

        public UniTask WillPopExit()
        {
            exitCts.Cancel();
            return handler?.WillPopExitAsync() ?? UniTask.CompletedTask;
        }

        public void DidPopExit()
        {
            handler?.DidPopExit();
        }

        public async UniTask Cleanup()
        {
            if (handler != null)
                await handler.CleanupAsync();
            disposeCts.Cancel();
            onCleanup(presenter);
        }
    }
}
