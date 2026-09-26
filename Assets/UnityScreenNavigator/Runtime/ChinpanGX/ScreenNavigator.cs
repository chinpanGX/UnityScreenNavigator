using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityScreenNavigator.Runtime.Core.Modal;
using UnityScreenNavigator.Runtime.Core.Page;
using VContainer;

namespace UnityScreenNavigator
{
    /// <summary>
    /// Presenter起点でPage/Modalを操作する<see cref="IScreenNavigator"/>の実装。
    /// Pushのたびにヘッドレスな子スコープを作ってPresenterを解決し、Presenterと画面IDの対応を管理する。
    /// </summary>
    public sealed class ScreenNavigator : IScreenNavigator, IDisposable
    {
        private sealed class Entry
        {
            public IPresenter Presenter;
            public string ScreenId;
            public IObjectResolver Scope;
            public UniTaskCompletionSource<object> Completion;
            public bool WaitStarted;
        }

        private readonly PageContainer pageContainer;
        private readonly ModalContainer modalContainer;
        private readonly ModalContainer overlayContainer;
        private readonly IObjectResolver resolver;
        private readonly Dictionary<IPresenter, Entry> entries = new();

        public ScreenNavigator(PageContainer pageContainer, ModalContainer modalContainer,
            OverlayContainer overlayContainer, IObjectResolver resolver)
        {
            this.pageContainer = pageContainer;
            this.modalContainer = modalContainer;
            this.overlayContainer = overlayContainer.Container;
            this.resolver = resolver;
        }

        public UniTask<TPresenter> PushPageAsync<TPresenter>(bool stack = true, bool playAnimation = true)
            where TPresenter : IPresenter
        {
            return PushPageInternalAsync<TPresenter>(null, playAnimation, stack);
        }

        public UniTask<TPresenter> PushPageAsync<TPresenter, TArgs>(TArgs args, bool stack = true,
            bool playAnimation = true)
            where TPresenter : IPresenter, IScreenWithArgs<TArgs>
            where TArgs : class
        {
            return PushPageInternalAsync<TPresenter>(args, playAnimation, stack);
        }

        public async UniTask PopPageAsync(bool playAnimation = true, int popCount = 1)
        {
            await pageContainer.Pop(playAnimation, popCount).Task.AsUniTask();
        }

        public async UniTask PopPageAsync(IPresenter presenter, bool playAnimation = true)
        {
            var entry = GetEntry(presenter);
            var orderedIds = pageContainer.OrderedPagesIds;
            var popCount = orderedIds.Count - IndexOf(orderedIds, entry.ScreenId);
            await pageContainer.Pop(playAnimation, popCount).Task.AsUniTask();
        }

        public UniTask<TPresenter> PushModalAsync<TPresenter>(bool playAnimation = true)
            where TPresenter : IPresenter
        {
            return PushModalLikeInternalAsync<TPresenter>(modalContainer, null, playAnimation);
        }

        public UniTask<TPresenter> PushModalAsync<TPresenter, TArgs>(TArgs args, bool playAnimation = true)
            where TPresenter : IPresenter, IScreenWithArgs<TArgs>
            where TArgs : class
        {
            return PushModalLikeInternalAsync<TPresenter>(modalContainer, args, playAnimation);
        }

        public UniTask PopModalAsync(bool playAnimation = true, int popCount = 1)
        {
            return PopModalLikeAsync(modalContainer, playAnimation, popCount);
        }

        public UniTask PopModalAsync(IPresenter presenter, bool playAnimation = true)
        {
            return PopModalLikeAsync(modalContainer, presenter, playAnimation);
        }

        public UniTask<TPresenter> PushOverlayAsync<TPresenter>(bool playAnimation = true)
            where TPresenter : IPresenter
        {
            return PushModalLikeInternalAsync<TPresenter>(overlayContainer, null, playAnimation);
        }

        public UniTask<TPresenter> PushOverlayAsync<TPresenter, TArgs>(TArgs args, bool playAnimation = true)
            where TPresenter : IPresenter, IScreenWithArgs<TArgs>
            where TArgs : class
        {
            return PushModalLikeInternalAsync<TPresenter>(overlayContainer, args, playAnimation);
        }

        public UniTask PopOverlayAsync(bool playAnimation = true, int popCount = 1)
        {
            return PopModalLikeAsync(overlayContainer, playAnimation, popCount);
        }

        public UniTask PopOverlayAsync(IPresenter presenter, bool playAnimation = true)
        {
            return PopModalLikeAsync(overlayContainer, presenter, playAnimation);
        }

        public async UniTask<TResult> WaitForPopAsync<TResult>(IPresenter presenter,
            CancellationToken cancellation = default)
        {
            var entry = GetEntry(presenter);
            if (entry.WaitStarted)
                throw new InvalidOperationException(
                    $"WaitForPopAsync has already been called for this {presenter.GetType()}.");
            entry.WaitStarted = true;

            var result = await entry.Completion.Task.AttachExternalCancellation(cancellation);
            return (TResult)result;
        }

        public void Dispose()
        {
            foreach (var entry in entries.Values)
                CompleteEntryAsync(entry).Forget();
            entries.Clear();
        }

        private async UniTask<TPresenter> PushPageInternalAsync<TPresenter>(object args, bool playAnimation,
            bool stack)
            where TPresenter : IPresenter
        {
            var resourceKey = GetResourceKey<TPresenter>();
            TPresenter presenter = default;
            var handle = pageContainer.Push(resourceKey, playAnimation, stack: stack, onLoad: loaded =>
            {
                presenter = CreateEntry<TPresenter>(args, loaded.page, loaded.pageId);
                loaded.page.AddLifecycleEvent(new PageLifecycleAdapter(presenter, OnScreenCleanup));
            });
            await handle.Task.AsUniTask();
            return presenter;
        }

        private async UniTask<TPresenter> PushModalLikeInternalAsync<TPresenter>(ModalContainer container,
            object args, bool playAnimation)
            where TPresenter : IPresenter
        {
            var resourceKey = GetResourceKey<TPresenter>();
            TPresenter presenter = default;
            var handle = container.Push(resourceKey, playAnimation, onLoad: loaded =>
            {
                presenter = CreateEntry<TPresenter>(args, loaded.modal, loaded.modalId);
                loaded.modal.AddLifecycleEvent(new ModalLifecycleAdapter(presenter, OnScreenCleanup));
            });
            await handle.Task.AsUniTask();
            return presenter;
        }

        private static async UniTask PopModalLikeAsync(ModalContainer container, bool playAnimation, int popCount)
        {
            await container.Pop(playAnimation, popCount).Task.AsUniTask();
        }

        private async UniTask PopModalLikeAsync(ModalContainer container, IPresenter presenter, bool playAnimation)
        {
            var entry = GetEntry(presenter);
            var orderedIds = container.OrderedModalIds;
            var popCount = orderedIds.Count - IndexOf(orderedIds, entry.ScreenId);
            await container.Pop(playAnimation, popCount).Task.AsUniTask();
        }

        private TPresenter CreateEntry<TPresenter>(object args, object view, string screenId)
            where TPresenter : IPresenter
        {
            var scope = resolver.CreateScope(builder =>
            {
                if (args != null)
                    builder.RegisterInstance(args, args.GetType());
                builder.RegisterInstance(view, view.GetType());
                builder.Register<TPresenter>(Lifetime.Transient);
            });
            var presenter = scope.Resolve<TPresenter>();

            entries[presenter] = new Entry
            {
                Presenter = presenter,
                ScreenId = screenId,
                Scope = scope,
                Completion = new UniTaskCompletionSource<object>()
            };
            return presenter;
        }

        private void OnScreenCleanup(IPresenter presenter)
        {
            if (!entries.TryGetValue(presenter, out var entry))
                return;
            entries.Remove(presenter);
            CompleteEntryAsync(entry).Forget();
        }

        private static async UniTaskVoid CompleteEntryAsync(Entry entry)
        {
            object result = null;
            try
            {
                result = await entry.Presenter.CompleteAsync();
            }
            finally
            {
                entry.Completion.TrySetResult(result);
                entry.Scope.Dispose();
            }
        }

        private Entry GetEntry(IPresenter presenter)
        {
            if (!entries.TryGetValue(presenter, out var entry))
                throw new InvalidOperationException(
                    $"{presenter.GetType()} is not managed by this {nameof(ScreenNavigator)}.");
            return entry;
        }

        private static int IndexOf(IReadOnlyList<string> ids, string id)
        {
            for (var i = 0; i < ids.Count; i++)
                if (ids[i] == id)
                    return i;
            throw new InvalidOperationException($"Screen id '{id}' is not found in the current stack.");
        }

        private static string GetResourceKey<TPresenter>()
        {
            var attribute = typeof(TPresenter).GetCustomAttribute<AssetAddressAttribute>();
            if (attribute == null)
                throw new InvalidOperationException(
                    $"{typeof(TPresenter)} must have {nameof(AssetAddressAttribute)}.");
            return attribute.Address;
        }
    }
}
