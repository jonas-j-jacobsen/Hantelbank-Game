using Godot;

public partial class DrawingCanvas : Control
{
    private Image _image;
    private ImageTexture _texture;
    private TextureRect _display;

    private Color _aktuellefarbe = Colors.Black;
    private float _pinselGröße = 4f;
    private PinselTyp _pinselTyp = PinselTyp.Rund;
    private bool _zeichnet = false;
    private Vector2 _letztePos;

    public enum PinselTyp { Rund, Kreide, Radiergummi }

    public override void _Ready()
    {
        _display = GetNode<TextureRect>("TextureRect");
        Leeren();
    }

    public void Leeren()
    {
        _image = Image.CreateEmpty(512, 512, false, Image.Format.Rgba8);
        _image.Fill(Colors.White);
        _texture = ImageTexture.CreateFromImage(_image);
        _display.Texture = _texture;
    }

    public void SetzeImage(Image image)
    {
        _image = image;
        _texture = ImageTexture.CreateFromImage(_image);
        _display.Texture = _texture;
    }

    public Image GetImage() => _image;

    public void SetzefarBe(Color farbe) => _aktuellefarbe = farbe;
    public void SetzePinselGröße(float größe) => _pinselGröße = größe;
    public void SetzePinselTyp(PinselTyp typ) => _pinselTyp = typ;

    public override void _Input(InputEvent @event)
    {
        if (!IsVisibleInTree()) return;

        var lokalEvent = MakeInputLocal(@event);

        if (lokalEvent is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
                _zeichnet = mb.Pressed;
        }

        if (lokalEvent is InputEventMouseMotion mm && _zeichnet)
        {
            var pos = mm.Position;

            if (pos.X >= 0 && pos.X < 512 && pos.Y >= 0 && pos.Y < 512)
            {
                ZeichneLinie(_letztePos, pos);
                _texture.Update(_image);
            }
            _letztePos = pos;

            GD.Print("mm.Position: " + mm.Position);
            GD.Print("GlobalRect: " + GetGlobalRect());
            GD.Print("GlobalPosition: " + GlobalPosition);
            GD.Print("Size: " + Size);
        }
    }

    private void ZeichneLinie(Vector2 von, Vector2 bis)
    {
        var delta = bis - von;
        int steps = (int)delta.Length() + 1;
        for (int i = 0; i <= steps; i++)
        {
            var p = von + delta * ((float)i / steps);
            ZeichnePunkt((int)p.X, (int)p.Y);
        }
    }

    private void ZeichnePunkt(int x, int y)
    {
        Color farbe = _pinselTyp == PinselTyp.Radiergummi ? Colors.White : _aktuellefarbe;
        int größe = (int)_pinselGröße;

        for (int dx = -größe; dx <= größe; dx++)
        {
            for (int dy = -größe; dy <= größe; dy++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px < 0 || px >= 512 || py < 0 || py >= 512) continue;

                switch (_pinselTyp)
                {
                    case PinselTyp.Rund:
                        if (dx * dx + dy * dy <= größe * größe)
                            _image.SetPixel(px, py, farbe);
                        break;

                    case PinselTyp.Kreide:
                        // Zufällige Pixel weglassen für Kreide-Effekt
                        if (dx * dx + dy * dy <= größe * größe &&
                            GD.Randf() > 0.3f)
                        {
                            var kreideFarbe = farbe;
                            kreideFarbe.A = GD.Randf() * 0.7f + 0.3f;
                            _image.SetPixel(px, py, kreideFarbe);
                        }
                        break;

                    case PinselTyp.Radiergummi:
                        if (dx * dx + dy * dy <= größe * größe)
                            _image.SetPixel(px, py, Colors.White);
                        break;
                }
            }
        }
    }
}