using Godot;

public partial class EmpfängerButton : Button
{
    private EmpfängerAuswahl _empfängerAuswahl;

    [Signal] public delegate void EmpfängerGesetztEventHandler(string id, bool istGruppe, string name);

    public string EmpfängerId { get; private set; }
    public bool IstGruppe { get; private set; }

    public override void _Ready()
    {
        _empfängerAuswahl = GetNode<EmpfängerAuswahl>("/root/Main/PaperUI/CanvasLayer/ControlRecipients");
        
        Pressed += () => _empfängerAuswahl.Öffne();

        _empfängerAuswahl.EmpfängerGewählt += (id, istGruppe, name) =>
        {
            EmpfängerId = id;
            IstGruppe = istGruppe;
            Text = name;
            EmitSignal(SignalName.EmpfängerGesetzt, id, istGruppe, name);
        };

        Text = "Kein Empfänger";
    }
}