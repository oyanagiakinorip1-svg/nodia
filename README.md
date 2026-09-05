# NODIA

**▶ 今すぐ試す: https://nodia-web-six.vercel.app**
(WebGLビルドのため、初回読み込みに数十秒かかります)

ノート同士の繋がりを、平面の図ではなく実際に歩ける3D空間として扱えるメモアプリ。
Unity製のWebGLクライアント本体で、バックエンドは別リポジトリの
[nodia-server](https://github.com/oyanagiakinorip1-svg/nodia-server)(Hono API + Supabase)。

## 構成

このリポジトリはUnityプロジェクトなので、見慣れない構造になっているかもしれませんが、
実際のロジックが入っているのは以下だけです。それ以外(`ProjectSettings/`、
`Packages/`、`Library/`相当のもの)はUnityが自動生成・管理するプロジェクト設定です。

- `Assets/Scripts/` — 実際のC#ロジック(ここが本体)
  - `Networking/` — Supabase認証・APIクライアント
  - `Nodes/` — ノード・接続線の生成/操作
  - `Player/` — 一人称視点のカメラ・移動
  - `UI/` — メニュー・検索・設定などの画面
  - `Data/` — APIとやり取りするDTO
- `Assets/Prefabs/`, `Assets/Fonts/` — プレハブ・埋め込みフォントなどのアセット

## 技術構成

- クライアント: Unity 6 (WebGL) — 一人称3D空間、TextMeshPro + 埋め込みNoto Sans JP
- API: [nodia-server](https://github.com/oyanagiakinorip1-svg/nodia-server) (Hono, Vercel Functions)
- データ: Supabase (Postgres + Auth + Row Level Security)
- ホスティング: クライアント・APIともにVercel

## セットアップ

Unity Editorでの環境構築手順は [SETUP.md](./SETUP.md) を参照してください。
