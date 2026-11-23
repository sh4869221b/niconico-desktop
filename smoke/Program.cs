using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace NiconicoDesktop;

internal static class SmokeProgram
{
    [STAThread]
    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        ApplicationConfiguration.Initialize();
        var profile = Directory.CreateTempSubdirectory("niconico-desktop-smoke-");
        using var window = new BrowserForm(Routes.Home + "search/%E7%8C%AB", profile.FullName)
        {
            ShowInTaskbar = false, Opacity = 0, StartPosition = FormStartPosition.Manual
        };
        var result = 1;
        window.Shown += async (_, _) =>
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(75));
                var token = timeout.Token;
                var view = Descendants(window).OfType<WebView2>().Single();
                while (view.CoreWebView2 is null) await Task.Delay(250, token);
                var core = view.CoreWebView2;
                var search = await Navigate(core, Routes.Home + "search/%E7%8C%AB", token);
                Require(search.Contains("search/", StringComparison.Ordinal), "Search did not reach official results.");
                var tag = await Navigate(core, Routes.ResolveTag("初音ミク")!, token);
                Require(Routes.Canonical(tag)?.Kind == "タグ検索", "Tag search did not reach official results.");
                var logIn = await Navigate(core, Routes.Login, token);
                Require(new Uri(logIn).Host == "account.nicovideo.jp", "Login hostname did not match.");
                var watch = await Navigate(core, Routes.Home + "watch/sm9", token);
                Require(watch.Contains("/watch/", StringComparison.Ordinal), "Watch page did not load.");
                Console.WriteLine("PASS: WebView2 started; official keyword search, tag search, login, and watch pages loaded.");
                result = 0;
            }
            catch (Exception e) { Console.Error.WriteLine(e.Message); }
            finally { window.Close(); }
        };
        Application.Run(window);
        return result;
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var item in Descendants(child)) yield return item;
        }
    }

    private static async Task<string> Navigate(CoreWebView2 core, string url, CancellationToken token)
    {
        var task = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        ulong? id = null;
        void Start(object? _, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri == url) id = e.NavigationId;
        }
        void Done(object? _, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (id != e.NavigationId) return;
            if (e.IsSuccess && e.HttpStatusCode is >= 200 and < 400) task.TrySetResult(core.Source);
            else task.TrySetException(new Exception($"{new Uri(url).GetLeftPart(UriPartial.Path)}: {e.WebErrorStatus} (HTTP {e.HttpStatusCode})"));
        }
        core.NavigationStarting += Start;
        core.NavigationCompleted += Done;
        try { core.Navigate(url); return await task.Task.WaitAsync(token); }
        finally { core.NavigationStarting -= Start; core.NavigationCompleted -= Done; }
    }

    private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
}
