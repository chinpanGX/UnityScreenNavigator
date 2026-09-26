using System.Threading;
using Cysharp.Threading.Tasks;

namespace UnityScreenNavigator
{
    /// <summary>
    /// Presenter起点でPage/Modalの積み上げを操作する窓口。
    /// Pushのたびに専用のPresenterを生成し、Popで閉じる。結果を返したい画面は
    /// <see cref="IPresenter.CompleteAsync"/>を上書きし、呼び出し側は<see cref="WaitForPopAsync{TResult}"/>で受け取る。
    /// </summary>
    public interface IScreenNavigator
    {
        /// <summary>Argsを必要としない画面をPushする。</summary>
        UniTask<TPresenter> PushPageAsync<TPresenter>(bool stack = true, bool playAnimation = true)
            where TPresenter : IPresenter;

        /// <summary>Argsを渡す画面をPushする。</summary>
        UniTask<TPresenter> PushPageAsync<TPresenter, TArgs>(TArgs args, bool stack = true, bool playAnimation = true)
            where TPresenter : IPresenter, IScreenWithArgs<TArgs>
            where TArgs : class;

        /// <summary>スタックの上からpopCount枚Popする。</summary>
        UniTask PopPageAsync(bool playAnimation = true, int popCount = 1);

        /// <summary>指定したPresenterの画面を、自分より上に積まれている画面ごとPopする。</summary>
        UniTask PopPageAsync(IPresenter presenter, bool playAnimation = true);

        UniTask<TPresenter> PushModalAsync<TPresenter>(bool playAnimation = true)
            where TPresenter : IPresenter;

        UniTask<TPresenter> PushModalAsync<TPresenter, TArgs>(TArgs args, bool playAnimation = true)
            where TPresenter : IPresenter, IScreenWithArgs<TArgs>
            where TArgs : class;

        UniTask PopModalAsync(bool playAnimation = true, int popCount = 1);

        UniTask PopModalAsync(IPresenter presenter, bool playAnimation = true);

        /// <summary>指定したPresenterがPopされ、CompleteAsyncの結果が返るまで待つ。</summary>
        UniTask<TResult> WaitForPopAsync<TResult>(IPresenter presenter, CancellationToken cancellation = default);
    }
}
