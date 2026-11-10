using System.Windows;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private ScriptOwner? SelectedScenarioAreaOwner(NativeRecord? record, ScriptEdge? insertion = null)
    {
        if (document is null || ScriptTree.SelectedItem is not ScriptNode { ScopeRoot: { Scope: 4 } root } node) return null;
        var matches = record is not null ? node.EventID == ScriptGraph.ID(record)
            : insertion is not null && (ScriptGraph.IsEvent(insertion.Owner) ? node.EventID == ScriptGraph.ID(insertion.Owner)
                : root.Owner.File == insertion.Owner.File && root.Owner.Offset == insertion.Owner.Offset && root.Field.Name == insertion.Field.Name);
        if (!matches) return null;
        try { return document.OwnerForScriptScope(root); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException) { return null; }
    }
    private ScenarioParameterWindow? scenarioParameterWindow;
    public Action<ScenarioParameterWindow>? ScenarioParameterPresenter { get; set; }
    private void CloseScenarioParameterWindow() { scenarioParameterWindow?.Close(); scenarioParameterWindow = null; }
    private void RefreshScenarioParameter(NativeRecord? record)
    {
        var type = record is null ? null : EntityReferences.EventType(record);
        var available = type is not null && ScenarioControlPages.Supports(type.Name)
            && (!NativeMapDocument.HasScenarioAliasPage(type.Name) || document?.CanEditScenarioNames == true);
        ScenarioParameterButton.Visibility = available ? Visibility.Visible : Visibility.Collapsed;
        ScenarioEditEventButton.Visibility = type is not null ? Visibility.Visible : Visibility.Collapsed;
        ScenarioEditEventButton.IsEnabled = CanEditScenarioEvent(record);
        ScenarioParameterButton.IsEnabled = available && document is not null && !scenarioNamesOwnerClosed
            && (record!.Category.StartsWith("Project.", StringComparison.Ordinal) || record.File.Data.Length >= 8 && System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(record.File.Data.Span[4..]) == 4);
    }
    public ScenarioParameterWindow? CreateScenarioParameterWindow()
    {
        if (scenarioNamesOwnerClosed || document is null || !CommitFields() || RecordList.SelectedItem is not RecordView selected) return null;
        var type = EntityReferences.EventType(selected.Record);
        if (type is null || !ScenarioControlPages.Supports(type.Name)
            || NativeMapDocument.HasScenarioAliasPage(type.Name) && !document.CanEditScenarioNames) return null;
        var source = document; var context = scenarioNamesContext; var resources = GameResources;
        var window = new ScenarioParameterWindow(source, selected.Record, () => !scenarioNamesOwnerClosed && context == scenarioNamesContext && ReferenceEquals(source, document) && ReferenceEquals(resources, GameResources), CommitFields, gameResources: resources,
            preferredAreaOwner: SelectedScenarioAreaOwner(selected.Record));
        window.SpawnPointRequester = parent => BeginScenarioSpawnPoint(parent, source, context);
        window.SpatialPointRequester = parent => BeginScenarioSpatialPoint(parent, source, context);
        return window;
    }
    private void BeginScenarioSpawnPoint(ScenarioParameterWindow parent, NativeMapDocument source, long context)
    {
        if (scenarioNamesOwnerClosed || context != scenarioNamesContext || !ReferenceEquals(source, document)) return;
        CancelRegionEditing(); Canvas.BeginRegionPick(true);
        void Cleanup() { Canvas.RegionPicked -= Picked; Canvas.RegionPickCanceled -= Canceled; parent.CancelSpawnPointPick = null; }
        void Picked(object? sender, MapRegionPickedEventArgs args)
        {
            Cleanup(); if (!scenarioNamesOwnerClosed && context == scenarioNamesContext && ReferenceEquals(source, document)) parent.ApplySpawnPoint(new(args.Bounds.X, args.Bounds.Y));
        }
        void Canceled(object? sender, EventArgs args) => Cleanup();
        Canvas.RegionPicked += Picked; Canvas.RegionPickCanceled += Canceled;
        parent.CancelSpawnPointPick = () => { Canvas.CancelRegionPick(); Cleanup(); };
        WorkspaceTabs.SelectedIndex = 0; ShowRegionStatus("ScenarioSpawn.PickPoint");
    }
    private void ScenarioParameter_Click(object sender, RoutedEventArgs args)
    {
        if (scenarioParameterWindow is not null) { scenarioParameterWindow.Activate(); return; }
        var window = CreateScenarioParameterWindow(); if (window is null) return;
        scenarioParameterWindow = window;
        window.Closed += (_, _) => { if (ReferenceEquals(scenarioParameterWindow, window)) scenarioParameterWindow = null; };
        if (ScenarioParameterPresenter is { } present) present(window);
        else { window.Owner = Window.GetWindow(this); window.Show(); }
    }
    private void BeginScenarioSpatialPoint(ScenarioParameterWindow parent, NativeMapDocument source, long context, bool rectangle = false, Action<TileRect>? acceptRectangle = null)
    {
        if (scenarioNamesOwnerClosed || context != scenarioNamesContext || !ReferenceEquals(source, document)) return;
        CancelRegionEditing(); Canvas.BeginRegionPick(!rectangle);
        void Cleanup() { Canvas.RegionPicked -= Picked; Canvas.RegionPickCanceled -= Canceled; parent.CancelSpawnPointPick = null; }
        void Picked(object? sender, MapRegionPickedEventArgs args)
        {
            Cleanup();
            if (!scenarioNamesOwnerClosed && context == scenarioNamesContext && ReferenceEquals(source, document))
            { if (rectangle) acceptRectangle?.Invoke(args.Bounds); else parent.ApplySpatialPoint(new(args.Bounds.X, args.Bounds.Y)); }
        }
        void Canceled(object? sender, EventArgs args) => Cleanup();
        Canvas.RegionPicked += Picked; Canvas.RegionPickCanceled += Canceled;
        parent.CancelSpawnPointPick = () => { Canvas.CancelRegionPick(); Cleanup(); };
        WorkspaceTabs.SelectedIndex = 0; ShowRegionStatus(rectangle ? "ScenarioSpatial.PickRectangle" : "ScenarioSpawn.PickPoint");
    }
}
