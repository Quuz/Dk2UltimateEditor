using System.Windows;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private ScenarioCreationContext? scenarioCreationContext;
    private ScenarioCreationWindow? scenarioCreationWindow;
    public Action<ScenarioCreationWindow>? ScenarioCreationPresenter { get; set; }
    public Action<ScenarioCreationWindow>? ScenarioEditingPresenter { get; set; }
    private void CloseScenarioCreationWindow()
    { scenarioCreationWindow?.Close(); scenarioCreationWindow = null; scenarioCreationContext = null; }
    public ScenarioCreationWindow? CreateScenarioCreationWindow(uint signature,
        LegacyEventSchemas.EventType? type = null, ScriptEdge? insertion = null)
        => CreateScenarioWizard(signature, type, insertion, null);

    private bool CanEditScenarioEvent(NativeRecord? record) => !scenarioNamesOwnerClosed && document is not null
        && record is not null && ScriptGraph.IsEvent(record) && EntityReferences.EventType(record) is not null
        && (document.IsLegacyProject ? record.Length == 56 : record.Length == 24 && record.File.Length >= 8
            && System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[4..]) == 4)
        && document.Records().Any(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length)
        && ScriptGraph.Build(document).DuplicateIDs.Count == 0;

    public ScenarioCreationWindow? CreateScenarioEditingWindow(NativeRecord? record = null)
    {
        if (scenarioNamesOwnerClosed || document is null) return null;
        var source = document; var context = scenarioNamesContext; var resources = GameResources;
        if (!CommitFields() || scenarioNamesOwnerClosed || context != scenarioNamesContext
            || !ReferenceEquals(source, document) || !ReferenceEquals(resources, GameResources)) return null;
        record ??= (RecordList.SelectedItem as RecordView)?.Record;
        if (!CanEditScenarioEvent(record)) return null;
        return CreateScenarioWizard(EntityReferences.EventType(record!)!.RecordSignature, null, null, record, fieldsCommitted: true);
    }

    private ScenarioCreationWindow? CreateScenarioWizard(uint signature, LegacyEventSchemas.EventType? type,
        ScriptEdge? insertion, NativeRecord? existing, bool fieldsCommitted = false)
    {
        if (scenarioNamesOwnerClosed || document is null || !fieldsCommitted && !CommitFields()) return null;
        var source = document; var context = scenarioNamesContext; var resources = GameResources;
        scenarioCreationContext ??= source.CreateScenarioCreationContext();
        var preferredRoot = (ScriptTree.SelectedItem as ScriptNode)?.ScopeRoot;
        var preferredRootIdentity = (ScriptTree.SelectedItem as ScriptNode)?.ScopeIdentity;
        var preferredAreaOwner = SelectedScenarioAreaOwner(existing, insertion);
        var window = new ScenarioCreationWindow(source, scenarioCreationContext, signature,
            () => !scenarioNamesOwnerClosed && context == scenarioNamesContext && ReferenceEquals(source, document) && ReferenceEquals(resources, GameResources),
            CommitFields, record =>
            {
                RecordOperation(() => record);
                if (insertion is not null || existing is not null) SelectTreeEvent(ScriptGraph.ID(record), preferredRoot: preferredRoot, preferredRootIdentity: preferredRootIdentity);
            }, type, insertion, resources, existing, preferredAreaOwner);
        window.SpawnPointRequester = parent => BeginScenarioSpawnPoint(parent, source, context);
        window.SpatialPointRequester = parent => BeginScenarioSpatialPoint(parent, source, context);
        window.RectangleRequester = parent => BeginScenarioSpatialPoint(parent, source, context, rectangle: true, acceptRectangle: bounds => window.AcceptRectangle(bounds));
        return window;
    }
    private void OpenScenarioCreation(uint signature, LegacyEventSchemas.EventType? type = null, ScriptEdge? insertion = null)
    {
        if (scenarioCreationWindow is not null) { scenarioCreationWindow.Activate(); return; }
        var window = CreateScenarioCreationWindow(signature, type, insertion); if (window is null) return;
        PresentScenarioWizard(window);
    }
    private void PresentScenarioWizard(ScenarioCreationWindow window)
    {
        scenarioCreationWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(scenarioCreationWindow, window)) scenarioCreationWindow = null; };
        var presenter = window.IsEditingExisting ? ScenarioEditingPresenter : ScenarioCreationPresenter;
        if (presenter is { } present) present(window);
        else { window.Owner = Window.GetWindow(this); window.Show(); }
    }
    private void OpenScenarioEditing(NativeRecord? record = null)
    {
        if (scenarioCreationWindow is not null) { scenarioCreationWindow.Activate(); return; }
        var window = CreateScenarioEditingWindow(record); if (window is not null) PresentScenarioWizard(window);
    }
    private void ScenarioEditEvent_Click(object sender, RoutedEventArgs args) => OpenScenarioEditing();
    private void ScriptEditEvent_Click(object sender, RoutedEventArgs args)
    {
        if (ScriptTree.SelectedItem is ScriptNode { EventID: not 0 } node
            && scriptGraph?.Events.TryGetValue(node.EventID, out var item) == true) OpenScenarioEditing(item.Record);
    }
}
