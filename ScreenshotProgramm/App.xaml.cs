using ScreenshotProgramm.Services;

namespace ScreenshotProgramm;

public partial class App : System.Windows.Application
{
    public static SettingsService SettingsService { get; } = new();
    public static LocalizationService LocalizationService { get; } = new();

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        SettingsService.Load();
        LocalizationService.SetLanguage(SettingsService.Settings.Language);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        SettingsService.Save();
        base.OnExit(e);
    }
}
