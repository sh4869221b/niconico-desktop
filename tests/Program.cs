using NiconicoDesktop;

static void Check(bool condition, string name) { if (!condition) throw new Exception(name); }
Check(Routes.Resolve("猫 動画") == Routes.Home + "search/%E7%8C%AB%20%E5%8B%95%E7%94%BB", "Japanese keyword");
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
    store.Visit(saved);
    store.Visit(saved);
    Check(store.History.Count == 1, "deduplicate history");
    store.Save();
    var check = new LibraryStore(path);
    check.Load();
    Check(check.Pins.Single() == saved && check.History.Single() == saved, "round trip");
    File.WriteAllText(path, "{invalid");
    check = new LibraryStore(path);
    check.Load();
    Check(check.LoadError is not null, "corrupt file warning");
    try { check.Save(); throw new Exception("corrupt file overwritten"); } catch (IOException) {}
    Check(File.ReadAllText(path) == "{invalid", "preserve damaged file");
}
finally { dir.Delete(recursive:true); }
Console.WriteLine("All route and local-library checks passed.");
