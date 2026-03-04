using Godot;
using System;
using System.Runtime.InteropServices;

public partial class WindowManager : Node
{
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);


    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);


    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_LAYERED = 0x00080000; 
    private IntPtr _hwnd;

    public override void _Ready()
    {
        CallDeferred(MethodName.Init);
    }

    private void Init()
    {
        DisplayServer.WindowSetTitle("GodotOverlay");
        _hwnd = FindWindow(null, "GodotOverlay");

        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Transparent, true);
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop, true);
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);

        SetClickThrough(true);

        // Prüfen ob WS_EX_TRANSPARENT wirklich gesetzt ist
        int style = (int)GetWindowLongPtr(_hwnd, GWL_EXSTYLE);
        bool transparentGesetzt = (style & WS_EX_TRANSPARENT) != 0;
        bool noactivateGesetzt = (style & WS_EX_NOACTIVATE) != 0;
        GD.Print("WS_EX_TRANSPARENT gesetzt: " + transparentGesetzt);
        GD.Print("WS_EX_NOACTIVATE gesetzt: " + noactivateGesetzt);
    }

    public override void _Process(double delta)
    {
        SetClickThrough(true);
    }

    public void SetClickThrough(bool enabled)
    {
        int exStyle = (int)GetWindowLongPtr(_hwnd, GWL_EXSTYLE);
        if (enabled)
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(exStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_LAYERED));
        else
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(exStyle & ~WS_EX_TRANSPARENT & ~WS_EX_NOACTIVATE));
    }
}