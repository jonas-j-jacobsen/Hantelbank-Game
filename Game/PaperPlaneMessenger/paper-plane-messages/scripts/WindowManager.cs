using Godot;
using System;

#if GODOT_WINDOWS
using System.Runtime.InteropServices;
#endif

public partial class WindowManager : Node
{
#if GODOT_WINDOWS
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [System.Runtime.InteropServices.StructLayout(
        System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_LAYERED = 0x00080000;

    private IntPtr _hwnd;
#endif

    public override void _Ready()
    {
        CallDeferred(MethodName.Init);
    }

    private void Init()
    {
        DisplayServer.WindowSetTitle("PaperPlaneMessenger");
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Transparent, true);
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop, true);
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);

#if GODOT_WINDOWS
        _hwnd = (IntPtr)DisplayServer.WindowGetNativeHandle(
        DisplayServer.HandleType.WindowHandle);
#endif

        SetClickThrough(true);
    }

    public void SetClickThrough(bool enabled)
    {
#if GODOT_WINDOWS
        int exStyle = (int)GetWindowLongPtr(_hwnd, GWL_EXSTYLE);
        if (enabled)
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(
                exStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_LAYERED));
        else
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(
                (exStyle & ~WS_EX_TRANSPARENT & ~WS_EX_NOACTIVATE) | WS_EX_LAYERED));
#else
        if (enabled)
            DisplayServer.WindowSetMousePassthrough(new Vector2[0]);
        else
        {
            var size = DisplayServer.WindowGetSize();
            DisplayServer.WindowSetMousePassthrough(new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(size.X, 0),
                new Vector2(size.X, size.Y),
                new Vector2(0, size.Y)
            });
        }
#endif
    }

    public Vector2 GetMausPosition()
    {
#if GODOT_WINDOWS
        GetCursorPos(out POINT p);
        var winPos = DisplayServer.WindowGetPosition();
        return new Vector2(p.X - winPos.X, p.Y - winPos.Y);
#else
        var mousePos = DisplayServer.MouseGetPosition();
        var winPos = DisplayServer.WindowGetPosition();
        return mousePos - winPos;
#endif
    }
}