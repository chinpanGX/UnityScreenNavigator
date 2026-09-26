using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Loader.Abstractions;

namespace UnityScreenNavigator
{
    /// <summary>
    /// <see cref="ISceneLoader"/>を使ってシーンを切り替える<see cref="ISceneNavigator"/>の実装。
    /// Bootstrapシーンは常駐させたまま、コンテンツシーンだけを加算ロードで差し替える。
    /// </summary>
    public sealed class SceneNavigator : ISceneNavigator
    {
        private readonly ISceneLoader sceneLoader;
        private ISceneHandle currentSceneHandle;

        public SceneNavigator(ISceneLoader sceneLoader)
        {
            this.sceneLoader = sceneLoader;
        }

        public async UniTask ChangeSceneAsync(string address, CancellationToken cancellation = default)
        {
            var previousSceneHandle = currentSceneHandle;

            // 前のシーンが消える一瞬を作らないよう、新しいシーンを読み込んでから前のシーンを外す
            var sceneHandle = await sceneLoader.ChangeScene(address, additive: true, cancellation);
            sceneLoader.SetActiveScene(sceneHandle);
            currentSceneHandle = sceneHandle;

            previousSceneHandle?.Dispose();
        }
    }
}
