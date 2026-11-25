namespace NotepadApp;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        MainForm form = new();

        if (args.Length >= 2 && args[0] == "--screenshots")
        {
            form.CaptureScreenshots(args[1]);
            return;
        }

        Application.Run(form);
    }
}
