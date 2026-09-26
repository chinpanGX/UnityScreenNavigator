namespace UnityScreenNavigator
{
    /// <summary>
    /// Argsを受け取るPresenterが実装するマーカー。PushPageAsync/PushModalAsyncのTPresenter/TArgsの
    /// 組み合わせをコンパイル時に検査する役割も兼ねる。
    /// </summary>
    public interface IScreenWithArgs<TArgs> : IPresenter where TArgs : class
    {
    }
}
