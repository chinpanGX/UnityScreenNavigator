using Cysharp.Threading.Tasks;

namespace UnityScreenNavigator
{
    /// <summary>
    /// Page/Modalの違いを意識せずライフサイクルを受け取りたいPresenterが任意で実装する。
    /// すべて既定実装があるので、必要なものだけ上書きすればよい。
    /// </summary>
    public interface ILifecycleHandler
    {
        UniTask InitializeAsync() => UniTask.CompletedTask;
        UniTask WillPushEnterAsync() => UniTask.CompletedTask;
        void DidPushEnter() { }
        UniTask WillPushExitAsync() => UniTask.CompletedTask;
        void DidPushExit() { }
        UniTask WillPopEnterAsync() => UniTask.CompletedTask;
        void DidPopEnter() { }
        UniTask WillPopExitAsync() => UniTask.CompletedTask;
        void DidPopExit() { }
        UniTask CleanupAsync() => UniTask.CompletedTask;
    }
}