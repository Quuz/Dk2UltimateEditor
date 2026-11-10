using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckScenarioAreaHints(bool native, string language)
    {
        var doc = NativeMapDocument.CreateNewMap(new("AreaHintUI", "AreaHintUI", 20, 20, NativeFormat: native));
        NativeRecord Add(string type) => native ? doc.AddRecord(RecordTemplate.All.Single(item => item.Structure == type), new(2, 3))
            : doc.AddLegacyRecord(type == "GAMEAREA" ? "Area" : type, new(2, 3));
        var first = Add("GAMEAREA"); var second = Add("GAMEAREA");
        doc.SetMapRegionBounds(first, new(2, 3, 1, 4)); doc.SetMapRegionBounds(second, new(6, 7, 2, 3));
        var owners = doc.ScriptOwners().Where(owner => owner.Scope == 4).ToArray();
        NativeRecord Condition()
        {
            var record = Add("Trigger"); doc.SetEventType(record, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_CLAIMAREA"));
            return doc.Records().Single(item => item.File == record.File && item.Offset == record.Offset && item.Length == record.Length);
        }
        var firstParent = Condition(); var secondParent = Condition(); var child = Condition();
        var firstParentID = ScriptGraph.ID(firstParent); var secondParentID = ScriptGraph.ID(secondParent); var childID = ScriptGraph.ID(child);
        doc.BindScriptScope(owners.Single(owner => owner.Record.Offset == first.Offset), firstParentID);
        doc.BindScriptScope(owners.Single(owner => owner.Record.Offset == second.Offset), secondParentID);
        var graph = ScriptGraph.Build(doc);
        firstParent = graph.Events[firstParentID].Record; secondParent = graph.Events[secondParentID].Record; child = graph.Events[childID].Record;
        foreach (var parent in new[] { firstParent, secondParent })
            doc.SetScriptLink(parent, parent.Fields.Single(field => field.Name == (native ? "ChildAnd" : "and")), ScriptGraph.ID(child));
        var workspace = new EditorWorkspace { Document = doc };
        Control<TabControl>(workspace, "WorkspaceTabs").SelectedIndex = 2;
        var tree = Control<TreeView>(workspace, "ScriptTree");
        T? Optional<T>(object item, string property) where T : class => item.GetType().GetProperty(property)?.GetValue(item) as T;
        void Layout() { workspace.Measure(new Size(1200, 800)); workspace.Arrange(new Rect(0, 0, 1200, 800)); workspace.UpdateLayout(); }
        Layout();
        var branch = tree.Items.Cast<object>().Single(node => Optional<ScriptEdge>(node, "ScopeRoot") is { Scope: 4 } root
            && doc.OwnerForScriptScope(root)?.Record.Offset == second.Offset);
        var container = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(branch); container.IsExpanded = true; Layout();
        Expect(SelectExistingTreeEvent(container, ScriptGraph.ID(child)), "Shared descendant could not be selected under the second AREA.");
        var before = doc.Files.ToDictionary(file => file, file => file.Data.ToArray()); var history = doc.GetHistory().Undo.Count;
        void Exact() => Expect(before.All(pair => pair.Key.Data.Span.SequenceEqual(pair.Value)) && doc.GetHistory().Undo.Count == history, "Informational AREA context changed live bytes/history.");
        var wizard = workspace.CreateScenarioEditingWindow(child)!;
        Expect(wizard.Next() && wizard.ParameterEditor is not null, "AREA editing page unavailable.");
        string Hint() => ((TextBlock)wizard.ParameterEditor!.FindName("ScenarioAreaMetrics")).Text;
        string Total(int value) => string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("ScenarioPage.AreaTotal"), value);
        Expect(Hint().StartsWith(Total(6), StringComparison.Ordinal), "Nested second AREA selection became the first shared owner.");
        var picker = (ComboBox)wizard.ParameterEditor!.FindName("ScenarioAreaContext");
        picker.SelectedItem = picker.Items.Cast<object>().Single(item => Optional<ScenarioAreaOwnerContext>(item, "Context")?.OriginalOwner.Record.Offset == first.Offset);
        Expect(Hint().StartsWith(Total(4), StringComparison.Ordinal) && Hint().EndsWith(string.Format(CultureInfo.CurrentCulture, LocalizationService.Get("ScenarioPage.AreaPerimeter"), 1), StringComparison.Ordinal), "Source single-column AREA hint differs.");
        Expect(wizard.Back() && wizard.Next() && Hint().StartsWith(Total(4), StringComparison.Ordinal), "Back/Next lost the chosen AREA context.");
        LocalizationService.SetLanguage(language == "ru" ? "en" : "ru");
        Expect(Hint().StartsWith(Total(4), StringComparison.Ordinal), "AREA context was lost or untranslated.");
        LocalizationService.SetLanguage(language); Exact(); wizard.Close(); Exact();
        var staleWizard = workspace.CreateScenarioEditingWindow(child)!;
        Expect(staleWizard.Next(), "Second AREA page could not open.");
        doc.SetScenarioName(ScenarioNameKind.Variable, 0, "External change");
        LocalizationService.SetLanguage(language == "ru" ? "en" : "ru");
        Expect(!((ComboBox)staleWizard.ParameterEditor!.FindName("ScenarioAreaContext")).IsEnabled && !staleWizard.Finish(), "Stale translated AREA context retained authority.");
        LocalizationService.SetLanguage(language); staleWizard.Close();
        workspace.EndScenarioNamesLifetime();
        Console.WriteLine($"PASS {language} {(native ? "native" : "project")} nested/shared AREA ownership, private wizard information, source width1 perimeter, retained navigation, EN/RU and Cancel");
    }
}
