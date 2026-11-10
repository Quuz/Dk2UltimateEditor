using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckObjectPainting(bool native, string language)
    {
        var doc = NativeMapDocument.CreateNewMap(new("PaintUI", "PaintUI", 12, 10, NativeFormat: native));
        doc.Paint([new(2, 2), new(3, 3)], new(3, 2, 1));
        var trap = ObjectBatchTemplate.Default(PlacedThingKind.Trap) with { Owner = 2 };
        doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(trap, new(Position: new(2, 2), Flags: (ObjectBatchFlags)127)), [0]);
        doc.DuplicateRecord(doc.ObjectManagerEntries().Single().Record);
        var workspace = new EditorWorkspace { Document = doc }; var host = new Window { Content = workspace };
        workspace.SetSelection(new(1, 1, 4, 4)); workspace.Copy(); var buffer = workspace.BufferedSelection;
        var canvas = Control<MapCanvas>(workspace, "Canvas"); var selection = canvas.Selection;
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact() => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Single-placement UI draft/undo changed unexpected bytes.");
        var window = workspace.CreateObjectPaintingWindow(new(2, 2))!;
        T Field<T>(string name) => (T)window.FindName(name);
        void Click(string name) => Field<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Field<ComboBox>("BatchFamily").SelectedValue = "1"; Field<TextBox>("BatchTypeID").Text = "6"; Field<TextBox>("BatchOwner").Text = "0";
        Click("BatchPreview"); var plan = window.PaintingPreview!;
        Expect(window.Title == LocalizationService.Get("Single.Title") && plan.Creates && plan.Removals.Count == 2 && !plan.Locked, "Real single dialog does not implement all-opposite replacement."); Exact();
        Expect(!Field<ComboBox>("BatchMode").IsVisible && !Field<CheckBox>("BatchFlagIgnoreTerrain").IsVisible, "Painting exposes batch terrain overrides/modes.");
        Field<CheckBox>("BatchConfirmed").IsChecked = true;
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
        Expect(ReferenceEquals(plan, window.PaintingPreview) && Field<CheckBox>("BatchConfirmed").IsChecked == true, "Live translation lost single-preview consent.");
        LocalizationService.SetLanguage(language);
        var content = (FrameworkElement)window.Content; content.Measure(new Size(1056, 708)); content.Arrange(new Rect(0, 0, 1056, 708)); content.UpdateLayout();
        if (Environment.GetEnvironmentVariable("DK2_OBJECT_PAINTING_UI_ARTIFACTS") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); var bitmap = new RenderTargetBitmap(1056, 708, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = new FileStream(Path.Combine(directory, $"painting-{language}-{native}.png"), FileMode.CreateNew); encoder.Save(output);
        }
        var history = doc.GetHistory().Undo.Count; var callerCount=doc.EditorChangeCount; var paintUsage=doc.ReadActivityStatistics()!.UsageOfPaintThings; Click("BatchApply");
        Expect(doc.EditorChangeCount==unchecked(callerCount+3)&&doc.ReadActivityStatistics()!.UsageOfPaintThings==unchecked(paintUsage+1),"Single dialog missed two source removal increments or counted paint usage incorrectly.");
        Expect(doc.ObjectManagerEntries().Single().Thing.Kind == PlacedThingKind.Door && doc.GetHistory().Undo.Count == history + 1, "Single Apply did not replace both traps in one undo.");
        Expect(Field<TextBlock>("BatchStatus").Text == string.Format(LocalizationService.Get("Single.Applied"), 1), "Single applied status untranslated.");
        Click("BatchPreview"); Expect(window.PaintingPreview is { TogglesDoor: true, EffectiveTemplate.DataAll: 1 }, "Real door second-click preview does not toggle lock.");
        Field<CheckBox>("BatchConfirmed").IsChecked = true; Click("BatchApply"); Expect(doc.ObjectManagerEntries().Single().DataAll == 1, "Real dialog lock not applied.");
        Expect(doc.EditorChangeCount==unchecked(callerCount+4)&&doc.ReadActivityStatistics()!.UsageOfPaintThings==unchecked(paintUsage+2),"Door update added removal counts or missed source usage.");
        doc.Undo(); doc.Undo(); Exact();Expect(doc.EditorChangeCount==callerCount&&doc.ReadActivityStatistics()!.UsageOfPaintThings==unchecked(paintUsage+2),"Single dialog Undo lost count or rolled back cumulative usage.");
        Field<TextBox>("BatchX").Text = "1"; Field<TextBox>("BatchY").Text = "1"; Click("BatchPreview");
        Expect(window.PaintingPreview is { Locked: true }, "Single solid terrain did not lock preview."); Field<CheckBox>("BatchConfirmed").IsChecked = true; Expect(!window.ApplyPreview(), "UI applied forbidden solid placement."); Exact();
        Field<TextBox>("BatchX").Text = "2"; Field<TextBox>("BatchY").Text = "2"; Click("SingleUseBrush");
        Expect(canvas.Tool == DrawingTool.PlaceObject && window.PaintingPreview is null, "Actual Use-as-brush command not integrated with canvas.");
        canvas.PaintTile = new(14, 3, 1); Expect(canvas.PlacementPreview is null, "Object brush shows terrain-heart footprint.");
        history = doc.GetHistory().Undo.Count; callerCount=doc.EditorChangeCount; paintUsage=doc.ReadActivityStatistics()!.UsageOfPaintThings; canvas.PaintObjectAt(new(2, 2));
        Expect(doc.EditorChangeCount==unchecked(callerCount+3)&&doc.ReadActivityStatistics()!.UsageOfPaintThings==unchecked(paintUsage+1),"Live brush missed source removal counts or double-counted usage.");
        Expect(doc.ObjectManagerEntries().Single().Thing.Kind == PlacedThingKind.Door && doc.GetHistory().Undo.Count == history + 1, "Live object brush did not replace all traps.");
        canvas.PaintObjectAt(new(2, 2)); Expect(doc.ObjectManagerEntries().Single().DataAll == 1, "Live brush lock toggle differs from single dialog."); doc.Undo(); doc.Undo(); Exact();
        canvas.PaintObjectAt(new(1, 1)); Exact(); Expect(Control<TextBlock>(workspace, "PlacementStatus").Text == LocalizationService.Get("Single.Blocked"), "Blocked brush status untranslated.");
        canvas.BeginRegionPick(true); try { canvas.PaintObjectAt(new(2, 2)); throw new Exception("Object brush interfered with region picking."); } catch (InvalidOperationException) { } canvas.CancelRegionPick(); Exact();
        Expect(canvas.Selection == selection && ReferenceEquals(workspace.BufferedSelection, buffer), "Object brush changed selection or copy buffer.");
        window = workspace.CreateObjectPaintingWindow(new(3, 3))!; Field<ComboBox>("BatchFamily").SelectedValue = "0";
        Field<TextBox>("BatchOwner").Text = "0"; Field<TextBox>("BatchHealth").Text = "73"; Click("SingleUseBrush");
        canvas.PaintObjectAt(new(3, 3)); canvas.PaintObjectAt(new(3, 3)); Expect(doc.ObjectManagerEntries().Count(entry => entry.Thing.Kind == PlacedThingKind.Creature) == 2, "Live creature brush did not stack."); doc.Undo(); doc.Undo(); Exact();
        // A brush never silently detaches existing scenario bindings; the full dialog remains available.
        var scripted = native ? doc.AddRecord(RecordTemplate.All.Single(layout => layout.Structure == "GAMEOBJECT"), new(2, 2), 2) : doc.AddLegacyRecord("Object", new(2, 2), 2);
        doc.SetField(scripted, ValueCatalogues.PrimaryField(scripted)!, "1");
        var binding = doc.EnsureScriptScope(doc.ScriptOwners().Single(owner => owner.Scope == 128));
        doc.InsertScriptEvent(binding.Owner, binding.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER"));
        before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        window = workspace.CreateObjectPaintingWindow(new(2, 2))!; Field<ComboBox>("BatchFamily").SelectedValue = "4"; Field<TextBox>("BatchTypeID").Text = "117"; Field<TextBox>("BatchOwner").Text = "2"; Click("SingleUseBrush");
        canvas.PaintObjectAt(new(2, 2)); Exact(); Expect(Control<TextBlock>(workspace, "PlacementStatus").Text == LocalizationService.Get("Single.ScriptReview"), "Live brush silently removed a bound object.");
        window = workspace.CreateObjectPaintingWindow(new(2, 2))!; Field<ComboBox>("BatchFamily").SelectedValue = "4"; Field<TextBox>("BatchTypeID").Text = "117"; Field<TextBox>("BatchOwner").Text = "2"; Click("BatchPreview");
        Expect(window.PaintingPreview!.Removals.Count(entry => entry.Scripted) == 1, "Bound replacement unavailable in single dialog.");
        Field<CheckBox>("BatchConfirmed").IsChecked = true; Click("BatchApply"); Expect(ScriptGraph.Build(doc).Events.Count == 1 && doc.ObjectManagerEntries().Any(entry => entry.Thing.Kind == PlacedThingKind.SpecialItem), "Confirmed bound replacement deleted events or failed."); doc.Undo(); Exact(); window.Cancel();
        window = workspace.CreateObjectPaintingWindow(new(2, 2))!; Click("BatchPreview"); Field<CheckBox>("BatchConfirmed").IsChecked = true;
        workspace.Document = null; Expect(window.PaintingPreview is null && !window.ApplyPreview(), "Closed map left live single-placement callback."); Exact(); host.Close();
        Console.WriteLine($"PASS WPF single object painting/brush {language} {(native ? "native" : "project")}");
    }
}
