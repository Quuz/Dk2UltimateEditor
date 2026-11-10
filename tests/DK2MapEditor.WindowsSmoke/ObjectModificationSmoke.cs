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
    private static void CheckObjectModification(bool native, string language)
    {
        var doc = NativeMapDocument.CreateNewMap(new("ModifyUI", "ModifyUI", 16, 14, NativeFormat: native));
        var template = ObjectBatchTemplate.Default(PlacedThingKind.Creature) with { Owner = 3, Objective = 0, Health = 72, GoldHeld = 13 };
        foreach (var position in new[] { new TilePoint(3, 4), new TilePoint(6, 7) })
            doc.ApplyObjectBatchInsertion(doc.PlanObjectBatchInsertion(template, new(Position: position, Flags: (ObjectBatchFlags)127)), [0]);
        var owner = doc.ScriptOwners().First(owner => owner.Scope == 2); var binding = doc.EnsureScriptScope(owner);
        doc.InsertScriptEvent(binding.Owner, binding.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMER"));
        var workspace = new EditorWorkspace { Document = doc }; var host = new Window { Content = workspace };
        workspace.SetSelection(new(1, 1, 8, 8)); workspace.Copy(); var buffer = workspace.BufferedSelection;
        var manager = workspace.CreateObjectManagerWindow()!; var list = (DataGrid)manager.FindName("ManagerObjects"); list.SelectAll();
        manager.ConfirmationPresenter=dialog=>Expect(dialog.Accept(),"Source modification Yes refused");
        Expect(((Button)manager.FindName("ManagerModify")).Content as string == LocalizationService.Get("Modify.Title"), "Modification manager command untranslated.");
        var window = manager.CreateModificationWindow()!;ShowManagerCallerWindow(window);
        T Field<T>(string name) => (T)window.FindName(name);
        void Click(string name) => Field<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact() => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)), "Modification UI draft/undo changed unexpected bytes.");
        Expect(window.Title == LocalizationService.Get("Modify.Title") && Field<ComboBox>("BatchFamily").Items.Count == 1, "Creature family restriction wrong.");
        // Original first-run masks include all fields except coordinates. This fixture
        // deliberately requests health-only editing and retains its unmasked-gold assertion.
        foreach (var mask in Enum.GetValues<ObjectModificationMask>().Where(mask => mask is not (ObjectModificationMask.None or ObjectModificationMask.All)))
            if (window.FindName("ModifyMask" + mask) is CheckBox check) check.IsChecked = false;
        Field<CheckBox>("ModifyMaskHealth").IsChecked = true; Field<TextBox>("BatchHealth").Text = "93";
        Field<TextBox>("BatchGoldHeld").Text = "999";
        Click("BatchPreview"); Expect(window.ModificationPreview?.Candidates.Count == 2 && window.ModificationPreview.Candidates.Count(candidate=>candidate.Source.Scripted)==1,"Source Yes failed to include the bound object");Exact();
        Expect(Field<CheckBox>("ModifyIncludeScripted").IsChecked==true && Field<CheckBox>("ModifyIncludeScripted").Visibility==Visibility.Collapsed,"Child must use the resolved source request instead of asking again");
        Field<CheckBox>("ModifyMaskCoordinates").IsChecked = true; Field<TextBox>("BatchX").Text = "7"; Field<TextBox>("BatchY").Text = "3";
        Field<CheckBox>("ModifyMaskCreatureOptions").IsChecked = true;
        Field<ComboBox>("ModifyCreatureBits").SelectedValue = "1"; Field<TextBox>("ModifyCreatureMask").Text = "4"; Field<TextBox>("BatchCreatureOptions").Text = "4";
        Click("BatchPreview"); Click("BatchSelectAll"); var plan = window.ModificationPreview!; Expect(plan.Candidates.Count == 2 && plan.Candidates.All(candidate => candidate.Template.CreatureOptions == 13), "Masked creature flags wrong in actual UI.");
        Field<CheckBox>("BatchConfirmed").IsChecked = true;
        LocalizationService.SetLanguage(language == "en" ? "ru" : "en");
        Expect(ReferenceEquals(plan, window.ModificationPreview) && Field<CheckBox>("BatchConfirmed").IsChecked == true && Field<TextBox>("BatchHealth").Text == "93", "Translation discarded modification draft/consent.");
        LocalizationService.SetLanguage(language); Exact();
        window.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);window.UpdateLayout();
        var table = Field<DataGrid>("BatchCandidates"); var rows = table.Items.Cast<ObjectBatchWindow.ModificationRow>().ToArray();
        Expect(rows.Length == 2 && rows.All(row => row.Choices.All(choice => !choice.Label.StartsWith("Modify.", StringComparison.Ordinal))), "Decision choices untranslated.");
        if (Environment.GetEnvironmentVariable("DK2_OBJECT_MODIFICATION_UI_ARTIFACTS") is { Length: > 0 } artifacts)
        {
            CaptureManagerCallerWindow(window,artifacts,$"modify-{language}-{native}.png",
                "BatchCandidates","BatchPreview","BatchApply","BatchConfirmed","ModifyMaskHealth");
        }
        rows[1].Decision = 0; Expect(Field<CheckBox>("BatchConfirmed").IsChecked == false, "Per-source Keep retained consent.");
        rows[1].Decision = 2; Field<CheckBox>("BatchConfirmed").IsChecked = true;
        var history = doc.GetHistory().Undo.Count;var modificationSourceCount=doc.EditorChangeCount; Click("BatchApply");
        Expect(doc.EditorChangeCount==modificationSourceCount+2,"Actual two-object modification counted as one.");
        Expect(doc.ObjectManagerEntries().All(entry => entry.Record.Read(entry.Record.Fields.Single(field => field.Name == (native ? "dwGlobalHealth" : "health"))) == "93")
            && doc.ObjectManagerEntries().All(entry => entry.Record.Read(entry.Record.Fields.Single(field => field.Name == (native ? "bGlobalGoldHeld" : "goldheld"))) == "13")
            && doc.GetHistory().Undo.Count == history + 1 && ScriptGraph.Build(doc).Events.Count == 1, "Masked group apply changed unselected gold/history/scenario.");
        Expect(Field<TextBlock>("BatchStatus").Text == string.Format(LocalizationService.Get("Modify.Applied"), 2), "Applied status untranslated/unformatted.");
        doc.Undo();Exact();Expect(doc.EditorChangeCount==modificationSourceCount,"Modification Undo source count wrong.");
        Expect(!window.BuildPreview() && window.ModificationPreview is null, "Changed selected identities allowed reuse of stale physical selection.");
        window.Cancel(); list.SelectAll(); window = manager.CreateModificationWindow()!;
        foreach(var mask in Enum.GetValues<ObjectModificationMask>().Where(mask=>mask is not(ObjectModificationMask.None or ObjectModificationMask.All)))
            if(window.FindName("ModifyMask"+mask) is CheckBox check)check.IsChecked=false;
        Field<CheckBox>("ModifyMaskHealth").IsChecked=true;Field<TextBox>("BatchHealth").Text="72";
        Field<CheckBox>("ModifyIncludeScripted").IsChecked=true;Click("BatchPreview");Click("BatchSelectAll");
        Field<CheckBox>("BatchConfirmed").IsChecked=true;var equalHistory=doc.GetHistory();Click("BatchApply");
        Expect(doc.EditorChangeCount==modificationSourceCount+2&&doc.GetHistory().Undo.Count==equalHistory.Undo.Count+1
            &&doc.GetHistory().Redo.Count==0&&Field<TextBlock>("BatchStatus").Text==string.Format(LocalizationService.Get("Modify.Applied"),2),
            "Equal actual manager Apply did not retain Undo/count, clear redo and report acceptance.");Exact();doc.Undo();Exact();
        Expect(doc.EditorChangeCount==modificationSourceCount,"Equal manager Undo count wrong.");
        window.Cancel();list.SelectAll();window=manager.CreateModificationWindow()!;
        Field<CheckBox>("ModifyMaskHealth").IsChecked = true; Field<TextBox>("BatchHealth").Text = "94"; Click("BatchPreview");
        Field<CheckBox>("BatchConfirmed").IsChecked = true; manager.Cancel();
        Expect(window.ModificationPreview is null && !window.ApplyPreview(), "Manager close left live child mutation callback."); Exact();
        Expect(ReferenceEquals(buffer, workspace.BufferedSelection), "Bulk modification changed copy buffer.");
        host.Close();
        // Actual noncreature decision editor rechecks a kept source before replacement.
        var objects = NativeMapDocument.CreateNewMap(new("CollisionUI", "CollisionUI", 12, 10, NativeFormat: native));
        var item = ObjectBatchTemplate.Default(PlacedThingKind.Object);
        foreach (var position in new[] { new TilePoint(2, 2), new TilePoint(3, 2) })
            objects.ApplyObjectBatchInsertion(objects.PlanObjectBatchInsertion(item, new(Position: position, Flags: (ObjectBatchFlags)127)), [0]);
        var objectWorkspace = new EditorWorkspace { Document = objects }; var objectHost = new Window { Content = objectWorkspace };
        var objectManager = objectWorkspace.CreateObjectManagerWindow()!; ((ComboBox)objectManager.FindName("ManagerFamily")).SelectedValue = 2;
        ((DataGrid)objectManager.FindName("ManagerObjects")).SelectAll(); window = objectManager.CreateModificationWindow()!;
        Expect(Field<ComboBox>("BatchFamily").Items.Count == 2 && !Field<ComboBox>("ModifyCoordinates").IsEnabled, "Object/special conversion or forced relative mode unavailable.");
        Field<CheckBox>("ModifyMaskCoordinates").IsChecked = true; Field<TextBox>("BatchX").Text = "3"; Field<TextBox>("BatchY").Text = "2"; Click("BatchPreview");
        Click("BatchSelectAll"); rows = Field<DataGrid>("BatchCandidates").Items.Cast<ObjectBatchWindow.ModificationRow>().ToArray(); rows[1].Decision = 0;
        Expect(rows[0].Decision == 0 && rows[0].Candidate.Replacement is not null, "Kept source did not force new actual UI replacement choice.");
        rows[0].Decision = 2; Field<CheckBox>("BatchConfirmed").IsChecked = true;var collisionCount=objects.EditorChangeCount;Click("BatchApply");
        Expect(objects.ObjectManagerEntries().Single().Thing.Position == new TilePoint(3, 2)&&objects.EditorChangeCount==collisionCount+2,
            "Explicit collision replacement UI failed or omitted the replacement removal count.");objects.Undo();
        Expect(objects.EditorChangeCount==collisionCount,"Replacement Undo count wrong.");
        objectManager.Cancel(); objectHost.Close();
        Console.WriteLine($"PASS WPF masked object modification/translation {language} {(native ? "native" : "project")}");
    }
}
