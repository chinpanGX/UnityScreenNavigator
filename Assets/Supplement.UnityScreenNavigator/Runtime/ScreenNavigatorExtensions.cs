using Cysharp.Threading.Tasks;
using UnityScreenNavigator.Runtime.Core.Modal;
using UnityScreenNavigator.Runtime.Core.Page;

namespace UnityScreenNavigator
{
    public static class ScreenNavigatorExtensions
    {
        /// <summary>
        /// 積まれているModal/Pageを全部Popし、履歴を空にする。
        /// シーンをまたいで<see cref="IScreenNavigator"/>を使い回す構成で、シーン遷移前の後始末に使う。
        /// </summary>
        public static async UniTask ClearAsync(
            this IScreenNavigator navigator,
            PageContainer pageContainer,
            ModalContainer modalContainer,
            bool playAnimation = false)
        {
            if (modalContainer.Modals.Count > 0)
                await navigator.PopModalAsync(playAnimation, modalContainer.Modals.Count);
            if (pageContainer.Pages.Count > 0)
                await navigator.PopPageAsync(playAnimation, pageContainer.Pages.Count);
        }
    }
}
