using UnityScreenNavigator.Runtime.Core.Modal;

namespace Demo.Core.Scripts.View.Overlay
{
    /// <summary>
    /// 通信中インジケーター。Push/Popされている間だけ表示される、Overlay用のModal。
    /// 表示・非表示の切り替えはUSN本体のPush/Pop(トランジション込み)に任せるため、
    /// 独自のShow/Hideは持たない。
    /// </summary>
    public sealed class ConnectingView : Modal
    {
    }
}
