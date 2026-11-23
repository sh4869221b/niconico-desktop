using System.Text.RegularExpressions;

namespace NiconicoDesktop;
internal static partial class Routes
{
    internal const string Home = "https://www.nicovideo.jp/";
    internal const string MyLists = "https://www.nicovideo.jp/my/mylist";
    internal const string Login = "https://account.nicovideo.jp/login";
    internal const string Discover = "https://www.nicovideo.jp/ranking";

    internal static bool IsAllowed(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var u) && IsAllowed(u);

    internal static bool IsAllowed(Uri u) => u.IsAbsoluteUri &&
        u.Scheme == Uri.UriSchemeHttps && u.UserInfo.Length == 0 && u.IsDefaultPort &&
        u.IdnHost is "www.nicovideo.jp" or "nicovideo.jp" or "account.nicovideo.jp"
            or "sp.nicovideo.jp" or "nico.ms" or "ch.nicovideo.jp" or "video.nicovideo.jp";

    internal static string? Resolve(string? value)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v) || v.Length > 500) return null;
        if (Uri.TryCreate(v, UriKind.Absolute, out var u))
            return IsAllowed(u) ? u.AbsoluteUri : null;
        if (v.StartsWith("//", StringComparison.Ordinal) || v.Contains("://", StringComparison.Ordinal))
            return null;
        if (v.StartsWith("nico.ms/", StringComparison.OrdinalIgnoreCase) ||
            v.StartsWith("www.nicovideo.jp/", StringComparison.OrdinalIgnoreCase))
            return Resolve("https://" + v);
        if (VideoId().IsMatch(v)) return Home + "watch/" + v.ToLowerInvariant();
        if (ShortPath().IsMatch(v)) return Home + v.TrimStart('/');
        return SearchUrl("search", v);
    }

    internal static string? ResolveTag(string? value)
    {
        var tag = value?.Trim();
        if (string.IsNullOrEmpty(tag) || tag.Length > 500 ||
            tag.StartsWith("//", StringComparison.Ordinal) || tag.Contains("://", StringComparison.Ordinal))
            return null;
        return SearchUrl("tag", tag);
    }

    private static string? SearchUrl(string mode, string term) =>
        term.Length is > 0 and <= 500 && term is not ("." or "..") && !term.Any(char.IsControl)
            ? Home + mode + "/" + Uri.EscapeDataString(term)
            : null;

    private static (string Url, string Kind)? CanonicalSearch(Uri u, string path)
    {
        var parts = path.Split('/');
        if (parts is not [("search" or "tag"), var encoded]) return null;
        var term = Uri.UnescapeDataString(encoded);
        if (SearchUrl(parts[0], term) is not { } url) return null;

        // Preserve sorting, but drop pagination, tracking and unknown query parameters.
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in u.Query.TrimStart('?').Split('&'))
        {
            var pair = parameter.Split('=', 2);
            if (pair is not [var key, var value]) continue;
            if ((key == "sort" && SortKey().IsMatch(value)) ||
                (key == "order" && value is "a" or "d"))
                options.TryAdd(key, value);
        }

        var query = string.Join("&", options.OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => x.Key + "=" + x.Value));
        return (query.Length == 0 ? url : url + "?" + query,
            parts[0] == "tag" ? "タグ検索" : "キーワード検索");
    }

    [GeneratedRegex(@"\A[a-z]{1,4}\z", RegexOptions.CultureInvariant)]
    private static partial Regex SortKey();

    internal static (string Url, string Kind)? Canonical(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var u) || !IsAllowed(u)) return null;
        if (u.IdnHost is "account.nicovideo.jp" or "ch.nicovideo.jp") return null;
        var p = u.AbsolutePath.Trim('/');
        if (u.IdnHost is "www.nicovideo.jp" or "nicovideo.jp" or "sp.nicovideo.jp" or "video.nicovideo.jp" &&
            CanonicalSearch(u, p) is { } search) return search;
        if (u.IdnHost == "nico.ms" && VideoId().IsMatch(p)) p = "watch/" + p;
        if (p.Equals("my/mylist", StringComparison.Ordinal)) return (MyLists, "マイリスト");
        if (!ShortPath().IsMatch(p)) return null;
        string kind = p.StartsWith("watch/", StringComparison.OrdinalIgnoreCase) ? "動画" :
            p.Contains("mylist", StringComparison.OrdinalIgnoreCase) ? "マイリスト" : "ユーザー";
        return (Home + p.ToLowerInvariant(), kind);
    }

    [GeneratedRegex(@"^(sm|so|nm)[0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VideoId();
    [GeneratedRegex(@"^/?(?:watch/(?:(?:sm|so|nm)?[0-9]+)|mylist/[0-9]+|user/[0-9]+(?:/(?:video|mylist)(?:/[0-9]+)?)?)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ShortPath();
}
