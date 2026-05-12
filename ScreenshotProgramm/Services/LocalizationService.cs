namespace ScreenshotProgramm.Services;

public sealed class LocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new()
    {
        ["de"] = new Dictionary<string, string>
        {
            ["app_title"] = "Screenshot Programm",
            ["capture"] = "Screenshot aufnehmen",
            ["rectangle"] = "Rechteck",
            ["ellipse"] = "Kreis/Ellipse",
            ["lasso"] = "Lasso",
            ["window"] = "Fenster",
            ["fullscreen"] = "Vollbild",
            ["settings"] = "Einstellungen",
            ["hotkey"] = "Hotkey",
            ["save_path"] = "Speicherpfad",
            ["browse"] = "Durchsuchen",
            ["autosave"] = "Automatisch speichern",
            ["format"] = "Format",
            ["delay"] = "Verzögerung (Sekunden)",
            ["sound"] = "Sound bei Screenshot",
            ["language"] = "Sprache",
            ["dark_mode"] = "Dark Mode",
            ["open_folder"] = "Ordner öffnen",
            ["copy_last"] = "Letzten Screenshot in Zwischenablage",
            ["tray_capture"] = "Screenshot",
            ["tray_open"] = "Öffnen",
            ["tray_exit"] = "Beenden",
            ["status_ready"] = "Bereit",
            ["status_saved"] = "Screenshot gespeichert: ",
            ["status_copied"] = "Letzter Screenshot in Zwischenablage kopiert",
            ["status_cancelled"] = "Aufnahme abgebrochen",
            ["status_error"] = "Fehler: ",
            ["hotkey_register_failed"] = "Hotkey konnte nicht registriert werden.",
            ["invalid_capture_region"] = "Ungültiger Aufnahmebereich.",
            ["editor_title"] = "Screenshot Editor",
            ["save"] = "Speichern",
            ["cancel"] = "Abbrechen",
            ["text"] = "Text",
            ["arrow"] = "Pfeil",
            ["blur"] = "Blur"
        },
        ["en"] = new Dictionary<string, string>
        {
            ["app_title"] = "Screenshot Program",
            ["capture"] = "Take Screenshot",
            ["rectangle"] = "Rectangle",
            ["ellipse"] = "Circle/Ellipse",
            ["lasso"] = "Lasso",
            ["window"] = "Window",
            ["fullscreen"] = "Fullscreen",
            ["settings"] = "Settings",
            ["hotkey"] = "Hotkey",
            ["save_path"] = "Save Path",
            ["browse"] = "Browse",
            ["autosave"] = "Auto save",
            ["format"] = "Format",
            ["delay"] = "Delay (seconds)",
            ["sound"] = "Play sound",
            ["language"] = "Language",
            ["dark_mode"] = "Dark Mode",
            ["open_folder"] = "Open Folder",
            ["copy_last"] = "Copy Last Screenshot to Clipboard",
            ["tray_capture"] = "Take screenshot",
            ["tray_open"] = "Open",
            ["tray_exit"] = "Exit",
            ["status_ready"] = "Ready",
            ["status_saved"] = "Screenshot saved: ",
            ["status_copied"] = "Last screenshot copied to clipboard",
            ["status_cancelled"] = "Capture cancelled",
            ["status_error"] = "Error: ",
            ["hotkey_register_failed"] = "Hotkey could not be registered.",
            ["invalid_capture_region"] = "Invalid capture region.",
            ["editor_title"] = "Screenshot Editor",
            ["save"] = "Save",
            ["cancel"] = "Cancel",
            ["text"] = "Text",
            ["arrow"] = "Arrow",
            ["blur"] = "Blur"
        }
    };

    public string CurrentLanguage { get; private set; } = "de";

    public void SetLanguage(string language)
    {
        CurrentLanguage = _resources.ContainsKey(language) ? language : "de";
    }

    public string this[string key]
    {
        get
        {
            if (_resources.TryGetValue(CurrentLanguage, out var language) && language.TryGetValue(key, out var value))
            {
                return value;
            }

            return key;
        }
    }
}
