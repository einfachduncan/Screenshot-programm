using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ScreenshotProgramm.Helpers;
using ScreenshotProgramm.Models;
using ScreenshotProgramm.Services;

namespace ScreenshotProgramm.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settingsService;
    private readonly LocalizationService _localizationService;
    private string _status;

    public MainViewModel(SettingsService settingsService, LocalizationService localizationService)
    {
        _settingsService = settingsService;
        _localizationService = localizationService;
        _status = _localizationService["status_ready"];

        AvailableFormats = new ObservableCollection<ScreenshotFormat>((ScreenshotFormat[])Enum.GetValues(typeof(ScreenshotFormat)));
        AvailableKeys = new ObservableCollection<Key>(
            Enumerable.Range((int)Key.A, 26).Select(i => (Key)i).Concat(new[] { Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12 }));
        Languages = new ObservableCollection<string>(new[] { "de", "en" });

        CaptureCommand = new RelayCommand(async parameter =>
        {
            if (parameter is CaptureShape shape && CaptureRequested is not null)
            {
                await CaptureRequested(shape);
            }
        });
        SaveSettingsCommand = new RelayCommand(_ => SaveSettingsRequested?.Invoke());
        BrowsePathCommand = new RelayCommand(_ => BrowsePathRequested?.Invoke());
        OpenFolderCommand = new RelayCommand(_ => OpenFolderRequested?.Invoke());
        CopyLastCommand = new RelayCommand(_ => CopyLastRequested?.Invoke());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? SaveSettingsRequested;
    public event Action? BrowsePathRequested;
    public event Action? OpenFolderRequested;
    public event Action? CopyLastRequested;
    public event Func<CaptureShape, Task>? CaptureRequested;

    public ICommand CaptureCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand BrowsePathCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand CopyLastCommand { get; }

    public ObservableCollection<ScreenshotFormat> AvailableFormats { get; }
    public ObservableCollection<Key> AvailableKeys { get; }
    public ObservableCollection<string> Languages { get; }

    public AppSettings Settings => _settingsService.Settings;

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public string AppTitle => _localizationService["app_title"];
    public string CaptureTitle => _localizationService["capture"];
    public string SettingsTitle => _localizationService["settings"];
    public string RectangleLabel => _localizationService["rectangle"];
    public string EllipseLabel => _localizationService["ellipse"];
    public string LassoLabel => _localizationService["lasso"];
    public string WindowLabel => _localizationService["window"];
    public string FullscreenLabel => _localizationService["fullscreen"];
    public string HotkeyLabel => _localizationService["hotkey"];
    public string SavePathLabel => _localizationService["save_path"];
    public string BrowseLabel => _localizationService["browse"];
    public string AutoSaveLabel => _localizationService["autosave"];
    public string FormatLabel => _localizationService["format"];
    public string DelayLabel => _localizationService["delay"];
    public string SoundLabel => _localizationService["sound"];
    public string LanguageLabel => _localizationService["language"];
    public string DarkModeLabel => _localizationService["dark_mode"];
    public string OpenFolderLabel => _localizationService["open_folder"];
    public string CopyLastLabel => _localizationService["copy_last"];

    public void RefreshTexts()
    {
        OnPropertyChanged(nameof(AppTitle));
        OnPropertyChanged(nameof(CaptureTitle));
        OnPropertyChanged(nameof(SettingsTitle));
        OnPropertyChanged(nameof(RectangleLabel));
        OnPropertyChanged(nameof(EllipseLabel));
        OnPropertyChanged(nameof(LassoLabel));
        OnPropertyChanged(nameof(WindowLabel));
        OnPropertyChanged(nameof(FullscreenLabel));
        OnPropertyChanged(nameof(HotkeyLabel));
        OnPropertyChanged(nameof(SavePathLabel));
        OnPropertyChanged(nameof(BrowseLabel));
        OnPropertyChanged(nameof(AutoSaveLabel));
        OnPropertyChanged(nameof(FormatLabel));
        OnPropertyChanged(nameof(DelayLabel));
        OnPropertyChanged(nameof(SoundLabel));
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(DarkModeLabel));
        OnPropertyChanged(nameof(OpenFolderLabel));
        OnPropertyChanged(nameof(CopyLastLabel));
    }

    public void NotifySettingsChanged() => OnPropertyChanged(nameof(Settings));

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
