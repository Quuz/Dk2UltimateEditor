using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    public Action<ObjectBatchWindow>? ObjectBrushPresenter {get;set;}
    private readonly ObjectBatchSession objectPaintingSession = new();
    private ObjectBatchTemplate? objectBrushTemplate;
    private bool objectBrushUpgrade;
    private string? objectPaintingStatusKey;
    private ObjectBatchSession CopyPaintingTemplates()=>new(objectPaintingSession.Templates.ToDictionary(p=>p.Key,p=>(string[])p.Value.Clone()));
    private void KeepPaintingTemplate(ObjectBatchTemplate template,ObjectBatchSession draft)
        =>objectPaintingSession.Templates[template.Kind]=draft.Templates[template.Kind].ToArray();
    public bool RequestObjectBrush(PlacedThingKind kind,NativeMapDocument? expectedOwner=null)
    {
        if(!Enum.IsDefined(kind))throw new ArgumentOutOfRangeException(nameof(kind));
        if(scenarioNamesOwnerClosed||document is not {} source||source.HasActivePenStroke||expectedOwner is not null&&!ReferenceEquals(source,expectedOwner)
            ||!CommitFields()||!ReferenceEquals(document,source)||source.HasActivePenStroke)return false;
        CloseObjectBatchWindow();
        // Source Things_Select edits a copy of MapImm<kind>. Cancel must not
        // write the source template or derive its type from the Inspector.
        var draft=CopyPaintingTemplates();
        var window=new ObjectBatchWindow(source,Canvas.CursorTile,()=>Canvas.Selection,
            ()=>!scenarioNamesOwnerClosed&&ReferenceEquals(document,source)&&!source.HasActivePenStroke&&CommitFields()&&ReferenceEquals(document,source)&&!source.HasActivePenStroke,draft,kind,singlePainting:true);
        window.ConfigureBrushTemplateEditor();
        window.ObjectBrushRequested+=(template,upgrade)=>
        {
            if(!ReferenceEquals(document,source)||source.HasActivePenStroke)return;
            KeepPaintingTemplate(template,draft);
            objectBrushTemplate=template;objectBrushUpgrade=upgrade;objectPaintingStatusKey="Single.BrushReady";
            WorkspaceTabs.SelectedIndex=0;ToolPicker.SelectedValue=(int)DrawingTool.PlaceObject;RefreshPlacementStatus();
        };
        objectBatchWindow=window;window.Closed+=(_,_)=>{if(ReferenceEquals(objectBatchWindow,window))objectBatchWindow=null;};
        CancelRegionEditing();
        if(ObjectBrushPresenter is {} present)present(window);else{window.Owner=System.Windows.Window.GetWindow(this);window.ShowDialog();}
        return true;
    }
    private static PlacedThingKind? PaintingKind(TemplateView? template) => (template?.LegacyKind ?? template?.Template?.Structure) switch
    {
        "Creature" or "GAMEHERO" or "GAMENEUTRAL" or "GAMEKEEPER" => PlacedThingKind.Creature,
        "Door" or "GAMEDOOR" => PlacedThingKind.Door, "Trap" or "GAMETRAP" => PlacedThingKind.Trap,
        "Object" or "GAMEOBJECT" => PlacedThingKind.Object, "MagicalObject" => PlacedThingKind.SpecialItem, _ => null
    };

    public ObjectBatchWindow? CreateObjectPaintingWindow(TilePoint? position = null)
    {
        if (document is null || !CommitFields()) return null;
        var source = document;
        var draft=CopyPaintingTemplates();
        var kind = PaintingKind(TemplatePicker.SelectedItem as TemplateView) ?? objectBrushTemplate?.Kind ?? PlacedThingKind.Creature;
        // Native GAMEOBJECT shares both catalogues, unlike the separate project templates.
        var selectedID = (ElementPicker.SelectedItem as NamedChoiceView)?.Choice.Value;
        if (kind == PlacedThingKind.Object && uint.TryParse(selectedID, out var id) && id is >= 117 and <= 129) kind = PlacedThingKind.SpecialItem;
        if (selectedID is not null)
        {
            if (!draft.Templates.TryGetValue(kind, out var values))
            {
                var value = ObjectBatchTemplate.Default(kind);
                values = new[] { value.TypeID.ToString(), value.Owner.ToString(), value.Health.ToString(), value.GoldHeld.ToString(), value.DataAll.ToString(),
                    value.HeroOptions.ToString(), value.CreatureOptions.ToString(), value.Objective.ToString(), value.Area.ToString(), value.Enemy.ToString() };
                draft.Templates[kind] = values;
            }
            values[0] = selectedID;
        }
        CloseObjectBatchWindow();
        var window = new ObjectBatchWindow(source, position ?? Canvas.CursorTile, () => Canvas.Selection,
            () => ReferenceEquals(source, document) && CommitFields(), draft, kind, singlePainting: true);
        window.ObjectPaintingTemplateAccepted+=template=>{if(ReferenceEquals(source,document))KeepPaintingTemplate(template,draft);};
        window.ObjectBrushRequested += (template, upgrade) =>
        {
            if (!ReferenceEquals(source, document)) return;
            KeepPaintingTemplate(template,draft);
            objectBrushTemplate = template; objectBrushUpgrade = upgrade; objectPaintingStatusKey = "Single.BrushReady";
            ToolPicker.SelectedValue = (int)DrawingTool.PlaceObject; RefreshPlacementStatus();
        };
        objectBatchWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(objectBatchWindow, window)) objectBatchWindow = null; };
        return window;
    }

    private void PaintSingleObject(TilePoint position)
    {
        if (document is null || !CommitFields()) return;
        try
        {
            PlacementStatus.ToolTip = null;
            if (objectBrushTemplate is null) { objectPaintingStatusKey = "Single.ConfigureBrush"; RefreshPlacementStatus(); return; }
            var preview = document.PlanObjectPainting(objectBrushTemplate, position, objectBrushUpgrade);
            if (preview.Locked) { objectPaintingStatusKey = "Single.Blocked"; RefreshPlacementStatus(); return; }
            PlayObjectPaintingSound(objectBrushTemplate);
            // Bindings are never silently detached by the live brush: review them in the single-object dialog.
            if (preview.Removals.Any(entry => entry.Scripted)) { objectPaintingStatusKey = "Single.ScriptReview"; RefreshPlacementStatus(); return; }
            document.ApplyEditorObjectPainting(preview);
            objectPaintingStatusKey = preview.Permission switch
            { ThingTerrainPermission.Enemy => "Single.BrushEnemy", ThingTerrainPermission.Fatal => "Single.BrushFatal", _ => "Single.BrushApplied" }; RefreshPlacementStatus();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.IO.IOException or OverflowException)
        { objectPaintingStatusKey = "Batch.Failed"; PlacementStatus.ToolTip = exception.Message; RefreshPlacementStatus(); }
    }
}
