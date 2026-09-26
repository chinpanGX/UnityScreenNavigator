using UnityEngine;
using UnityScreenNavigator.Runtime.Core.Modal;

namespace UnityScreenNavigator
{
    /// <summary>
    /// Overlay Canvas(通信エラーダイアログ等、Page/Modalより前面に出す画面用)のコンテナ。
    /// 実体はUSNの<see cref="ModalContainer"/>そのもので、VContainerが通常の
    /// ModalContainer(Modal Canvas用)と型で区別できるようにするためのマーカー。
    /// </summary>
    [RequireComponent(typeof(ModalContainer))]
    public sealed class OverlayContainer : MonoBehaviour
    {
        private ModalContainer container;

        public ModalContainer Container => container ??= GetComponent<ModalContainer>();
    }
}
