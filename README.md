# Niconico Desktop

ニコニコ動画の公式サイトをアプリ内で見る、非公式の Windows 用ブラウザです。公式プレーヤーを使うため、ニコニコ側の動画・コメント弾幕・公開条件がそのまま適用されます。会員限定などの動画は、視聴資格を持つアカウントで公式ログインしてください。

## 使い方

- **検索**：上部にキーワード、動画 ID、またはニコニコの URL を入れます。
- **ログイン**：アプリ内のニコニコ公式画面で認証してください。認証情報はアプリのコードでは読み取りません。サインイン状態は端末内の専用 WebView2 プロファイルに保存されます。
- **マイリスト**：公式の自分のマイリストを開きます。公開マイリストやユーザーの投稿動画・マイリスト一覧は、公式ページのリンクまたは URL から開けます。
- **☆ 登録**：動画、マイリスト、ユーザーページをローカルに保存します。ニコニコ側のマイリストは変更しません。
- 動画説明のニコニコ内リンクは同じウィンドウで開きます。外部サイトや外部アプリを起動するリンクはブロックします。
- **視聴履歴**：開いた動画を最大100件記録します。認証ページや URL のクエリはローカルのリンク・履歴に保存しません。
- Ctrl+L で検索欄、Alt+← / Alt+→ で戻る・進む、F5 で更新。

## Windows で起動

Windows 10/11 と [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) が必要です。配布 ZIP は展開したフォルダの `NiconicoDesktop.exe` を実行します。

```powershell
dotnet run --project tests/NiconicoDesktop.Tests.csproj
dotnet publish NiconicoDesktop.csproj -c Release -r win-x64 --self-contained true -o .publish/win-x64
```

データは `%LOCALAPPDATA%\NiconicoDesktop` に残ります。ログアウトはニコニコのアカウント画面から行えます。Google など外部事業者経由のサインインは対象外です。
