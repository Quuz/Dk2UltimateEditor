using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckManagerCategoryScope(bool native,string language)
    {
        var d=NativeMapDocument.CreateNewMap(new("CategoryUI","CategoryUI",16,16,NativeFormat:native));d.ApplyEditorObjectBatchInsertion(d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Door),new(Position:new(4,5),Flags:(ObjectBatchFlags)127)),[0]);
        var root=d.EnsureScriptScope(d.ScriptOwners().Single(owner=>owner.Scope==64));d.InsertScriptEvent(root.Owner,root.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));
        var workspace=new EditorWorkspace{Document=d};var host=new Window{Content=workspace};ShowManagerCallerWindow(host);var manager=workspace.CreateObjectManagerWindow()!;ShowManagerCallerWindow(manager);
        ((ComboBox)manager.FindName("ManagerFamily")).SelectedValue=1;((DataGrid)manager.FindName("ManagerObjects")).SelectAll();
        manager.ConfirmationPresenter=dialog=>Expect(dialog.Accept(),"Category source request refused");
        var window=manager.CreateModificationWindow()!;ShowManagerCallerWindow(window);var before=d.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        var id=d.ObjectManagerEntries().Single().Identity;var eventBytes=ScriptGraph.Build(d).Events.ToDictionary(pair=>pair.Key,pair=>pair.Value.Record.File.Data.Slice(pair.Value.Record.Offset,pair.Value.Record.Length).ToArray());
        foreach(var mask in Enum.GetValues<ObjectModificationMask>().Where(mask=>mask is not(ObjectModificationMask.None or ObjectModificationMask.All)))if(window.FindName("ModifyMask"+mask) is CheckBox check)check.IsChecked=false;
        ((CheckBox)window.FindName("ModifyMaskType")).IsChecked=true;((ComboBox)window.FindName("BatchFamily")).SelectedValue="2";
        Expect(((CheckBox)window.FindName("ModifyDetachScripts")).Visibility==Visibility.Collapsed,"Source category path still offers generic point deletion consent");
        Expect(window.BuildPreview() && window.ModificationPreview!.Candidates.Single().DetachesScript==false,"Source category preview retained generic detachment warning");
        ((CheckBox)window.FindName("BatchConfirmed")).IsChecked=true;Expect(window.ApplyPreview(),"Source category apply refused retained point");
        Expect(d.ObjectManagerEntries().Single().Thing.Kind==PlacedThingKind.Trap && d.ObjectManagerEntries().Single().Identity==id
            && d.ScriptBindings().Any(binding=>binding.Root.Scope==64 && binding.Owner is null && d.HasStoredScriptScope(binding.Root)),"UI door→trap changed UID or point scope");
        var after=ScriptGraph.Build(d).Events;Expect(eventBytes.All(pair=>after[pair.Key].Record.File.Data.Slice(after[pair.Key].Record.Offset,after[pair.Key].Record.Length).Span.SequenceEqual(pair.Value)),"UI category edit changed events");
        d.Undo();Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Category UI Undo did not restore full old files");manager.Cancel();host.Close();
        Expect(LocalizationService.CurrentLanguage==language,"Category harness changed language");Console.WriteLine($"PASS WPF source category UID/point retention {language} {native}");
    }
}
