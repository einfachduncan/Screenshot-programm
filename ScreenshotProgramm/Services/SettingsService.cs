using System.Text.Json;
using ScreenshotProgramm.Models;
using System.IO;

namespace ScreenshotProgramm.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsFile;

    public SettingsService()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenshotProgramm");
        Directory.CreateDirectory(appData);
        _settingsFile = Path.Combine(appData, "settings.json");
    }

    public AppSettings Settings { get; private set; } = new();

    public void Load()
    {
        if (!File.Exists(_settingsFile))
        {
            Save();
            return;
        }

        try
        {
            var content = File.ReadAllText(_settingsFile);
            var loaded = JsonSerializer.Deserialize<AppSettings>(content, JsonOptions);
            Settings = loaded ?? new AppSettings();
        }
        catch
        {
            Settings = new AppSettings();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Settings, JsonOptions);
        File.WriteAllText(_settingsFile, json);
    }
}
