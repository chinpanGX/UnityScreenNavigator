using System;
#if USN_USE_ASYNC_METHODS
using Cysharp.Threading.Tasks;

#else
using System.Collections;
#endif

namespace UnityScreenNavigator.Runtime.Core.Page
{
    public static class PageExtensions
    {
#if USN_USE_ASYNC_METHODS
        public static void AddLifecycleEvent(this Page self, Func<UniTask> initialize = null,
            Func<UniTask> onWillPushEnter = null, Action onDidPushEnter = null,
            Func<UniTask> onWillPushExit = null, Action onDidPushExit = null,
            Func<UniTask> onWillPopEnter = null, Action onDidPopEnter = null,
            Func<UniTask> onWillPopExit = null, Action onDidPopExit = null, Func<UniTask> onCleanup = null,
            int priority = 0)
#else
        public static void AddLifecycleEvent(this Page self, Func<IEnumerator> initialize = null,
            Func<IEnumerator> onWillPushEnter = null, Action onDidPushEnter = null,
            Func<IEnumerator> onWillPushExit = null, Action onDidPushExit = null,
            Func<IEnumerator> onWillPopEnter = null, Action onDidPopEnter = null,
            Func<IEnumerator> onWillPopExit = null, Action onDidPopExit = null, Func<IEnumerator> onCleanup = null,
            int priority = 0)
#endif
        {
            var lifecycleEvent = new AnonymousPageLifecycleEvent(initialize, onWillPushEnter, onDidPushEnter,
                onWillPushExit, onDidPushExit, onWillPopEnter, onDidPopEnter, onWillPopExit, onDidPopExit,
                onCleanup);
            self.AddLifecycleEvent(lifecycleEvent, priority);
        }
    }
}
