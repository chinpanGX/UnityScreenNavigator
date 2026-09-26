using System.Threading;
using Cysharp.Threading.Tasks;

namespace UnityScreenNavigator
{
    /// <summary>
    /// シーンをまたぐ遷移だけを担う窓口。<see cref="IScreenNavigator"/>とは独立した責務。
    /// </summary>
    public interface ISceneNavigator
    {
        /// <summary>現在のシーンをアンロードし、指定したシーンをロードする。</summary>
        UniTask ChangeSceneAsync(string address, CancellationToken cancellation = default);
    }
}