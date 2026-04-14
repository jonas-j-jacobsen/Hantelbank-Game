using Godot;

public partial class FreundOnlinePopup : Control
{
    private VBoxContainer _container;

    public override void _Ready()
    {
        _container = GetNode<VBoxContainer>("VBoxContainer");

        var wsManager = GetNode<WebSocketManager>("/root/WebSocketManager");
        wsManager.FreundOnline += (userId, username) => 
        {
            GD.Print("FreundOnline Signal empfangen: " + username);
            ZeigePopup(username); 
        };
    }

    private void ZeigePopup(string username)
    {
        var label = new Label();
        label.Text = "✈ " + username + " ist online!";
        label.HorizontalAlignment = HorizontalAlignment.Right;
        label.Modulate = new Color(1, 1, 1, 0);
        _container.AddChild(label);

        var tween = label.CreateTween();
        tween.TweenProperty(label, "modulate:a", 1f, 0.3f);
        tween.TweenInterval(5f);
        tween.TweenProperty(label, "modulate:a", 0f, 0.5f);
        tween.TweenCallback(Callable.From(() => label.QueueFree()));
    }
}