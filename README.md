# Niconico Desktop

ニコニコ動画の公式サイトをアプリ内で見る、非公式の Windows 用ブラウザです。公式プレーヤーを使うため、ニコニコ側の動画・コメント弾幕・公開条件がそのまま適用されます。会員限定などの動画は、視聴資格を持つアカウントで公式ログインしてください。

## 使い方

- **検索／タグ検索**：上部に文字を入力し、「検索」（Enter）でキーワード検索、「タグ検索」で入力したタグの動画一覧を開きます。「検索」は動画 ID やニコニコの URL も開けます。
- **ログイン**：アプリ内のニコニコ公式画面で認証してください。認証情報はアプリのコードでは読み取りません。サインイン状態は端末内の専用 WebView2 プロファイルに保存されます。
- **マイリスト**：公式の自分のマイリストを開きます。公開マイリストやユーザーの投稿動画・マイリスト一覧は、公式ページのリンクまたは URL から開けます。
- **URLをコピー**：ユーザーの投稿動画一覧や個別のマイリストを開き、上部のボタンで [go-nico-list](https://github.com/sh4869221b/go-nico-list) に渡せる URL をコピーします。コピーした URL は `go-nico-list <URL>` の引数や入力ファイルに貼り付けられます。マイリストの一覧画面では、先に個別のマイリストを選択してください。ログイン状態は共有しないため、非公開マイリストは go-nico-list では取得できません。
- **☆ 登録**：キーワード・タグの検索結果一覧、動画、マイリスト、ユーザーページを端末内に登録します。検索一覧は同じ検索語・タグと並び順で先頭ページを開き直します。結果のコピーではないため、新着動画も反映されます。ニコニコ側のマイリストは変更しません。
- 動画説明のニコニコ内リンクは同じウィンドウで開きます。外部サイトや外部アプリを起動するリンクはブロックします。
- **視聴履歴**：開いた動画を最大100件記録します。認証ページは保存しません。URL のクエリは検索の並び替え設定を除いて保存しません。
- Ctrl+L で検索欄、Alt+← / Alt+→ で戻る・進む、F5 で更新。

## Windows で起動

Windows 10/11 と [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) が必要です。配布 ZIP は展開したフォルダの `NiconicoDesktop.exe` を実行します。

```powershell
dotnet run --project tests/NiconicoDesktop.Tests.csproj
dotnet publish NiconicoDesktop.csproj -c Release -r win-x64 --self-contained true -o .publish/win-x64
```

データは `%LOCALAPPDATA%\NiconicoDesktop` に残ります。ログアウトはニコニコのアカウント画面から行えます。Google など外部事業者経由のサインインは対象外です。

## リリース

GitHub Actions の「Windows build」を `main` から手動実行し、`vMAJOR.MINOR.PATCH` を入力します。同じ形式のタグを push してもリリースできます。テストに合格した Windows x64 ZIP と SHA256 の一覧を GitHub Releases に添付します。
