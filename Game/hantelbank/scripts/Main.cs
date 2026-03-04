using Godot;

public partial class Main : Node
{
    public override void _Ready()
    {
        // Fenster transparent & immer im Vordergrund
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Transparent, true);
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop, true);

        // Optional: Kein Rahmen
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);

        // Fenster maximieren
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
    }
}