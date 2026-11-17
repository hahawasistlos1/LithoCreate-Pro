using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace LithoCreate.App;

internal static class WindowWorkArea
{
    private const uint NearestMonitor = 2;
    private const uint KeepZOrderAndActivation = 0x0004 | 0x0010;

    internal static void KeepVisible(Window window)
    {
        double layoutMinWidth = window.MinWidth;
        double layoutMinHeight = window.MinHeight;
        window.Loaded += (_, _) => Fit();
        window.DpiChanged += (_, _) => QueueFit();
        window.StateChanged += (_, _) => QueueFit();

        void QueueFit()
        {
            window.Dispatcher.BeginInvoke(Fit, DispatcherPriority.Loaded);
        }

        void Fit()
        {
            if (!window.IsLoaded || window.WindowState != WindowState.Normal)
                return;

            nint handle = new WindowInteropHelper(window).Handle;
            if (!TryReadBounds(handle, out Rect work, out Rect frame))
                return;

            double scale = GetDpiForWindow(handle) / 96.0;
            if (scale <= 0) return;
            int margin = (int)Math.Round(scale * 8);
            double availableWidth = Math.Max(1, work.Width - 2 * margin);
            double availableHeight = Math.Max(1, work.Height - 2 * margin);
            double width = Math.Min(frame.Width, availableWidth);
            double height = Math.Min(frame.Height, availableHeight);

            // WPF minimums are in device-independent units; the monitor bounds are in pixels.
            window.MinWidth = Math.Min(layoutMinWidth, Math.Floor(availableWidth / scale));
            window.MinHeight = Math.Min(layoutMinHeight, Math.Floor(availableHeight / scale));
            double left = Math.Clamp(frame.Left, work.Left, work.Right - width);
            double top = Math.Clamp(frame.Top, work.Top, work.Bottom - height);
            if (frame != new Rect(left, top, width, height))
                SetWindowPos(handle, 0, (int)left, (int)top, (int)width, (int)height, KeepZOrderAndActivation);
        }
    }

    internal static bool TryReadBounds(nint handle, out Rect work, out Rect frame)
    {
        work = frame = Rect.Empty;
        if (handle == 0 || !GetWindowRect(handle, out NativeRect bounds)) return false;
        var monitor = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(handle, NearestMonitor), ref monitor)) return false;
        work = monitor.Work.ToRect();
        frame = bounds.ToRect();
        return work.Width > 0 && work.Height > 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
        public readonly Rect ToRect() => new(Left, Top, Right - Left, Bottom - Top);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor, Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out NativeRect bounds);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
}
