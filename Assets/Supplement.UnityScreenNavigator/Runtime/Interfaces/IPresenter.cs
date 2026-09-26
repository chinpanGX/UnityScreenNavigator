using System;
using Cysharp.Threading.Tasks;

namespace UnityScreenNavigator
{
    /// <summary>
    /// PushPageAsync/PushModalAsyncで生成するPresenterが実装するマーカー。
    /// </summary>
    public interface IPresenter : ILifecycleHandler, IDisposable
    {
        /// <summary>Popが完了した時点でフレームワークが呼ぶ。結果を返したい画面はここを上書きする。</summary>
        UniTask<object> CompleteAsync() => UniTask.FromResult<object>(null);
    }
}
