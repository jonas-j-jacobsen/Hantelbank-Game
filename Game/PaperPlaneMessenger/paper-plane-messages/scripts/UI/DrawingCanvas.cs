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

    // Pinsel-Texturen im Inspector zuweisbar
    [Export] public Texture2D KreideTextur;
    [Export] public Texture2D PinselTextur;
    private Image _kreideImage;
    private Image _pinselImage;

    public enum PinselTyp { Rund, Kreide, Radiergummi, Pinsel }

    public override void _Ready()
    {
        _display = GetNode<TextureRect>("TextureRect");
        if (KreideTextur != null) _kreideImage = KreideTextur.GetImage();
        if (PinselTextur != null) _pinselImage = PinselTextur.GetImage();
        Leeren();
    }

    public void Leeren()
    {
        _image = Image.CreateEmpty(512, 512, false, Image.Format.Rgba8);
        _texture = ImageTexture.CreateFromImage(_image);
        _display.Texture = _texture;
    }

    public void SetzeImage(Image image)
    {
        if (image.GetWidth() != 512 || image.GetHeight() != 512)
            image.Resize(512, 512, Image.Interpolation.Lanczos);
        _image = image;
        _texture = ImageTexture.CreateFromImage(_image);
        _display.Texture = _texture;
    }

    public Image GetImage() => _image;
    public void SetzefarBe(Color farbe) => _aktuellefarbe = farbe;
    public void SetzePinselGröße(float größe) => _pinselGröße = größe;
    public void SetzePinselTyp(PinselTyp typ) => _pinselTyp = typ;

    public void SetzeKreideTextur(Texture2D textur)
    {
        KreideTextur = textur;
        _kreideImage = textur?.GetImage();
    }

    public void SetzePinselTextur(Texture2D textur)
    {
        PinselTextur = textur;
        _pinselImage = textur?.GetImage();
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsVisibleInTree()) return;
        var lokalEvent = MakeInputLocal(@event);
        if (lokalEvent is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
                _zeichnet = mb.Pressed;
            if (mb.Pressed)
                _letztePos = mb.Position;
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
        }
    }

    private void ZeichneLinie(Vector2 von, Vector2 bis)
    {
        // Bei Textur-Pinseln stempeln wir in Abständen, nicht pixelweise
        bool textureBrush = (_pinselTyp == PinselTyp.Kreide || _pinselTyp == PinselTyp.Pinsel);
        var delta = bis - von;
        float distance = delta.Length();

        if (textureBrush)
        {
            // Abstand zwischen Stempeln (~25 % Pinseldurchmesser für gleichmäßige Striche)
            float spacing = Mathf.Max(1f, _pinselGröße * 0.5f);
            int steps = Mathf.Max(1, (int)(distance / spacing));
            for (int i = 0; i <= steps; i++)
            {
                var p = von + delta * ((float)i / steps);
                StempleTextur((int)p.X, (int)p.Y);
            }
        }
        else
        {
            int steps = (int)distance + 1;
            for (int i = 0; i <= steps; i++)
            {
                var p = von + delta * ((float)i / steps);
                ZeichnePunkt((int)p.X, (int)p.Y);
            }
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

                if (_pinselTyp == PinselTyp.Rund)
                {
                    if (dx * dx + dy * dy <= größe * größe)
                        _image.SetPixel(px, py, farbe);
                }
                else if (_pinselTyp == PinselTyp.Radiergummi)
                {
                    if (dx * dx + dy * dy <= größe * größe)
                        _image.SetPixel(px, py, Colors.White);
                }
            }
        }
    }

    // Stempelt die jeweilige Pinsel-Textur an Position (centerX, centerY)
    // und mischt sie mit Alpha-Blending in das bestehende Bild ein.
    private void StempleTextur(int centerX, int centerY)
    {
        Image stempel = _pinselTyp == PinselTyp.Kreide ? _kreideImage : _pinselImage;
        if (stempel == null) return;

        int größe = Mathf.Max(2, (int)(_pinselGröße * 2)); // Durchmesser
        int halb = größe / 2;
        int stempelW = stempel.GetWidth();
        int stempelH = stempel.GetHeight();

        for (int dy = 0; dy < größe; dy++)
        {
            for (int dx = 0; dx < größe; dx++)
            {
                int px = centerX - halb + dx;
                int py = centerY - halb + dy;
                if (px < 0 || px >= 512 || py < 0 || py >= 512) continue;

                // Textur abtasten (nearest neighbor)
                int sx = Mathf.Clamp((int)((float)dx / größe * stempelW), 0, stempelW - 1);
                int sy = Mathf.Clamp((int)((float)dy / größe * stempelH), 0, stempelH - 1);
                Color stempelPixel = stempel.GetPixel(sx, sy);

                // Intensität aus Alpha (falls Textur Alpha hat) sonst aus Helligkeit
                float intensität = stempelPixel.A;
                if (intensität < 0.999f && stempelPixel.A >= 0.999f)
                    intensität = 1f - (stempelPixel.R + stempelPixel.G + stempelPixel.B) / 3f;
                if (intensität <= 0.001f) continue;

                // Bei Kreide: leichter Zufalls-Effekt für körniges Aussehen
                if (_pinselTyp == PinselTyp.Kreide)
                    intensität *= GD.Randf() * 0.5f + 0.5f;

                Color brushFarbe = _aktuellefarbe;
                brushFarbe.A = intensität;

                Color existing = _image.GetPixel(px, py);
                _image.SetPixel(px, py, AlphaBlend(existing, brushFarbe));
            }
        }
    }

    // Standard "source over" Alpha-Compositing
    private static Color AlphaBlend(Color dst, Color src)
    {
        float outA = src.A + dst.A * (1f - src.A);
        if (outA <= 0.001f) return new Color(0, 0, 0, 0);
        float outR = (src.R * src.A + dst.R * dst.A * (1f - src.A)) / outA;
        float outG = (src.G * src.A + dst.G * dst.A * (1f - src.A)) / outA;
        float outB = (src.B * src.A + dst.B * dst.A * (1f - src.A)) / outA;
        return new Color(outR, outG, outB, outA);
    }
}