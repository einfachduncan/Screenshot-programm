using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ScreenshotProgramm.Services;

public sealed class HotkeyManager : IDisposable
{
    private const int WmHotKey = 0x0312;
    private const int HotkeyId = 9000;

    private HwndSource? _source;
    private bool _isRegistered;

    public event EventHandler? HotkeyPressed;

    public bool Register(Window window, ModifierKeys modifiers, Key key)
    {
        Unregister();
        var helper = new WindowInteropHelper(window);
        _source = HwndSource.FromHwnd(helper.EnsureHandle());
        _source?.AddHook(WndProc);

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        _isRegistered = RegisterHotKey(helper.Handle, HotkeyId, (uint)modifiers, (uint)virtualKey);
        return _isRegistered;
    }

    public void Unregister()
    {
        if (_source is null)
        {
            return;
        }

        var handle = _source.Handle;
        if (_isRegistered)
        {
            UnregisterHotKey(handle, HotkeyId);
            _isRegistered = false;
        }

        _source.RemoveHook(WndProc);
        _source = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotKey && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose() => Unregister();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
