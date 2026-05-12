using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScreenshotProgramm.Models;
using ScreenshotProgramm.Services;
using ScreenshotProgramm.ViewModels;
using ScreenshotProgramm.Views;
using Point = System.Windows.Point;

namespace ScreenshotProgramm;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ScreenshotService _screenshotService = new();
    private readonly HotkeyManager _hotkeyManager = new();
    private NotifyIcon? _trayIcon;
    private bool _isExitRequested;
    private string? _lastScreenshotPath;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(App.SettingsService, App.LocalizationService);
        _viewModel.CaptureRequested += CaptureRequestedAsync;
        _viewModel.SaveSettingsRequested += SaveSettings;
        _viewModel.BrowsePathRequested += BrowseSavePath;
        _viewModel.OpenFolderRequested += OpenFolder;
        _viewModel.CopyLastRequested += CopyLastScreenshot;

        DataContext = _viewModel;

        Loaded += OnLoaded;
        Closed += (_, _) => _trayIcon?.Dispose();
        _hotkeyManager.HotkeyPressed += async (_, _) => await Dispatcher.InvokeAsync(() => CaptureRequestedAsync(CaptureShape.Rectangle));
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyModifierSelectionsFromSettings();
        ApplyTheme();
        RegisterHotkey();
        InitializeTray();
        HideToTray();
    }

    private void ApplyModifierSelectionsFromSettings()
    {
        CtrlModifier.IsChecked = App.SettingsService.Settings.HotkeyModifiers.HasFlag(ModifierKeys.Control);
        ShiftModifier.IsChecked = App.SettingsService.Settings.HotkeyModifiers.HasFlag(ModifierKeys.Shift);
        AltModifier.IsChecked = App.SettingsService.Settings.HotkeyModifiers.HasFlag(ModifierKeys.Alt);
        WinModifier.IsChecked = App.SettingsService.Settings.HotkeyModifiers.HasFlag(ModifierKeys.Windows);
    }

    private ModifierKeys GetSelectedModifiers()
    {
        var modifiers = ModifierKeys.None;
        if (CtrlModifier.IsChecked == true) modifiers |= ModifierKeys.Control;
        if (ShiftModifier.IsChecked == true) modifiers |= ModifierKeys.Shift;
        if (AltModifier.IsChecked == true) modifiers |= ModifierKeys.Alt;
        if (WinModifier.IsChecked == true) modifiers |= ModifierKeys.Windows;
        return modifiers;
    }

    private void SaveSettings()
    {
        App.SettingsService.Settings.HotkeyModifiers = GetSelectedModifiers();
        App.LocalizationService.SetLanguage(App.SettingsService.Settings.Language);
        App.SettingsService.Save();
        _viewModel.RefreshTexts();
        _viewModel.Status = App.LocalizationService["status_ready"];
        RegisterHotkey();
    }

    private void RegisterHotkey()
    {
        var ok = _hotkeyManager.Register(this, App.SettingsService.Settings.HotkeyModifiers, App.SettingsService.Settings.HotkeyKey);
        if (!ok)
        {
            _viewModel.Status = App.LocalizationService["status_error"] + App.LocalizationService["hotkey_register_failed"];
        }
    }

    private async Task CaptureRequestedAsync(CaptureShape shape)
    {
        try
        {
            SaveSettings();

            if (App.SettingsService.Settings.DelaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(App.SettingsService.Settings.DelaySeconds));
            }

            BitmapSource? captured = shape switch
            {
                CaptureShape.Fullscreen => _screenshotService.CaptureFullscreen(),
                CaptureShape.Window => _screenshotService.CaptureActiveWindow(),
                _ => await CaptureWithOverlayAsync(shape)
            };

            if (captured is null)
            {
                _viewModel.Status = App.LocalizationService["status_cancelled"];
                return;
            }

            var editor = new EditorWindow(captured, App.LocalizationService, App.SettingsService.Settings.DarkMode)
            {
                Owner = this
            };

            var result = editor.ShowDialog();
            if (result != true)
            {
                _viewModel.Status = App.LocalizationService["status_cancelled"];
                return;
            }

            var finalImage = editor.EditedImage;
            if (finalImage is null)
            {
                return;
            }

            System.Windows.Clipboard.SetImage(finalImage);

            if (App.SettingsService.Settings.AutoSave)
            {
                _lastScreenshotPath = _screenshotService.SaveScreenshot(finalImage, App.SettingsService.Settings.SavePath, App.SettingsService.Settings.ScreenshotFormat);
                _viewModel.Status = App.LocalizationService["status_saved"] + _lastScreenshotPath;
            }

            if (App.SettingsService.Settings.PlaySound)
            {
                SystemSounds.Asterisk.Play();
            }
        }
        catch (Exception ex)
        {
            var message = ex.Message == "invalid_capture_region"
                ? App.LocalizationService["invalid_capture_region"]
                : ex.Message;
            _viewModel.Status = App.LocalizationService["status_error"] + message;
        }
    }

    private async Task<BitmapSource?> CaptureWithOverlayAsync(CaptureShape shape)
    {
        Hide();
        await Task.Delay(150);

        var overlay = new Views.SelectionOverlayWindow(shape);
        var dialogResult = overlay.ShowDialog();

        Show();
        Activate();

        if (dialogResult != true || overlay.Result is null)
        {
            return null;
        }

        var rect = overlay.Result.Region;
        var captureRegion = new System.Drawing.Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        var image = _screenshotService.CaptureRegion(captureRegion);

        return shape switch
        {
            CaptureShape.Ellipse => _screenshotService.ApplyEllipseMask(image),
            CaptureShape.Lasso => _screenshotService.ApplyLassoMask(image, overlay.Result.LassoPoints.Select(p => new Point(p.X - rect.X, p.Y - rect.Y)).ToList()),
            _ => image
        };
    }

    private void BrowseSavePath()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            App.SettingsService.Settings.SavePath = dialog.SelectedPath;
            _viewModel.NotifySettingsChanged();
        }
    }

    private void OpenFolder()
    {
        Directory.CreateDirectory(App.SettingsService.Settings.SavePath);
        Process.Start(new ProcessStartInfo
        {
            FileName = App.SettingsService.Settings.SavePath,
            UseShellExecute = true
        });
    }

    private void CopyLastScreenshot()
    {
        if (_lastScreenshotPath is null || !File.Exists(_lastScreenshotPath))
        {
            return;
        }

        var image = new BitmapImage(new Uri(_lastScreenshotPath, UriKind.Absolute));
        System.Windows.Clipboard.SetImage(image);
        _viewModel.Status = App.LocalizationService["status_copied"];
    }

    private void InitializeTray()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = App.LocalizationService["app_title"]
        };

        _trayIcon.DoubleClick += (_, _) => ShowFromTray();
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(App.LocalizationService["tray_capture"], null, async (_, _) => await CaptureRequestedAsync(CaptureShape.Rectangle));
        contextMenu.Items.Add(App.LocalizationService["tray_open"], null, (_, _) => ShowFromTray());
        contextMenu.Items.Add(App.LocalizationService["tray_exit"], null, (_, _) => ExitApplication());
        _trayIcon.ContextMenuStrip = contextMenu;
    }

    private void ShowFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
    }

    private void ExitApplication()
    {
        _isExitRequested = true;
        _trayIcon?.Dispose();
        _hotkeyManager.Dispose();
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExitRequested)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        base.OnClosing(e);
    }

    private void LanguageSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        SaveSettings();
        InitializeTray();
    }

    private void DarkModeChanged(object sender, RoutedEventArgs e)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var dark = App.SettingsService.Settings.DarkMode;
        System.Windows.Application.Current.Resources["AppBackgroundBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(dark ? "#FF111827" : "#FFF7F7F9"));
        System.Windows.Application.Current.Resources["AppForegroundBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(dark ? "#FFF9FAFB" : "#FF1F2937"));
        System.Windows.Application.Current.Resources["CardBackgroundBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(dark ? "#FF1F2937" : "#FFFFFFFF"));
    }
}
