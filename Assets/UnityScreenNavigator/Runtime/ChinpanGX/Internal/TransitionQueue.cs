using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UnityScreenNavigator
{
    /// <summary>
    /// 1つのコンテナへの遷移を、要求された順に1つずつ実行する。
    /// USNは遷移中のPush/Popを例外で拒否するため、別々の場所から来た遷移(開くアニメーション中の
    /// エラーダイアログ、閉じる途中の画面など)が重なると後の方が失敗する。これを順番待ちにする。
    /// </summary>
    internal sealed class TransitionQueue
    {
        private readonly Component container;
        private UniTask tail = UniTask.CompletedTask;

        public TransitionQueue(Component container)
        {
            this.container = container;
        }

        /// <summary>
        /// 前の遷移が終わってから(成功・失敗を問わず)transitionを実行する。
        /// 順番が来た時点でコンテナが破棄されていれば(シーンの切り替え等)、実行せずにキャンセル扱いにする。
        /// </summary>
        public async UniTask<T> EnqueueAsync<T>(Func<UniTask<T>> transition)
        {
            var previous = tail;
            var done = new UniTaskCompletionSource();
            tail = done.Task;
            try
            {
                await previous;
                if (container == null)
                    throw new OperationCanceledException(
                        "The screen container was destroyed before the transition started.");

                return await transition();
            }
            finally
            {
                done.TrySetResult();
            }
        }

        public UniTask EnqueueAsync(Func<UniTask> transition)
        {
            return EnqueueAsync(async () =>
            {
                await transition();
                return AsyncUnit.Default;
            });
        }
    }
}
