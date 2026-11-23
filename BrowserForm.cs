using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace NiconicoDesktop;

internal sealed class BrowserForm : Form
{
    private static readonly Color Background = Color.FromArgb(20, 24, 32);
    private static readonly Color PanelColor = Color.FromArgb(28, 34, 45);
    private static readonly Color TextColor = Color.FromArgb(244, 247, 252);
    private static readonly Color Muted = Color.FromArgb(180, 194, 211);
    private static readonly Color Accent = Color.FromArgb(44, 102, 211);
    private readonly string _data;
    private readonly WebView2 _view = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly TextBox _address = new();
    private readonly ListBox _list = new();
    private readonly Label _status = new();
    private readonly Label _placeholder = new();
    private readonly Button _back = MakeButton("‹", 44);
    private readonly Button _forward = MakeButton("›", 44);
    private readonly Button _reload = MakeButton("更新", 64);
    private readonly Button _pin = MakeButton("☆ 登録", 92);
    private readonly Button _pins = MakeButton("登録済み", 95);
    private readonly Button _history = MakeButton("視聴履歴", 95);
    private readonly Button _remove = MakeButton("選択項目を削除", 205);
    private readonly LibraryStore _library;
    private CoreWebView2? _core;
    private bool _showHistory;
    private string? _lastSuccessful;
    private string? _queued;
    private bool _allowHistory = true;

    internal BrowserForm(string? initial, string? profileRoot = null)
    {
        _data = profileRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NiconicoDesktop");
        _queued = initial;
        _library = new LibraryStore(Path.Combine(_data, "library.json"));
        _library.Load();
        Text = "Niconico Desktop";
        Font = new Font("Yu Gothic UI", 10F);
        Width = 1260;
        Height = 850;
        MinimumSize = new Size(890, 610);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = TextColor;
        KeyPreview = true;
        KeyDown += Shortcuts;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Margin = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 236));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        var toolbar = BuildToolbar();
        layout.Controls.Add(toolbar, 0, 0);
        layout.SetColumnSpan(toolbar, 2);
        layout.Controls.Add(BuildSidebar(), 0, 1);
        var content = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Background };
        _placeholder.Dock = DockStyle.Fill;
        _placeholder.TextAlign = ContentAlignment.MiddleCenter;
        _placeholder.ForeColor = Muted;
        _placeholder.Text = "ニコニコを読み込んでいます…";
        content.Controls.Add(_view);
        content.Controls.Add(_placeholder);
        layout.Controls.Add(content, 1, 1);
        _status.Dock = DockStyle.Fill;
        _status.Padding = new Padding(10, 3, 0, 0);
        _status.ForeColor = Muted;
        _status.AutoEllipsis = true;
        layout.Controls.Add(_status, 0, 2);
        layout.SetColumnSpan(_status, 2);
        Controls.Add(layout);
        SetStatus(_library.LoadError ?? "ニコニコ公式ページを表示します。");
        RenderList();
        UpdateControls();
        Shown += async (_, _) => await StartAsync();
    }

    private Control BuildToolbar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(8, 9, 10, 8),
            BackColor = PanelColor, ColumnCount = 9, RowCount = 1
        };
        foreach (int n in new[] { 48, 48, 68, 80 }) bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, n));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int n in new[] { 76, 98, 100, 102 }) bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, n));
        _back.AccessibleName = "戻る";
        _forward.AccessibleName = "進む";
        _back.Click += (_, _) => { if (_core?.CanGoBack == true) _core.GoBack(); };
        _forward.Click += (_, _) => { if (_core?.CanGoForward == true) _core.GoForward(); };
        _reload.Click += (_, _) => _core?.Reload();
        var home = MakeButton("ホーム", 74);
        home.Click += (_, _) => Navigate(Routes.Home);
        _address.Dock = DockStyle.Fill;
        _address.Font = new Font("Yu Gothic UI", 12F);
        _address.Margin = new Padding(8, 7, 8, 3);
        _address.BorderStyle = BorderStyle.FixedSingle;
        _address.BackColor = Background;
        _address.ForeColor = TextColor;
        _address.AccessibleName = "動画を検索、またはニコニコのURLを入力";
        _address.PlaceholderText = "キーワード・動画ID・ニコニコのURL";
        _address.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            SubmitAddress();
        };
        var go = MakeButton("検索", 70, true);
        go.Click += (_, _) => SubmitAddress();
        _pin.Click += (_, _) => PinCurrent();
        var my = MakeButton("マイリスト", 94);
        my.Click += (_, _) => Navigate(Routes.MyLists);
        var login = MakeButton("ログイン", 96);
        login.Click += (_, _) => Navigate(Routes.Login);
        foreach (Control c in new Control[] { _back, _forward, _reload, home, _address, go, _pin, my, login })
            bar.Controls.Add(c);
        return bar;
    }

    private Control BuildSidebar()
    {
        var side = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(12),
            BackColor = PanelColor, ColumnCount = 1, RowCount = 5
        };
        side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        var heading = new Label
        {
            Text = "NICONICO\nDESKTOP", Dock = DockStyle.Fill, ForeColor = TextColor,
            Font = new Font("Yu Gothic UI", 13, FontStyle.Bold), Padding = new Padding(4, 4, 0, 0)
        };
        var switches = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
        _pins.Click += (_, _) => { _showHistory = false; RenderList(); };
        _history.Click += (_, _) => { _showHistory = true; RenderList(); };
        switches.Controls.AddRange([_pins, _history]);
        _list.Dock = DockStyle.Fill;
        _list.BackColor = Background;
        _list.ForeColor = TextColor;
        _list.BorderStyle = BorderStyle.None;
        _list.HorizontalScrollbar = true;
        _list.FormattingEnabled = true;
        _list.Format += (_, e) =>
        {
            if (e.ListItem is SavedPage x) e.Value = $"[{x.Kind}] {x.Title}";
        };
        _list.DoubleClick += (_, _) => OpenSelection();
        _list.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; OpenSelection(); } };
        _list.AccessibleName = "登録済みリンク、または視聴履歴";
        var open = MakeButton("選択項目を開く", 205, true);
        open.Click += (_, _) => OpenSelection();
        _remove.Click += (_, _) => RemoveSelection();
        side.Controls.Add(heading);
        side.Controls.Add(switches);
        side.Controls.Add(_list);
        side.Controls.Add(open);
        side.Controls.Add(_remove);
        return side;
    }

    private static Button MakeButton(string title, int width, bool primary = false)
    {
        var b = new Button
        {
            Text = title, Width = width, Height = 34, Margin = new Padding(2, 3, 2, 3),
            BackColor = primary ? Accent : Color.FromArgb(42, 51, 67),
            ForeColor = TextColor, FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private async Task StartAsync()
    {
        try
        {
            Directory.CreateDirectory(_data);
            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: Path.Combine(_data, "BrowserProfile"));
            if (IsDisposed) return;
            await _view.EnsureCoreWebView2Async(env);
            if (IsDisposed) return;
            _core = _view.CoreWebView2;
            _core.Settings.IsWebMessageEnabled = false;
            _core.Settings.AreHostObjectsAllowed = false;
            _core.Settings.IsPasswordAutosaveEnabled = false;
            _core.Settings.IsGeneralAutofillEnabled = false;
            _core.Settings.AreDevToolsEnabled = false;
            _core.NavigationStarting += (_, e) =>
            {
                if (Routes.IsAllowed(e.Uri))
                {
                    _lastSuccessful = null;
                    _allowHistory = true;
                    SetStatus("読み込み中…");
                    return;
                }
                e.Cancel = true;
                SetStatus("ニコニコ以外への移動をブロックしました。");
            };
            _core.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                if (Routes.IsAllowed(e.Uri) && (e.IsUserInitiated ||
                    new Uri(e.Uri).IdnHost == "account.nicovideo.jp"))
                    Navigate(e.Uri);
                else
                    SetStatus("アプリ外の新規ウィンドウは開きません。");
            };
            _core.LaunchingExternalUriScheme += (_, e) => e.Cancel = true;
            _core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            _core.DownloadStarting += (_, e) => { e.Cancel = true; SetStatus("このアプリは動画やファイルのダウンロードを行いません。"); };
            _core.SourceChanged += (_, _) => UpdateControls();
            _core.HistoryChanged += (_, _) => UpdateControls();
            _core.DocumentTitleChanged += (_, _) => { if (_lastSuccessful == _core.Source) RecordVideo(); };
            _core.NavigationCompleted += (_, e) =>
            {
                UpdateControls();
                if (!e.IsSuccess)
                {
                    if (e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled)
                        SetStatus("ページを読み込めませんでした: " + e.WebErrorStatus);
                    return;
                }
                _lastSuccessful = _core.Source;
                SetStatus(new Uri(_core.Source).IdnHost == "account.nicovideo.jp"
                    ? "ニコニコ公式のアカウント画面" : "ニコニコ公式ページ");
                RecordVideo();
            };
            _placeholder.Visible = false;
            _view.Visible = true;
            Navigate(_queued ?? Routes.Home);
            _queued = null;
        }
        catch (Exception e) when (!IsDisposed)
        {
            _placeholder.Text = "WebView2 を初期化できませんでした。\n\nMicrosoft Edge WebView2 Runtime とネットワークを確認してください。";
            SetStatus("起動エラー: " + e.GetType().Name);
        }
    }

    private void Navigate(string url)
    {
        if (!Routes.IsAllowed(url)) { SetStatus("このURLは開けません。"); return; }
        if (_core is null) { _queued = url; return; }
        _core.Navigate(url);
    }

    private void SubmitAddress()
    {
        if (Routes.Resolve(_address.Text) is { } target)
        {
            Navigate(target);
            _view.Focus();
        }
        else SetStatus("キーワード、動画ID、またはニコニコ公式のHTTPS URLを入力してください。");
    }

    private void UpdateControls()
    {
        _back.Enabled = _core?.CanGoBack == true;
        _forward.Enabled = _core?.CanGoForward == true;
        _reload.Enabled = _core is not null;
        _pin.Enabled = Routes.Canonical(_core?.Source) is not null;
        if (!_address.Focused && Uri.TryCreate(_core?.Source, UriKind.Absolute, out var source) && Routes.IsAllowed(source))
            _address.Text = source.IdnHost == "account.nicovideo.jp" ? source.GetLeftPart(UriPartial.Path) : source.AbsoluteUri;
    }

    private void RecordVideo()
    {
        if (!_allowHistory || _core is null || LibraryStore.Create(_core.Source, _core.DocumentTitle) is not { Kind: "動画" } x)
            return;
        if (_library.History.FirstOrDefault() == x) return;
        _library.Visit(x);
        Persist();
        if (_showHistory) RenderList();
    }

    private void PinCurrent()
    {
        if (_core is null || LibraryStore.Create(_core.Source, _core.DocumentTitle) is not { } x)
        { SetStatus("動画・マイリスト・ユーザーページを開いてから登録してください。"); return; }
        if (!_library.Pin(x)) { SetStatus("登録済み、または登録件数が上限に達しています。"); return; }
        var saved = Persist();
        _showHistory = false;
        RenderList();
        if (saved) SetStatus(x.Kind + "を登録しました。");
    }

    private void OpenSelection() { if (_list.SelectedItem is SavedPage x) Navigate(x.Url); }

    private void RemoveSelection()
    {
        if (_list.SelectedItem is not SavedPage selected) return;
        (_showHistory ? _library.History : _library.Pins).Remove(selected);
        // A title event on the current watch page should not immediately re-add a deleted history entry.
        if (_showHistory && Routes.Canonical(_core?.Source)?.Url == selected.Url)
            _allowHistory = false;
        Persist();
        RenderList();
    }

    private bool Persist()
    {
        try { _library.Save(); return true; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { SetStatus("リンクを保存できませんでした。既存のファイルは上書きしていません。"); return false; }
    }

    private void RenderList()
    {
        _list.BeginUpdate();
        try
        {
            var selected = (_list.SelectedItem as SavedPage)?.Url;
            _list.Items.Clear();
            foreach (var x in _showHistory ? _library.History : _library.Pins)
            {
                int i = _list.Items.Add(x);
                if (x.Url == selected) _list.SelectedIndex = i;
            }
            _pins.BackColor = _showHistory ? Color.FromArgb(42, 51, 67) : Accent;
            _history.BackColor = _showHistory ? Accent : Color.FromArgb(42, 51, 67);
        }
        finally { _list.EndUpdate(); }
    }

    private void SetStatus(string text) => _status.Text = text;

    private void Shortcuts(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.L)
        { _address.Focus(); _address.SelectAll(); e.SuppressKeyPress = true; }
        else if (e.Alt && e.KeyCode == Keys.Left && _core?.CanGoBack == true)
        { _core.GoBack(); e.SuppressKeyPress = true; }
        else if (e.Alt && e.KeyCode == Keys.Right && _core?.CanGoForward == true)
        { _core.GoForward(); e.SuppressKeyPress = true; }
        else if (e.KeyCode == Keys.F5 && _core is not null)
        { _core.Reload(); e.SuppressKeyPress = true; }
    }
}
