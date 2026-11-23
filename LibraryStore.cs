using System.Text.Json;

namespace NiconicoDesktop;
internal sealed record SavedPage(string Url, string Title, string Kind);
internal sealed class LibraryStore(string path)
{
    private sealed record Data(List<SavedPage> Pins, List<SavedPage> History);
    internal List<SavedPage> Pins { get; private set; } = [];
    internal List<SavedPage> History { get; private set; } = [];
    internal string? LoadError { get; private set; }

    internal void Load()
    {
        try
        {
            if (!File.Exists(path)) return;
            if (new FileInfo(path).Length > 2_000_000)
            {
                LoadError = "保存ファイルが大きすぎます。";
                return;
            }
            var d = JsonSerializer.Deserialize<Data>(File.ReadAllText(path));
            if (d is null)
            {
                LoadError = "保存ファイルを読み込めません。";
                return;
            }
            Pins = Normalize(d.Pins, 500);
            History = Normalize(d.History, 100);
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        { LoadError = "保存済みのリンクを読み込めません。"; }
    }

    private static List<SavedPage> Normalize(IEnumerable<SavedPage>? input, int max) =>
        (input ?? []).Select(x => Create(x?.Url, x?.Title)).OfType<SavedPage>()
                     .DistinctBy(x => x.Url).Take(max).ToList();

    internal static SavedPage? Create(string? url, string? title)
    {
        if (Routes.Canonical(url) is not { } route) return null;
        var t = string.IsNullOrWhiteSpace(title) ? route.Url : title.Trim();
        t = string.Concat(t.Where(c => !char.IsControl(c)));
        return new SavedPage(route.Url, t.Length > 160 ? t[..160] : t, route.Kind);
    }

    internal bool Pin(SavedPage item)
    {
        if (Pins.Any(x => x.Url == item.Url) || Pins.Count >= 500) return false;
        Pins.Insert(0, item);
        return true;
    }

    internal void Visit(SavedPage item)
    {
        History.RemoveAll(x => x.Url == item.Url);
        History.Insert(0, item);
        if (History.Count > 100) History.RemoveAt(100);
    }

    internal void Save()
    {
        if (LoadError is not null) throw new IOException(LoadError);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(tmp, JsonSerializer.Serialize(new Data(Pins, History)));
            File.Move(tmp, path, true);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }
}
