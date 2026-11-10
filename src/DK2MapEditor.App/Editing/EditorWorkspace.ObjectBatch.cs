using System.Windows;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private ObjectBatchWindow? objectBatchWindow;
    private ObjectManagerWindow? objectManagerWindow;
    private readonly ObjectManagerSession objectManagerSession;
    internal EditorRequestPreferences RequestPreferences=>objectManagerSession.RequestPreferences;
    public ObjectBatchWindow? CreateObjectBatchWindow()
    {
        if (document is null || !CommitFields()) return null;
        var source = document;
        var template = TemplatePicker.SelectedItem as TemplateView;
        var kind = (template?.LegacyKind ?? template?.Template?.Structure) switch
        {
            "Door" or "GAMEDOOR" => PlacedThingKind.Door, "Trap" or "GAMETRAP" => PlacedThingKind.Trap,
            "Object" or "GAMEOBJECT" => PlacedThingKind.Object, "MagicalObject" => PlacedThingKind.SpecialItem, _ => PlacedThingKind.Creature
        };
        CloseObjectBatchWindow();
        kind = objectManagerSession.Addition.LastKind ?? kind; kind = objectManagerSession.KindFor(kind);
        var window = new ObjectBatchWindow(source, Canvas.CursorTile, () => Canvas.Selection,
            () => ReferenceEquals(source, document) && CommitFields(), objectManagerSession.Addition, kind, managerSession: objectManagerSession);
        objectBatchWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(objectBatchWindow, window)) objectBatchWindow = null; };
        return window;
    }
    private void ObjectBatch_Click(object sender, RoutedEventArgs e)
    {
        var window = CreateObjectBatchWindow(); if (window is null) return;
        CancelRegionEditing(); window.Owner = Window.GetWindow(this); window.ShowDialog();
    }
    private void CloseObjectBatchWindow() { objectBatchWindow?.Cancel(); objectBatchWindow = null; }
    public Action<ObjectManagerWindow>? ObjectManagerPresenter{get;set;}
    public ObjectManagerWindow? CreateObjectManagerWindow(int? family=null)
    {
        if(family is <0 or >3)throw new ArgumentOutOfRangeException(nameof(family));
        if (document is null || document.HasActivePenStroke || !CommitFields()) return null;
        var source = document; CloseObjectManagerWindow();
        if(family is {} chosen)objectManagerSession.Family=chosen;
        var window = new ObjectManagerWindow(source, () => Canvas.Selection, () => ReferenceEquals(source, document) && CommitFields(), objectManagerSession,
            ownerCurrent:()=>ReferenceEquals(source,document),viewRequester:(parent,point)=>ViewObjectManagerPoint(parent,source,point),
            pointRequester:parent=>BeginObjectManagerPoint(parent,source));
        objectManagerWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(objectManagerWindow, window)) objectManagerWindow = null; };
        return window;
    }
    private void ObjectManager_Click(object sender, RoutedEventArgs e)=>RequestObjectManager();
    public bool RequestObjectManager(int? family=null)
    {
        var window = CreateObjectManagerWindow(family); if (window is null) return false;
        CancelRegionEditing();
        if(ObjectManagerPresenter is {} present)present(window);else{window.Owner = Window.GetWindow(this); window.ShowDialog();}
        return true;
    }
    private void CloseObjectManagerWindow() { objectManagerWindow?.Cancel(); objectManagerWindow = null; }
}
