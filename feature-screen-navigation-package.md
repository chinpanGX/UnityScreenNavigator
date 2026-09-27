# 画面遷移パッケージ 設計書

USN(UnityScreenNavigator)を GitHub 上でフォークしつつ、Presenter起点のDIを提供する新しいパッケージ
`Supplement.UnityScreenNavigator`を、**フォークと同じリポジトリの中に**別パッケージ・別アセンブリとして
用意する設計(2章)。

**Page/ModalのGameObjectプール化は見送る(6-7章は現時点では実施しない)。** 当初はプール化も主目的の
1つだったが、プール化以外の機能(Presenter起点のDI・シーン遷移)はUSN本体の`PageContainer`/
`ModalContainer`のpublic API(`Push`/`Pop`の`onLoad`コールバック、`AddLifecycleEvent`、`Of`/`Find`等)
だけで実現でき、USN本体のソースを一切書き換える必要が無いことが分かったため。プール化だけが
「USN内部に直接手を入れる必要がある変更」だったので、プール化を見送るなら別パッケージに分けても
循環依存にならない(以前の版で「別パッケージに分けると循環依存になる」としていたのは、プール化を
やる前提だったため)。プール化が必要になったら、その時点で`Assets/UnityScreenNavigator`(フォーク)側を
直接拡張すればよい(パッケージを増やしてまで分離を保つ必要は無い)。

参考にした実装(詳細な比較検討は会話ログ参照。ここには結論だけ書く):

- UnityScreenNavigator(USN)1.8.0: <https://github.com/Haruma-K/UnityScreenNavigator>
- DevelopmentBooster の `ScreenService`(`Assets/InternalPackages/ScreenService`。View のプール、
  ヘッドレスな DI スコープ、`ScreenSystem` サブフォルダの `LifecyclePageBase`/`LifecycleModalBase`/
  `AssetNameAttribute`)

---

## 1. 目的

USN自体は使いやすいので、Presenter起点のDIを足してさらに使いやすくする。具体的には:

- PresenterをUSNのコンテナ・View prefabから疎結合にし、Pushのたびにヘッドレスな DI スコープで作り直す
- USNの豊富なライフサイクルイベント・Sheet機能・Timeline非依存のアニメーション機構はそのまま活かす
- USN本体(`Assets/UnityScreenNavigator`)のソースは変更しない。新機能はすべて別パッケージ
  `Supplement.UnityScreenNavigator`の新規ファイルとして追加する(2章)。USNをforkしているのは、
  upstreamの更新を追跡しつつ、将来ソースを直接書き換える必要が出た場合にすぐ対応できるようにする
  ためであり、現時点でforkの差分は無い

---

## 2. パッケージ構成

**同じフォークリポジトリの中に、2つのUnityパッケージを置く**(開発は1つのUnityプロジェクトで完結させたい
という判断。1章参照):

- **`UnityScreenNavigator`(既存。`Assets/UnityScreenNavigator`)**: USN本家のフォーク。現時点では
  ソース変更なし(1章参照)。依存はUnity本体のみで、VContainer/UniTask/Supplementのいずれにも依存しない
- **`Supplement.UnityScreenNavigator`(新規。`Assets/Supplement.UnityScreenNavigator`)**: Presenter起点の
  DI・シーン遷移まわりを提供する。`UnityScreenNavigator`(上記)・VContainer・UniTask・`Supplement.Loader`
  (`com.chinpangx.supplement`。`ISceneLoader`、4章参照)に依存する(実装済み)。`Supplement.Unity`
  (`IAsyncRenderable<T>`、5.2参照)は実装が5.2に進んだ時点で追加する。非同期処理はすべてUniTaskにする

依存の向きは`Supplement.UnityScreenNavigator → UnityScreenNavigator`の一方向のみ。逆方向の参照は無い
(USN本体のソースを書き換えないため、循環の心配が無い)。

- USNは GitHub 上で正式に **Fork** し、upstream(`Haruma-K/UnityScreenNavigator`)を git remote として
  追跡する。`git fetch upstream && git merge upstream/master` で本家の更新を取り込める状態にする
- Atlas側の `manifest.json` は、同じフォークリポジトリを指す2つのgit URL(`?path=Assets/UnityScreenNavigator`
  と`?path=Assets/Supplement.UnityScreenNavigator`)を両方登録する(UPMのgit依存はpackage.json経由の
  自動解決に頼らず、消費側のmanifest.jsonに明示的に列挙する)
- アプリの要件に依存しない。見た目・アニメーション・表示データの内容はアプリが用意する Prefab と `Args` が
  決め、パッケージは呼び出しの順序と制御だけを持つ

### 2.1 フォルダ構成(`Supplement.UnityScreenNavigator`)

実装済み・実装予定のファイルは以下の通り(実装済みのものはそのままの配置に合わせて記載):

```
Assets/
  UnityScreenNavigator/              ← 既存(フォーク本体。変更なし)
    Runtime/
      Core/ ...
      Foundation/ ...
  Supplement.UnityScreenNavigator/   ← 新規パッケージ
    package.json                     (実装済み)
    Runtime/
      Supplement.UnityScreenNavigator.asmdef   (実装済み。UnityScreenNavigator/VContainer/UniTaskを参照)
      AssetAddressAttribute.cs       (3.2。実装済み)
      Interfaces/
        IPresenter.cs                (3.3。実装済み)
        IScreenWithArgs.cs           (3.3。実装済み)
        ILifecycleHandler.cs         (5.1。実装済み)
        IScreenNavigator.cs          (3章。実装済み)
        ISceneNavigator.cs           (4章。実装済み。`IScreenNavigator`とは別ファイルに分離済み)
      SceneNavigator/                (4章。未実装。フォルダのみ用意済み)
      (ScreenNavigator.cs/ScreenNavigatorExtensions.cs(3.5)/PageLifecycleAdapter.cs/
       ModalLifecycleAdapter.cs は未実装。配置場所は実装時に決める)
```

名前空間は`UnityScreenNavigator`(実装済みファイルの実態に合わせる。本家の`UnityScreenNavigator.Runtime.Core.*`
とは重ならない)。

---

## 3. IScreenNavigator

```csharp
public interface IScreenNavigator
{
    // Argsが不要な画面向け
    UniTask<TPresenter> PushPageAsync<TPresenter>(
        bool playAnimation = true, bool stack = true)
        where TPresenter : IPresenter;
    // Argsを渡す画面向け
    UniTask<TPresenter> PushPageAsync<TPresenter, TArgs>(TArgs args,
        bool playAnimation = true, bool stack = true)
        where TPresenter : IPresenter, IScreenWithArgs<TArgs>
        where TArgs : class;
    UniTask PopPageAsync(bool playAnimation = true, int popCount = 1);
    // 自分自身(this)を指定して、自分の位置から閉じる(3.4参照)
    UniTask PopPageAsync(IPresenter presenter, bool playAnimation = true);

    UniTask<TPresenter> PushModalAsync<TPresenter>(
        bool playAnimation = true)
        where TPresenter : IPresenter;
    UniTask<TPresenter> PushModalAsync<TPresenter, TArgs>(TArgs args,
        bool playAnimation = true)
        where TPresenter : IPresenter, IScreenWithArgs<TArgs>
        where TArgs : class;
    UniTask PopModalAsync(bool playAnimation = true, int popCount = 1);
    UniTask PopModalAsync(IPresenter presenter, bool playAnimation = true);

    // 通信エラーダイアログ等、Modalよりさらに前面(Overlay Canvas)に出す画面(3.6参照)
    UniTask<TPresenter> PushOverlayAsync<TPresenter>(
        bool playAnimation = true)
        where TPresenter : IPresenter;
    UniTask<TPresenter> PushOverlayAsync<TPresenter, TArgs>(TArgs args,
        bool playAnimation = true)
        where TPresenter : IPresenter, IScreenWithArgs<TArgs>
        where TArgs : class;
    UniTask PopOverlayAsync(bool playAnimation = true, int popCount = 1);
    UniTask PopOverlayAsync(IPresenter presenter, bool playAnimation = true);

    // Pop完了(=IPresenter.CompleteAsyncが呼ばれた)を待って結果を受け取る(3.4参照)
    UniTask<TResult> WaitForPopAsync<TResult>(IPresenter presenter, CancellationToken cancellation = default);
}
```

`ClearAsync`(3.1のパターンB向けヘルパー)は`IScreenNavigator`のメンバーにはしない。既存メンバー
(`PopPageAsync`/`PopModalAsync`)だけで実装できるため、拡張メソッドとして提供する(3.5)。

- `ScreenNavigator` の登録スコープ(シーンごとの`Scoped`か、アプリ全体で1つのRoot常駐(`Singleton`)か)は
  パッケージ側で固定しない。アプリの構成に合わせてどちらも選べるようにする(詳細は3.1)
- `TPresenter` はピュアC#のPresenter(5章)。Viewの具体的な型はジェネリック引数に含めない(3.2参照)。
  すべてのPresenterは空のマーカー `IPresenter` を実装する(3.3参照)
- 結果を返したい画面は `IPresenter.CompleteAsync()` を上書きする。`IScreenWithResult<TResult>` のような
  専用インターフェースは作らない(3.4参照)
- シーンをまたぐ遷移(`ChangeSceneAsync`)は`IScreenNavigator`とは別インターフェースにするが、
  このパッケージ自身が提供する。Atlas既存の`Atlas.Navigation.ISceneNavigator`は、このパッケージ内の
  型に置き換える(4章)

### 3.1 コンテナ登録はアプリの構成に委ねる

パッケージは`IScreenNavigator`/`PageContainer`/`ModalContainer`の型だけを提供し、それらを**どの
`LifetimeScope`に登録するか**は規定しない。アプリによって画面遷移の粒度(シーンごとに区切りたいか、
シーンをまたいで1つのスタックを保ちたいか)が異なるため、パッケージ側で決め打ちにしない。

**パターンA: シーンの`LifetimeScope`にScoped登録**(Atlasの`HomeLifetimeScope`(既存コード)が既に
やっている方法)

```csharp
// シーンに1つ置く LifetimeScope(既存の HomeLifetimeScope/BattleLifetimeScope と同じ形)
protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterComponent(pageContainer);
    builder.RegisterComponent(modalContainer);
    builder.Register<IScreenNavigator, ScreenNavigator>(Lifetime.Scoped);
}
```

この方式には`AttachScene`の可変状態も、「切り替え中に呼んだら例外」という特別扱いも要らない。シーンが
Unloadされて`LifetimeScope`がDisposeされれば、それに紐づく`ScreenNavigator`・Push中だったPresenterの
ヘッドレススコープもすべて一緒にDisposeされる。

これはUSN自体の設計とも合っている。USNの`PageContainer`/`ModalContainer`は`OnDestroy`で「スタック上の
Page/Modalを全部`Destroy`し、`AssetLoader.Release`し、静的レジストリ(`InstanceCacheByName`等)から自分を
外す」という後始末を自前で持っている(コンテナ自体が破棄されること自体は最初から想定されている)。逆に、
シーンをまたいで永続させなければならないのはUSN内の`UpdateDispatcher`/`CoroutineManager`(アニメーションの
Tick用、`DontDestroyOnLoad`)だけで、Page/Modalのスタックそのものを永続化する前提はUSNには無い。
そのため「コンテナ(と、それをラップする`ScreenNavigator`)はシーンの寿命で作り直す」というパターンAは
USNの設計から外れるものではない。

**パターンB: Rootの`LifetimeScope`にSingleton登録**(シーンをまたいで1つのスタックを保ちたいアプリ向け)

```csharp
protected override void Configure(IContainerBuilder builder)   // Root LifetimeScope
{
    builder.RegisterComponent(pageContainer);   // DontDestroyOnLoadに置く前提
    builder.RegisterComponent(modalContainer);
    builder.Register<IScreenNavigator, ScreenNavigator>(Lifetime.Singleton);
}
```

この場合、コンテナ・`ScreenNavigator`はシーン遷移で破棄されないため、パターンAでは`LifetimeScope`の
Disposeが自動でやっていた「前のシーンのPage/Modalを片付ける」処理を、アプリ側で明示的に行う必要がある。
これをアプリ側で1から書かせるのではなく、パッケージが`ClearAsync`(3.5、`IScreenNavigator`の拡張メソッド)
というヘルパーとして用意する。`AttachScene`/`DetachScene`のような「コンテナの生成・破棄そのもの」に関わる
専用APIは引き続き用意しない(コンテナ自体はRootに1つのまま生き続けるため、その必要が無い)。

パターンBは`ClearAsync`で毎回空にする使い方だけでなく、**あえてPopせずスタックを残したままシーンを
またぐ**使い方もできる(例: シーンAでPushしたPage/Modalを、シーンBを経由して戻ってきたときに
再構築せず、そのまま表示を再開する)。この場合はPresenterやViewを再生成するコストが無くなる分、
戻ってくる速度が上がる。ただし「どのタイミングでClearし、どのタイミングで残すか」はアプリ側の
判断・管轄であり、パッケージはClearAsyncを呼ぶかどうかを強制しない(2章の「アプリの要件に依存しない」
方針どおり)。

どちらのパターンを選ぶかはアプリのDI構成(`LifetimeScope`の置き場所)だけで決まり、`IScreenNavigator`の
インターフェース自体に差は無い。

### 3.2 resourceKeyの解決(`AssetAddressAttribute`)

`resourceKey`(USN側の呼び方。Prefabのロード・Poolからの取得に使う)は**Viewがまだ存在しない時点**で
必要になる。そのため、呼び出し側が指定する`TPresenter`(その時点で唯一分かっている型)に属性を貼る。
Viewを見てからresourceKeyを知る、という順序は循環するため成立しない。

属性の定義(実装済み。`Assets/Supplement.UnityScreenNavigator/Runtime/AssetAddressAttribute.cs`):

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class AssetAddressAttribute : Attribute
{
    public string Address { get; }

    public AssetAddressAttribute(string address)
    {
        Address = address;
    }
}
```

Presenterでの使用例(このPresenterの完全な例(コンストラクタ・各インターフェースの実装)はこの1つだけで、
5章でも同じクラスを参照する):

```csharp
// Atlasの自動生成AddressDefinition(public const string)はコンパイル時定数なのでそのまま渡せる
[AssetAddress(AddressDefinition.BattlePage)]
public sealed class BattlePagePresenter : IScreenWithArgs<BattlePageArgs>, ILifecycleHandler
{
    private readonly BattlePageView view;
    private readonly BattlePageArgs args;
    private readonly IScreenNavigator screenNavigator;
    private BattleResult? result;

    public BattlePagePresenter(BattlePageView view, BattlePageArgs args, IScreenNavigator screenNavigator)
    {
        this.view = view;
        this.args = args;
        this.screenNavigator = screenNavigator;
    }

    // ボタン押下等で呼ぶ。結果を控えて、自分自身(this)を渡して閉じる(3.4)
    private UniTask OnConfirm(BattleResult chosen)
    {
        result = chosen;
        return screenNavigator.PopPageAsync(this);
    }

    // フレームワークがPop完了時に呼ぶ(IPresenterの既定実装を上書き。3.4)
    public UniTask<object> CompleteAsync() => UniTask.FromResult<object>(result);

    // Openアニメーションより前にViewへ描画する(5.2)
    public async UniTask InitializeAsync()
    {
        var dto = new BattlePageRenderDto(args.MatchId, ...);
        await view.RenderAsync(dto);
    }
}
```

属性が無ければ、エラーにする。


### 3.3 型の結び付け

```csharp
public interface IPresenter
{
    // フレームワークがPop完了時に呼ぶ。既定は「結果なし」。結果を返したい画面はこれを上書きする(3.4)
    UniTask<object> CompleteAsync() => UniTask.FromResult<object>(null);
}

public interface IScreenWithArgs<TArgs> : IPresenter where TArgs : class { }
```

`IPresenter`はすべてのPresenterが実装する(Push制約。3.2参照)。`IScreenWithArgs<TArgs>`は`IPresenter`を
継承しているため、Argsを受け取るPresenterは`IPresenter`を別途書く必要はない。`TPresenter` と `TArgs`
の組み合わせをコンパイル時に検査する役割も兼ねる。

結果を返す仕組みは専用インターフェース(`IScreenWithResult<TResult>`)を作らず、`IPresenter.CompleteAsync()`
の上書きと`IScreenNavigator.WaitForPopAsync<TResult>`(3.4)だけで完結させる。
                                                                                                                                                                                                
具象の `TArgs` クラスは「機能名+Page/Modal+Args」の命名にする(例:
`BattlePageArgs`、`SwitchSelectModalArgs`)。Atlasでは「Battle」のように画面と別の意味(対戦ロジック)
でも使われる語があるため、単に `BattleArgs` にすると何のArgsか曖昧になる。`Page`/`Modal` を挟むことで、
画面遷移用のデータだと明確にし、`BattlePagePresenter` 等のPresenter名とも対応させる。

### 3.4 Presenterが自分自身を閉じる・結果を返す(ScreenServiceの`WaitForPop`方式)

USNのコンテナはView単位でスタックを管理しており、Presenterという概念を知らない。「自分を閉じる」ためには、
閉じる時点で自分より上に別の画面が積まれている可能性があるため、単純にスタックの上から1枚Popするのでは
不十分(自分ではない画面を閉じてしまう)。

これを解決するのが `PopPageAsync(IPresenter presenter, ...)`/`PopModalAsync(IPresenter presenter, ...)`
で、Presenterは`this`を渡すだけでよい。内部では、Push時(5.1)に生成する`PageLifecycleAdapter`/
`ModalLifecycleAdapter`がPresenterとViewを1組で保持しているため、`ScreenNavigator`はこの対応を使って
Presenterから Viewの位置を逆引きし、自分より上を含めてPopする(旧`ResultPage`の`CountFromTop`と同じ考え方)。

結果を返す流れは`ScreenService`の`WaitForPop<T>`と同じ方式にする(3.3):

1. Pushした側は `screenNavigator.WaitForPopAsync<BattleResult>(presenter, ct)` で結果を待つ
2. Presenterは`OnConfirm`のような内部処理で結果を控え、`screenNavigator.PopPageAsync(this)` で閉じる
   (`PopPageAsync`自体には結果を渡さない)
3. **`ScreenNavigator`はPopが実際に完了した時点で、対象Presenterの`IPresenter.CompleteAsync()`を呼び、
   その戻り値で`WaitForPopAsync`の待機を解決する**。USN本体のソースは変更しないため、この呼び出しは
   USN側のフックではなく、`ScreenNavigator`自身の実装で担う:
   - **明示的なPop呼び出し・自己Pop**: `PageContainer.Pop(...)`/`ModalContainer.Pop(...)`が返す
     `AsyncProcessHandle`をUniTaskに変換して`await`し、完了した時点で、Pushの内部対応表
     (上記のPresenter↔View対応)から対象のPresenterを引いて`CompleteAsync()`を呼ぶ
   - **シーンの`LifetimeScope`がDisposeされたことによる破棄**(3.1のパターンA。パターンBは
     `ClearAsync`(3.5)を明示的に呼ぶ運用になる): `ScreenNavigator`自体が`IDisposable`を実装し、
     `Dispose()`(VContainerがScopeのDispose時に呼ぶ)で、その時点で内部対応表に残っている
     Presenterすべてに対して`CompleteAsync()`を呼ぶ。`PageContainer.OnDestroy`(USN本体側の後始末)に
     フックする必要はない

   いずれの経路でも`ScreenNavigator`自身のコードで完結するため、`WaitForPopAsync`は
   `CompleteAsync`を経由しない閉じ方でも必ずいずれ解決する(旧`ScreenResultCompletion`が
   `destroyCancellationToken`で保証していたのと同じ効果を、USN本体を変更せずに得られる)

これにより`IScreenSelfCloser`や`IScreenWithResult<TResult>`のような専用インターフェースは不要になる。
`IScreenNavigator`という既存の窓口に、対象を指定できるオーバーロードを足すだけで済む。

**この方式のトレードオフと未規定の細部:**

- **型安全性が下がる**: `IPresenter.CompleteAsync()`は`UniTask<object>`を返すため、`WaitForPopAsync<TResult>`
  の`TResult`とPresenterが実際に返す型が食い違ってもコンパイルは通り、実行時に`InvalidCastException`になる。
  5.2で解決した「型の食い違いに気づけない」問題を部分的に再導入するが、`ScreenService`も同じ割り切りを
  しているため許容する
- **同じPresenterに対する`WaitForPopAsync`の二重呼び出し**は例外にする(`ScreenService`も同様。1つの
  Presenterにつき1回しか待てない)
- **シーン切り替え後に自己Popしようとするケース**は、3.1のパターンA(シーンの`LifetimeScope`に
  Scoped登録)であれば、シーンがUnloadされて`LifetimeScope`がDisposeされた時点で`ScreenNavigator`・
  Push中のPresenterのヘッドレススコープも道連れにDisposeされ、大半は自然に解消する。パターンB
  (Root常駐)ではこの自動解消は起きないため、シーン遷移前に`ClearAsync`(3.5)を呼んでスタックを
  整理する。いずれのパターンでも、非同期処理が`ExitCancellationToken`/`DisposeCancellationToken`(5.1)を
  無視して処理を続け、Dispose後(またはClear後)に`PopPageAsync(this)`を呼んでしまうケースはアプリ側の
  バグとして扱う(パッケージ側で無害化する特別扱いは用意しない)

### 3.5 `ClearAsync`(パターンB向けの一括クリア。拡張メソッド)

パターンA(3.1)では、シーンの`LifetimeScope`がDisposeされることで、積まれているPage/Modalの後始末が
自動的に行われる。パターンB(Root常駐)ではコンテナが破棄されないため、同じ後始末を明示的に呼べる形で
パッケージが提供する。

**`IScreenNavigator`のメンバーにはしない。** Popする枚数さえ分かれば既存の`PopPageAsync`/`PopModalAsync`
だけで実装できるため、`ScreenNavigator`自体に手を入れる新規メンバーではなく、`IScreenNavigator`に対する
**拡張メソッド**として提供する。積まれている枚数は`PageContainer`/`ModalContainer`(USN本体が元から
持つ`Of(transform)`/`Find(name)`でどこからでも取得できる。`Runtime/Core/Page/PageContainer.cs`・
`Core/Modal/ModalContainer.cs`参照)の`Pages`/`Modals`(`IReadOnlyDictionary`)からそのまま数えられる:

```csharp
public static class ScreenNavigatorExtensions
{
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
```

- Modal→Pageの順で、積まれているものを全部Popする(Modalが上に乗っている状態でPageだけを消すと
  表示が壊れるため、常にModalを先に片付ける)
- `pageContainer`/`modalContainer`の引数は、呼び出し側が`PageContainer.Find("...")`/`.Of(transform)`
  で用意する(コンテナを取得する方法はUSN本体のドキュメント参照)。`ScreenNavigator`内部の状態を
  介さずに済むため、`IScreenNavigator`自体はViewの具体的な型を知らないという3章の方針とも矛盾しない
- 各Presenterの`IPresenter.CompleteAsync()`は、内部で呼んでいる`PopModalAsync`/`PopPageAsync`と
  同じタイミングで呼ばれる。したがって`WaitForPopAsync`で待っている側があれば、理由(自己Pop/`ClearAsync`)
  を問わず必ず解決される(3.4と同じ保証。`ClearAsync`は既存メソッドを呼んでいるだけなので、この保証も
  自動的に付いてくる)
- 空いたViewは通常のPopと同じくUSN本体が`Destroy`する(プール化は見送っているため。1章参照)。
  `PopModalAsync`/`PopPageAsync`をそのまま呼んでいるだけなので、挙動は通常のPopと完全に同じ
- `playAnimation`の既定は`false`(シーン自体が切り替わる=画面全体が入れ替わる場面での使用を想定するため、
  1枚ずつのPop演出は基本的に不要)。個別の遷移演出を見せたいケースのために引数は残す
- 呼び出しはアプリ側の責務(4章の`ISceneNavigator`が自動で呼ぶことはしない)。`ISceneNavigator`は
  シーンのLoad/Unloadだけを知っており、その先のPage/Modal構成(パターンA/Bのどちらを使っているか)を
  知らないため、`ChangeSceneAsync`を呼ぶ前にアプリが`screenNavigator.ClearAsync(pageContainer, modalContainer)`
  を呼ぶ、という順序で組み合わせる

### 3.6 Overlay(通信エラーダイアログ等、Page/Modalより前面に出す画面)

Page/Modalとは別に、常に最前面(Modal表示中でも隠れない)に出したい画面(通信エラーダイアログ等)向けに
`PushOverlayAsync`/`PopOverlayAsync`を用意する。**Modalが表示中でもさらに前面に出したい**という要件が
起点なので、Modalのスタックとは別の、3本目のスタックとして扱う。

実装はPage/Modalと同じくUSN本体のコンテナをそのまま使う。USNには「Overlay」というコンテナ種別は無いが、
`ModalContainer`自体がシーンに複数個置いて名前で区別できる作りになっている(`ModalContainer.Find(name)`)
ため、**新しいコンテナクラスを自作せず、`ModalContainer`をもう1つ(Overlay Canvas用に)追加する**だけで
済む。Modalと同じPush/Pop・ライフサイクルイベント・自己Close(3.4)・`ClearAsync`(3.5)がそのまま使える。

```csharp
[RequireComponent(typeof(ModalContainer))]
public sealed class OverlayContainer : MonoBehaviour
{
    public ModalContainer Container => container ??= GetComponent<ModalContainer>();
    private ModalContainer container;
}
```

`ModalContainer`をそのまま2つ`PageContainer`/`ModalContainer`のように渡すと、VContainerが同じ型の
登録を区別できず解決に失敗する。そのため`OverlayContainer`という薄いマーカーコンポーネント(実体は
`GetComponent<ModalContainer>()`を返すだけ)を挟み、`ScreenNavigator`のコンストラクタは
`ModalContainer modalContainer, OverlayContainer overlayContainer`のように**型で**区別して受け取る。

- Overlay用のGameObject構成は既存の`ModalContainer`(例: `MainModalContainer`)と同じで、Overlay Canvas
  (Modal Canvasよりさらに高い`Sorting Order`)配下に置くだけでよい
- Toastのような「複数同時に表示され、自動で消える」ものは、Modalのスタック(1つが前面、他は裏に隠れる)
  にはそのまま乗らない。今回のOverlayは通信エラーダイアログ(Modal同様、閉じるまで残る1つのスタック)
  向けの設計で、Toastの多重表示は別途検討する

---

## 4. シーン遷移

シーンをまたぐ遷移は、Page/Modalのスタック管理(`IScreenNavigator`)とは**別のインターフェース・別実装**に
する(責務を分ける方針そのものは変えない)。ただし、この別インターフェースもこのフォーク版USNパッケージに
含める(2章)。Atlas既存の`Atlas.Navigation.ISceneNavigator`(`Client/.../Navigation/SceneNavigator.cs`)は
パッケージ内の型に置き換え、Atlas側は個別に実装を持たない。

```csharp
public interface ISceneNavigator
{
    UniTask ChangeSceneAsync(string address, CancellationToken cancellation = default);
}
```

実装(`SceneNavigator`)は`Supplement.Loader`(`Supplement.Loader.Abstractions.ISceneLoader`。実装済み。
`Runtime/SceneNavigator/SceneNavigator.cs`)にすべて委ねる:

```
ISceneNavigator.ChangeSceneAsync(address)
  → ISceneLoader.ChangeScene(address, additive: true, ct) で次のシーンを加算ロードし、Activate
  → ISceneLoader.SetActiveScene(...) で次のシーンをアクティブにする
  → 前のシーンの ISceneHandle を Dispose(内部でアンロードされる)
```

`additive: true`で読み込むのは、Bootstrapシーンを常駐させたまま「コンテンツシーン」だけを差し替える
構成のため(`LoadSceneMode.Single`にすると常駐しているシーンまで巻き込まれる)。前のシーンを先に消さず、
次のシーンを読み込んでから外す順序にしているのは、切り替え中に「何も無い一瞬」を作らないため
(当初案の「先にDispose・アンロードしてからロード」から、実装時にこの順序へ変更した)。
前のシーンの`LifetimeScope`(と、それに紐づく`ScreenNavigator`・Push中のPresenterのヘッドレススコープ)は、
`ISceneHandle.Dispose()`によるシーンアンロードに連動してUnityが`OnDestroy`を呼ぶことで、3.1のパターンAの
場合は自動的に破棄される(パターンBの場合はアプリ側でスタックの整理が必要な点は変わらない)。

- `ISceneNavigator`はRootの`LifetimeScope`に`Singleton`登録する(アプリ全体で1つ、常駐)。3.1の
  `IScreenNavigator`の登録パターン(A/B)とは独立した話で、`ISceneNavigator`は常にRoot常駐という点は
  固定でよい(シーンをまたいで動く責務そのものなので、シーンの寿命に紐づけようが無い)
- Atlas固有の実装(どのシーンをBootstrapとするか、`address`の解決等)はアプリ側の設定・呼び出し側に残し、
  パッケージ側は`ISceneLoader`を呼ぶだけの汎用的な手順だけを持つ
- `IScreenNavigator`と`ISceneNavigator`は1つの巨大なインターフェースにまとめない。シーン内の
  Page/Modal管理とシーンそのものの入れ替えは別の関心事のままにする

---

## 5. Page/ModalのDI設計

`ScreenNavigator`はシーンの`LifetimeScope`に`Scoped`登録される(3.1)ため、コンストラクタで
`IObjectResolver`を受け取れば、それは常に**そのシーンのresolver**になる(VContainerが各スコープの
`IObjectResolver`を自動登録するため、特別な配線は不要)。Push された Page / Modal の Presenter は、
Push のたびにこのresolverから`CreateScope(...)`で専用スコープを作り直す(Unityの `LifetimeScope`
コンポーネントは介さない、ヘッドレスなC#側のスコープ)。View と Presenter はこのスコープの生存期間に
紐づく(Push で生成、Pop で Dispose される)。View(GameObject)自体もUSN本体の通常のPush/Popと同じ
寿命(Pushで`Instantiate`、Popで`Destroy`)で、プール化はしない(1章参照)。

- Pushの引数で親スコープを差し替える口は持たない(シーンの`LifetimeScope`で決まるため)
- シーンをまたいだ「機能スコープ」のような追加の名前付き概念はSupplement側には作らない
- **`IAssetLoader`はRootの`LifetimeScope`にのみ登録し、シーンの`LifetimeScope`では再登録しない。**
  シーンScopedの`ScreenNavigator`から見ると`IAssetLoader`は自分のスコープに無い依存になるが、VContainerの
  子スコープは未登録の型を親に辿って解決するため、普通に解決できる。シーン側で`IAssetLoader`を登録して
  しまうと、シーンごとに別インスタンスができ、Addressablesの参照カウント管理がシーンをまたいで
  共有されなくなる(将来View自体をプール化する場合(6章、現時点では見送り)も同じ理由が当てはまる)

**依存の向きはView→Presenterではなく、Presenter→Viewの一方向にする。** Viewは自分がどのPresenterと
組むか知らない(Presenter型への参照を持たない)。Presenterは具体的なView型・`TArgs`・その他のサービスを
すべてコンストラクタインジェクションで受け取る:

```csharp
var scope = resolver.CreateScope(builder =>    // resolver = このシーンのScreenNavigatorが受け取ったIObjectResolver
{
    builder.RegisterInstance(args);
    builder.RegisterInstance(view, view.GetType());   // Viewを実行時の具体型で解決可能にする
});
var presenter = scope.Resolve<TPresenter>();          // コンストラクタでView/Args/他サービスを受け取る
```

`builder.RegisterInstance(view, view.GetType())`(`object instance, Type implementationType`の
非ジェネリックオーバーロード)は、Viewを**実行時の具体型**(例: `BattlePageView`)で登録する。これにより
`BattlePagePresenter`のコンストラクタは`BattlePageView view`のように具体型をそのまま受け取れる
(VContainer本体の`InstanceRegistrationBuilder`/`Registry.Build()`の実装で、`.As(type)`に渡した型で
解決可能になることを確認済み)。ViewがPresenterの型を知るような形は
依存の向きが逆になるため採用しない(5.2参照)。Viewが独自にサービスを注入される必要は無くなるため、
`InjectGameObject`は不要になる。

### 5.1 ライフサイクルは継承ではなく共通インターフェースで扱う

USNの `IPageLifecycleEvent`/`IModalLifecycleEvent`(Page/Modalで別インターフェース)はPresenterに継承させず、
パッケージ内部の `PageLifecycleAdapter`/`ModalLifecycleAdapter` が実装する。`ScreenNavigator` がPushの
たびにアダプタを生成し、`page.AddLifecycleEvent(adapter)`/`modal.AddLifecycleEvent(adapter)` で登録する。
アプリ側のPresenterは、Page/Modalの違いを意識しない共通の `ILifecycleHandler`(既定メソッド持ちなので
必要な分だけ実装すればよい)を任意で実装するだけでよい。

```csharp
public interface ILifecycleHandler
{
    UniTask InitializeAsync() => UniTask.CompletedTask;
    UniTask WillPushEnterAsync() => UniTask.CompletedTask;
    void DidPushEnter() { }
    UniTask WillPushExitAsync() => UniTask.CompletedTask;
    void DidPushExit() { }
    UniTask WillPopEnterAsync() => UniTask.CompletedTask;
    void DidPopEnter() { }
    UniTask WillPopExitAsync() => UniTask.CompletedTask;
    void DidPopExit() { }
    UniTask CleanupAsync() => UniTask.CompletedTask;
}
```

アダプタは `ExitCancellationToken`(Enter〜Exitの間だけ有効)/`DisposeCancellationToken`(破棄まで有効)も
管理する。

結果を返す仕組みはPresenter側にフィールドを持たせず、`IPresenter.CompleteAsync()`の上書きと
`IScreenNavigator.WaitForPopAsync<TResult>`だけで完結させる(3.4参照。`ScreenResultCompletion`/
`IScreenWithResult<TResult>`のようなPresenter側のコンポジションは不要)。完了待ちの管理(旧
`ScreenResultCompletion`が担っていたもの)は`ScreenNavigator`内部のPresenter↔View対応表(3.4の自己Popと
同じもの)に移り、Popが完了すれば理由を問わず`CompleteAsync()`が呼ばれ、待機している`WaitForPopAsync`が
解決される。共通の抽象基底クラス(`ScreenPresenter<TResult>` 等)も作らない。

### 5.2 PresenterがViewに描画する(Openアニメーションより前)

Viewへの描画は新しいインターフェースを作らず、Supplementに既にある `IRenderable<T>`/`IAsyncRenderable<T>`
(`Supplement.Core`/`Supplement.Unity`)をそのまま使う。**`T`はPresenterの型ではなく、その画面専用の
描画用DTO**(例: `BattlePageRenderDto`)にする。ViewはこのDTOの型だけを知り、Presenterの型は知らない
(5.1の依存の向きと矛盾しないため)。

```csharp
// アプリ側: Viewは描画用DTOの型だけを知る
public sealed class BattlePageView : Page, IAsyncRenderable<BattlePageRenderDto>
{
    public async UniTask RenderAsync(BattlePageRenderDto dto) { /* UIに反映 */ }
}
```

Presenter側の実装は3.2の`BattlePagePresenter`の`InitializeAsync`を参照(同じクラス)。

`InitializeAsync`はUSNの`Initialize`(Push時、`WillPushEnter`→遷移アニメーションより前)にマップされる
(5.1)ため、ここで描画を済ませれば「Openアニメーションより前に描画準備を終える」という要件を、
フレームワーク側に仕組みを追加せず満たせる。配線(`is`判定等)はフレームワークではなく**Presenter自身**が
行う(Presenterはコンストラクタで具体的なView型を既に持っているため)。

このパターンを使うため、このパッケージは`Supplement.Unity`(`IAsyncRenderable<T>`がある)に依存する(2章)。

---

## 6. Viewのプール化(見送り)

Page/ModalのView(GameObject)をプールして再利用する案は、当面**見送る**(1章参照)。理由は、
プール化以外の機能はUSN本体を無改造で実現できるのに対し、プール化だけは`PageContainer`/
`ModalContainer`の`LoadPage`内`Instantiate`・`AfterPushRoutine`/`AfterPopRoutine`内`Destroy`という
USN本体のソースを直接書き換える必要があり、これをやると`Assets/UnityScreenNavigator`(フォーク)に
初めて実際の差分が生まれるため。

必要になった場合の対応方針だけ書いておく: `ViewPool`(`ScreenService`の`ViewCachePool`と同じ、
シーンをまたいで共有するRoot Singletonの実装)を追加し、`PageContainer`/`ModalContainer`の
`Instantiate`/`Destroy`をそこからの取得/返却に差し替える。あわせて`Page.AfterLoad`/`Modal.AfterLoad`の
自己登録漏れ(`_lifecycleEvents.AddItem(this, 0)`に対応する`RemoveItem`が無く、毎回Instantiateしていた
頃は問題にならなかったが、プール化して同じインスタンスを使い回すと`Initialize`等が再利用回数分だけ
重複して呼ばれる)も同時に直す必要がある。これらは`Assets/UnityScreenNavigator`側への直接の変更になる。

## 7. USNから無改造で使う内容

- Push/Pop/スタック管理、`IsInTransition` によるガード等のコンテナ機構
- 豊富なライフサイクルイベント(`Initialize`/`Cleanup`/`WillPushEnter`/`DidPushEnter`/`WillPushExit`/
  `DidPushExit`/`WillPopEnter`/`DidPopEnter`/`WillPopExit`/`DidPopExit`)、`AddLifecycleEvent`、
  `IPageContainerCallbackReceiver`
- Sheet機能
- `ITransitionAnimation` ベースのアニメーション機構(Timeline/PlayableDirector非依存)
- `PageContainer`/`ModalContainer`の`Of(transform)`/`Find(name)`(コンテナ取得。3.5で使用)

USNの拡張要望としてデフォルトでAddressablesLoaderを利用するようにしたい(これは通常の利用設定の話で、
ソース変更は不要)。

----

## 8 残作業

1. ~~`Supplement.UnityScreenNavigator`側に`ScreenNavigator`/`ScreenNavigatorExtensions`(3, 3.5)・
   `PageLifecycleAdapter`/`ModalLifecycleAdapter`(5.1)を実装する~~ 実装済み
2. ~~`Supplement.UnityScreenNavigator`側に`SceneNavigator`(4章)を実装する~~ 実装済み。
   `Supplement.Loader`(`com.chinpangx.supplement`)を依存に追加し、`ISceneLoader`をそのまま利用した
3. ~~Client側 `Atlas.Navigation`・`HomeLifetimeScope`/`BattleLifetimeScope` 等を`Supplement.UnityScreenNavigator`
   に置き換える。Atlas側`manifest.json`に2章の2つのgit URLを登録する~~ 実装済み。パッケージはフォーク本体に
   統合したため、`manifest.json`には`?path=/Assets/UnityScreenNavigator#develop`の1つだけを登録した。
   あわせて、Atlasが持っていた遷移の直列化(`TransitionQueue`)をフォークの`ScreenNavigator`へ移した
   (二重Popの防止・遷移完了を待つシーン切り替えはAtlas側で持つ)。また、PresenterをTransientで登録していたため
   子スコープのDisposeでPresenterがDisposeされていなかった(VContainerはTransientをDisposeしない)不具合を、
   Scoped登録に変えて修正した
4. ~~`OverlayContainer`/`PushOverlayAsync`/`PopOverlayAsync`(3.6)を実装する~~ C#側は実装済み。
   Demoの`Overlay Canvas`配下にモーダル用GameObject(`ModalContainer`+`OverlayContainer`)を追加し、
   `DemoLifetimeScope`の`overlayContainer`フィールドに割り当てる作業が残っている(シーンの手動編集)
