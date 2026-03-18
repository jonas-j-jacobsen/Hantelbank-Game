using Godot;

public partial class AktionsBar : DraggablePanel
{
    private UIManager _uiManager;
    private DrawingCanvas _canvas;
    private FileDialog _fileDialog;

    public override void _Ready()
    {
        _uiManager = GetNode<UIManager>("/root/Main/PaperUI/CanvasLayer");
        _canvas = GetNode<DrawingCanvas>("/root/Main/PaperUI/CanvasLayer/ControlEasel/DrawingCanvas");

        _fileDialog = new FileDialog();
        _fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        _fileDialog.Filters = new string[] { "*.png ; PNG Bilder" };
        AddChild(_fileDialog);

        GetNode<Button>("/root/Main/PaperUI/CanvasLayer/ControlActions/HBoxContainer/Schließen").Pressed += OnSchließenPressed;
        GetNode<Button>("/root/Main/PaperUI/CanvasLayer/ControlActions/HBoxContainer/Speichern").Pressed += OnSpeichernPressed;
        GetNode<Button>("/root/Main/PaperUI/CanvasLayer/ControlActions/HBoxContainer/Laden").Pressed += OnLadenPressed;
        GetNode<Button>("/root/Main/PaperUI/CanvasLayer/ControlActions/HBoxContainer/Falten").Pressed += OnFaltenPressed;
    }

    private void OnSchließenPressed()
    {
        _uiManager.AllesSchließen();
        _canvas.Leeren();
    }

    private void OnSpeichernPressed()
    {
        _fileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
        _fileDialog.PopupCentered(new Vector2I(800, 600));
        _fileDialog.FileSelected += OnSpeichernDateiGewählt;
    }

    private void OnSpeichernDateiGewählt(string pfad)
    {
        _fileDialog.FileSelected -= OnSpeichernDateiGewählt;
        _canvas.GetImage().SavePng(pfad);
        GD.Print("Gespeichert: " + pfad);
    }

    private void OnLadenPressed()
    {
        _fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        _fileDialog.PopupCentered(new Vector2I(800, 600));
        _fileDialog.FileSelected += OnLadenDateiGewählt;
    }

    private void OnLadenDateiGewählt(string pfad)
    {
        _fileDialog.FileSelected -= OnLadenDateiGewählt;
        var image = Image.LoadFromFile(pfad);
        _canvas.SetzeImage(image);
        GD.Print("Geladen: " + pfad);
    }

    private void OnFaltenPressed()
    {
        // TODO: Empfängerliste Context öffnen
        // var empfängerListe = GetNode<EmpfängerListe>("/root/Main/PaperUI/CanvasLayer/EmpfängerListe");
        // var empfänger = empfängerListe.GetAktuelleId();
        // if (empfänger == null)
        // {
        //     GD.Print("Kein Empfänger ausgewählt!");
        //     return;
        // }
        // var image = _canvas.GetImage();
        // GD.Print("Falte zu Flieger für: " + empfänger);
        // _uiManager.AllesSchließen();
    }
}