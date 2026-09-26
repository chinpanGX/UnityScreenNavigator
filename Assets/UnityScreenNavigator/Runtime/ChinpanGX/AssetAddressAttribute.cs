using System;

namespace UnityScreenNavigator
{
    /// <summary>
    /// PresenterにPrefabのAddressablesアドレスを紐づける。PushPageAsync/PushModalAsyncで
    /// 指定するPresenter型にこの属性を付けておくと、ScreenNavigatorがそこからロード先を解決する。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AssetAddressAttribute : Attribute
    {
        public string Address { get; }
        
        public AssetAddressAttribute(string address)
        {
            Address = address;
        }
    }
}