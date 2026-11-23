using NiconicoDesktop;

static void Check(bool condition, string name) { if (!condition) throw new Exception(name); }
Check(Routes.Resolve("猫 動画") == Routes.Home + "search/%E7%8C%AB%20%E5%8B%95%E7%94%BB", "Japanese keyword");
const string tagUrl = Routes.Home + "tag/%E5%88%9D%E9%9F%B3%E3%83%9F%E3%82%AF";
Check(Routes.ResolveTag(" 初音ミク ") == tagUrl, "Japanese tag");
Check(Routes.ResolveTag("SM9") == Routes.Home + "tag/SM9", "tag is not a video ID");
Check(Routes.ResolveTag("C++ / #") == Routes.Home + "tag/C%2B%2B%20%2F%20%23", "tag escaping");
Check(Routes.ResolveTag(null) is null && Routes.ResolveTag("  ") is null &&
    Routes.ResolveTag("..") is null && Routes.ResolveTag("https://www.nicovideo.jp/") is null,
    "invalid tag input");
const string keywordUrl = Routes.Home + "search/%E7%8C%AB";
Check(Routes.Canonical(keywordUrl)?.Kind == "キーワード検索", "keyword list pin");
Check(Routes.Canonical(tagUrl + "?sort=f&order=d&page=2&ref=abc#x") ==
    (tagUrl + "?order=d&sort=f", "タグ検索"), "tag list pin without pagination or tracking");
Check(Routes.Canonical(Routes.Home + "tag/C%2b%2b")?.Url == Routes.Home + "tag/C%2B%2B",
    "normalize search encoding");
Check(Routes.Canonical(Routes.Home + "tag/") is null &&
    Routes.Canonical(Routes.Home + "tag/foo/bar") is null &&
    Routes.Canonical("https://account.nicovideo.jp/search/secret") is null, "invalid search routes");
Check(Routes.Resolve("SM9") == Routes.Home + "watch/sm9", "video ID");
Check(Routes.Resolve("nico.ms/sm9") == "https://nico.ms/sm9", "short link");
Check(Routes.Resolve("user/123/video") == Routes.Home + "user/123/video", "user uploads");
Check(Routes.Resolve("user/123/mylist/456") == Routes.Home + "user/123/mylist/456", "user playlist");
Check(Routes.IsAllowed(Routes.Login), "official login");
foreach (var url in new[] { "http://www.nicovideo.jp/", "https://evil.com/?x=www.nicovideo.jp", "https://www.nicovideo.jp.evil.test",
    "https://evil.test@www.nicovideo.jp", "https://www.nicovideo.jp@evil.test", "https://www.nicovideo.jp:8443/",
    "javascript:alert(1)", "file:///c:/windows", "ms-settings:", "//evil.com/" })
    Check(!Routes.IsAllowed(url) && Routes.Resolve(url) is null, "blocked: " + url);
Check(Routes.Canonical("https://nico.ms/sm9?ref=abc#x")?.Url == Routes.Home + "watch/sm9", "canonical short link");
Check(Routes.Canonical("https://www.nicovideo.jp/my/mylist")?.Kind == "マイリスト", "account playlist pin");
Check(Routes.Canonical("https://www.nicovideo.jp/user/123/mylist/456")?.Kind == "マイリスト", "user playlist pin");
Check(Routes.Canonical(Routes.Login + "?token=test") is null, "auth not saved");

var dir = Directory.CreateTempSubdirectory("niconico-test-");
try
{
    var path = Path.Combine(dir.FullName, "library.json");
    var store = new LibraryStore(path);
    store.Load();
    var saved = LibraryStore.Create("https://nico.ms/sm9?ref=foo", "テスト動画")!;
    Check(store.Pin(saved) && !store.Pin(saved), "deduplicate pins");
    var keyword = LibraryStore.Create(keywordUrl, "猫の検索結果")!;
    var tag = LibraryStore.Create(tagUrl + "?sort=f&order=d&page=3", "初音ミクのタグ検索")!;
    Check(store.Pin(keyword) && store.Pin(tag), "save both search modes");
    Check(!store.Pin(LibraryStore.Create(tagUrl + "?order=d&sort=f", "別タイトル")!),
        "deduplicate the same search with different pagination");
    store.Visit(saved);
    store.Visit(saved);
    Check(store.History.Count == 1, "deduplicate history");
    store.Save();
    var check = new LibraryStore(path);
    check.Load();
    Check(check.Pins.SequenceEqual(new[] { tag, keyword, saved }) && check.History.Single() == saved, "round trip");
    File.WriteAllText(path, "{invalid");
    check = new LibraryStore(path);
    check.Load();
    Check(check.LoadError is not null, "corrupt file warning");
    try { check.Save(); throw new Exception("corrupt file overwritten"); } catch (IOException) {}
    Check(File.ReadAllText(path) == "{invalid", "preserve damaged file");
}
finally { dir.Delete(recursive:true); }
Console.WriteLine("All route and local-library checks passed.");
