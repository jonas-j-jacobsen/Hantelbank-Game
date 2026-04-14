using Godot;
using System;

public partial class AktionsBar : DraggablePanel
{
    private UIManager _uiManager;
    private DrawingCanvas _canvas;
    private FileDialog _fileDialog;
    private EmpfängerAuswahl _empfängerAuswahl;
    private EmpfängerButton _empfängerButton;

    private FreundeUI _freundeUI;
    private MousePassthroughManager _passthroughManager;

    private FileDialog.FileModeEnum _aktuellerModus;

    public override void _Ready()
    {
        _uiManager = GetNode<UIManager>("/root/Main/PaperUI/CanvasLayer");
        _canvas = GetNode<DrawingCanvas>("/root/Main/PaperUI/CanvasLayer/ControlEasel/DrawingCanvas");
        _empfängerButton = GetNode<EmpfängerButton>("/root/Main/PaperUI/CanvasLayer/ControlEasel/DrawingCanvas/HBoxContainerEmpfänger/Button");
        _freundeUI = GetNode<FreundeUI>("/root/Main/PaperUI/CanvasLayer/FriendsPanel");
        _passthroughManager = GetNode<MousePassthroughManager>("/root/MousePassthroughManager");

        var faltenButton = GetNode<Button>("/root/Main/PaperUI/CanvasLayer/ControlActions/HBoxContainer/Falten");
        faltenButton.Disabled = true;
        faltenButton.TooltipText = "Erst einen Empfänger auswählen!";

        _empfängerButton.EmpfängerGesetzt += (id, istGruppe, name) =>
        {
            faltenButton.Disabled = false;
            faltenButton.TooltipText = "";
        };


        _fileDialog = new FileDialog();
        _fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        _fileDialog.Filters = new string[] { "*.png ; PNG Bilder" };
        AddChild(_fileDialog);

        _fileDialog.FileSelected += OnDateiGewählt;
        _fileDialog.Canceled += OnFileDialogCanceled;

        GetNode<Button>("/root/Main/PaperUI/CanvasLayer/ControlActions/HBoxContainer/Schließen").Pressed += OnSchließenPressed;
        GetNode<Button>("/root/Main/PaperUI/CanvasLayer/ControlActions/HBoxContainer/Speichern").Pressed += OnSpeichernPressed;
        GetNode<Button>("/root/Main/PaperUI/CanvasLayer/ControlActions/HBoxContainer/Laden").Pressed += OnLadenPressed;
        faltenButton.Pressed += OnFaltenPressed;
        GetNode<Button>("HBoxContainer/VBoxContainer/FreundeFinden").Pressed += () => _freundeUI.ToggleSichtbarkeit();
    }

    private void OnSchließenPressed()
    {
        _uiManager.AllesSchließen();
        _canvas.Leeren();
    }

    private void OnSpeichernPressed()
    {
        _aktuellerModus = FileDialog.FileModeEnum.SaveFile;
        _fileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
        _fileDialog.PopupCentered(new Vector2I(800, 600));
        _passthroughManager.PassthroughSperren();
    }

    private void OnLadenPressed()
    {
        _aktuellerModus = FileDialog.FileModeEnum.OpenFile;
        _fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        _fileDialog.PopupCentered(new Vector2I(800, 600));
        _passthroughManager.PassthroughSperren();
    }

    private void OnDateiGewählt(string pfad)
    {
        _passthroughManager.PassthroughEntsperren();
        if (_aktuellerModus == FileDialog.FileModeEnum.SaveFile)
            _canvas.GetImage().SavePng(pfad);
        else
        {
            var image = Image.LoadFromFile(pfad);
            _canvas.SetzeImage(image);
        }
    }

    private void OnFileDialogCanceled()
    {
        _passthroughManager.PassthroughEntsperren();
    }

    private void OnFaltenPressed()
    {
        if (_empfängerButton.EmpfängerId == null) return;

        var bild = _canvas.GetImage();
        try
        {
            var scene = GD.Load<PackedScene>("res://scenes/papierflugzeug.tscn");
            if (scene == null)
            {
                GD.PrintErr("Scene nicht gefunden!");
                return;
            }
            var flieger = GD.Load<PackedScene>("res://scenes/papierflugzeug.tscn").Instantiate<Papierflugzeug>();
        GetTree().CurrentScene.AddChild(flieger);

        // Links vom Bildschirm spawnen
        var kamera = GetViewport().GetCamera3D();
        var screenSize = GetViewport().GetVisibleRect().Size;
        var from = kamera.ProjectRayOrigin(new Vector2(0, screenSize.Y / 2));
        var richtung = kamera.ProjectRayNormal(new Vector2(0, screenSize.Y / 2));
        float t = (0f - from.Z) / richtung.Z;
        var spawnPos = from + richtung * t;
        spawnPos.X -= 2f;

        flieger.GlobalPosition = spawnPos;
        flieger.LinearVelocity = new Vector3(5f, 0, 0);
        flieger._image = bild;
        flieger.SetEmpfänger(_empfängerButton.EmpfängerId, _empfängerButton.IstGruppe);

        _uiManager.AllesSchließen();
        }
        catch (Exception e)
        {
            GD.PrintErr("Fehler beim Spawnen: " + e.Message);
        }
    }
}