using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared actual-control acceptance. Compilation does not execute this UI.
    private static void CheckObjectManagerCommands(bool native,string language)
    {
        var document=NativeMapDocument.CreateNewMap(new("ManagerCommands","ManagerCommands",16,14,NativeFormat:native));
        document.Paint([new(3,3)],new(4,2,1));
        var before=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var prior=document.GetHistory();
        var workspace=new EditorWorkspace {Document=document};var host=new Window {Content=workspace};
        var manager=workspace.CreateObjectManagerWindow()!;ShowManagerCallerWindow(manager);
        Button UndoButton()=>(Button)manager.FindName("ManagerUndo");
        Expect(!UndoButton().IsEnabled&&!manager.UndoManagerCommand(),"Fresh manager claimed prior document history.");
        Expect(((Button)manager.FindName("ManagerAdd")).Content as string==LocalizationService.Get("Manager.Add"),"Manager Add label is untranslated.");
        var canceled=manager.CreateAdditionWindow()!;canceled.Cancel();
        Expect(!UndoButton().IsEnabled&&before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Canceled addition changed the map/history.");
        void Add()
        {
            var child=manager.CreateAdditionWindow()!;
            ((TextBox)child.FindName("BatchX")).Text="4";((TextBox)child.FindName("BatchY")).Text="5";
            Expect(child.BuildPreview(),"Manager addition preview failed.");((CheckBox)child.FindName("BatchConfirmed")).IsChecked=true;
            Expect(child.ApplyPreview(),"Manager addition did not apply.");child.Cancel();
            Expect(UndoButton().IsEnabled,"Applied child did not enable parent Undo.");
        }
        Add();Expect(manager.UndoManagerCommand()&&!UndoButton().IsEnabled,"Parent local Undo failed or crossed its boundary.");
        Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.GetHistory().Undo.SequenceEqual(prior.Undo),"Parent Undo lost previous data/history.");
        Add();document.Paint([new(6,6)],new(5,2,1));var foreign=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
        Expect(!UndoButton().IsEnabled&&!manager.UndoManagerCommand()&&foreign.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Parent Undo accepted an unrelated edit.");
        document.Undo();Expect(manager.UndoManagerCommand(),"Restored manager head could not be undone.");
        LocalizationService.SetLanguage(language=="en"?"ru":"en");
        Expect(((Button)manager.FindName("ManagerAdd")).Content as string==LocalizationService.Get("Manager.Add"),"Live manager translation failed.");
        manager.Cancel();Expect(!manager.UndoManagerCommand(),"Closed manager retained Undo authority.");
        var reopened=workspace.CreateObjectManagerWindow()!;Expect(!reopened.UndoManagerCommand(),"Reopened manager inherited a prior local count.");reopened.Cancel();
        host.Close();LocalizationService.SetLanguage(language);
    }
}
