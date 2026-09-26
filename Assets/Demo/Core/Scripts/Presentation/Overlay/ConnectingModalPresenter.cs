using Demo.Core.Scripts.Foundation.Common;
using UnityScreenNavigator;

namespace Demo.Core.Scripts.Presentation.Overlay
{
    /// <summary>
    /// 通信中インジケーター。呼び出し側が<see cref="IScreenNavigator.PushOverlayAsync{TPresenter}"/>で
    /// 開始し、戻り値のPresenterを<see cref="IScreenNavigator.PopOverlayAsync(IPresenter,bool)"/>に渡して
    /// 閉じる。表示内容が無いので、Viewへのデータ渡しやライフサイクルの上書きは不要。
    /// </summary>
    [AssetAddress(ResourceKey.Prefabs.ConnectingModal)]
    public sealed class ConnectingModalPresenter : IPresenter
    {
        public void Dispose()
        {
        }
    }
}
