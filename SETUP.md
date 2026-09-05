# NODIA セットアップ手順

このリポジトリはUnityクライアントのみを含む。バックエンド(Hono API + Supabaseスキーマ)は
別リポジトリ [`nodia-server`](../nodia-server) に分離してある(標準的なVercelプロジェクト構成
にするため)。残りはSupabase/Vercelの設定と、Unity Editor上でのシーン組み立て(GUI操作なので
こちらで代行できない部分)。

## 1. Supabase

1. プロジェクトを作成し、SQL Editorで `nodia-server/supabase/schema.sql` を実行する。
   (既にnodes/connectionsがある既存プロジェクトの場合は、代わりに `migration_001_spaces.sql` を実行する)
2. Authentication > Sign In / Providers で **Anonymous Sign-ins** を有効化する。
   (「お試しで始める」用。無効だとゲストモードが使えない)
3. Authentication > Providers > Email で **Confirm email** を無効化する。
   (有効のままだとメール登録直後にセッションが発行されず、ノード作成が全部失敗する)
4. Project Settings > API Keys から `Project URL` と `Publishable key`(anon key相当)を控える。

## 2. Hono API のデプロイ

`nodia-server` リポジトリ側のREADMEを参照。GitHubにpushしてVercelダッシュボードから
Importするだけで、api/ フォルダをVercelが自動検出する(Root Directory指定不要)。
デプロイ後のURL (`https://xxxx.vercel.app`) を控える。API本体は `https://xxxx.vercel.app/api` 配下。

## 3. Unityスクリプトへの値設定

- `SupabaseAuth` コンポーネントの Inspector に Supabase の `Project URL` / `anon key` を設定。
- `ApiClient` コンポーネントの Inspector に `https://xxxx.vercel.app/api` を設定。

## 4. シーン構築 (Unity Editor)

手動でのGameObject/プレハブ作成・参照のひも付けは `Assets/Editor/Nodia*.cs` の一連のEditorスクリプトが自動化する。**この順番で**メニューから実行する(後の手順は前の手順が作ったオブジェクトを探しに行くため)。

1. **Nodia > Setup Scene** — Player/ノード関連プレハブ/メモUIの土台を生成
2. **Nodia > Style Note UI** — TextMeshPro化、Noto Sans JPフォント、角丸カードのスプライトを用意(以降の画面はすべてこれらを再利用する)
3. **Nodia > Setup Search** — ノード検索オーバーレイ
4. **Nodia > Setup Space Select** — スペース(ノート空間)選択・新規作成画面
5. **Nodia > Setup Auth Screen** — 起動時のお試し/メール登録・ログイン画面(4のSpaceSelectControllerに接続する)
6. **Nodia > Setup Settings** — マウス感度・移動速度の設定画面
7. **Nodia > Setup Help** — 操作方法一覧
8. **Nodia > Setup Main Menu** — 3〜7をまとめて開くメニュー(画面右上の常時表示ボタン、またはTabキーで開く。スマホ対応も見据え、画面ごとの個別キーは廃止した。Escキーはブラウザがフルスクリーン/ポインタロック解除に予約しているため使えない)
9. **Nodia > Setup Crosshair** — 画面中央の照準
10. **Nodia > Style Environment** — 背景・ライティング・ノードの発光を調整

その後、`SupabaseAuth` の Inspector に `Project URL` / `Publishable key` を、`ApiClient` の Inspector に `Api Base Url`(`https://xxxx.vercel.app/api`)を入力する。

いずれも既存オブジェクトを見つけたら作り直さず再利用するので、見た目を調整したくなったら同じメニューを再実行すればよい(ただし `Setup Scene` だけは `Player` が既にあると中断する)。

**動作確認**
- 起動直後: 「お試しで始める」かメール登録/ログイン → スペースを選ぶ/作る、の順で本編に入る。
- WASDで移動、マウスで視点、Space/Ctrlで上下。
- **右クリック**で空間にノードを生成(誤操作防止のため生成は右クリックのみ、左クリックは開く/繋ぐ専用)。
- ノードを左クリック → メモUIが開く(カーソルが出る)。
- ノードにカーソルを合わせると発光色が変わる(狙っている合図、緑)。**Shift+左クリック**で選択(少し拡大+オレンジ色)、もう一度Shift+左クリックで別のノードを選ぶと接続線が引かれる(既に繋がっている2つのノードに同じ操作をすると接続が削除される、トグル式)。
- 接続線を消すもう一つの方法として、**Shift+右クリック**で線そのものを直接クリックして削除できる(ノード接続=Shift+左、線削除=Shift+右、とボタンを分けて誤操作を防止)。
- **Tabキー**でメニューを開く(ノード検索・スペース切り替え・設定・操作方法はすべてここからボタンで開く)。もう一度Tabでメニューや各パネルを閉じる。画面右上の丸いボタンは同じ役割だがタッチ端末でのみ表示される(PCではゲーム中マウスカーソルが視点操作のため隠れて動かせないので、ボタンを置いても押せないため)。

## 補足

- 認証は「お試し(匿名)」と「メール登録・ログイン」の2本立て。同じブラウザなら保存されたセッションで自動再開するので、2回目以降は選択画面自体が出ない。別ブラウザ/端末や、データを消した後に同じメモへ戻るにはメール登録が必要(お試しのデータをメール登録後に引き継ぐことはできない、完全に別アカウント扱い)。
- 1アカウントで複数の「スペース」(ノート空間)を持てる。ノード・接続はすべて`space_id`で分離される。
- ノードのドラッグ移動は未実装(FPS視点だとマウスが視点操作と競合するため、今回はスコープ外にした)。ノード位置は生成時のプレイヤー位置で確定する。
- 初めてスペースに入った時だけ、自動で「操作方法」パネルが開く(見たかどうかはブラウザに保存されるので2回目以降は出ない)。ゲーム内に他のチュートリアルは無いため、新規ユーザーへの説明はここが唯一の入口。
