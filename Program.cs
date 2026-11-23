namespace NiconicoDesktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var initial = args.FirstOrDefault(x => Routes.IsAllowed(x));
        Application.Run(new BrowserForm(initial));
    }
}
