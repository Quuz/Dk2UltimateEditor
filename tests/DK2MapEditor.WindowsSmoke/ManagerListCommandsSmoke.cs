using System.IO;
using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.App.Settings;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Compile only. No GUI execution is implied by these prepared callers.
    private static void CheckManagerListCommands(bool native,string language,string directory)
    {
        var source=NativeMapDocument.CreateNewMap(new("ListCommands","List commands",16,14,NativeFormat:native));
        foreach(var kind in Enum.GetValues<PlacedThingKind>())
            source.ApplyObjectBatchInsertion(source.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(kind) with{Owner=3},
                new(Position:new(2+(int)kind,3),Flags:(ObjectBatchFlags)127)),[0]);
        var extra=source.ObjectManagerEntries().Single(e=>e.Thing.Kind==PlacedThingKind.Object).Record;
        var x=extra.Fields.First(f=>native?f.Name.EndsWith("LocationX",StringComparison.Ordinal):f.Name=="x");
        source.SetField(extra,x,"16");
        var input=source.SaveCopy(Path.Combine(directory,$"manager-list-{language}-{native}"));
        var shell=new MainWindow(new AppSettings(),persistSettings:false){DiscardChangesPrompt=()=>true,SaveNoticePresenter=_=>{}};
        var workspace=(EditorWorkspace)shell.FindName("Editor");var menus=new[]{"CreatureManagerMenuItem","DoorTrapManagerMenuItem","ItemManagerMenuItem"};
        ObjectManagerWindow? window=null;var presented=0;workspace.ObjectManagerPresenter=w=>{window=w;presented++;};
        try
        {
            Expect(menus.All(n=>!((MenuItem)shell.FindName(n)).IsEnabled),"Source manager menus enabled without a map.");
            shell.OpenMapFromPath(input);var document=workspace.Document!;
            var before=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=document.EditRevision;
            Expect(document.ObjectManagerEntries().Count==5,"Imported out-of-map fixture was silently removed.");
            for(var family=0;family<3;family++)
            {
                var menu=(MenuItem)shell.FindName(menus[family]);Expect(menu.IsEnabled,"Loaded manager menu disabled.");
                menu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Expect(window is not null,"Source menu did not use the real manager factory.");
                var table=(DataGrid)window!.FindName("ManagerObjects");var scope=(CheckBox)window.FindName("ManagerSelectionOnly");
                Expect((int)((ComboBox)window.FindName("ManagerFamily")).SelectedValue==family&&scope.IsChecked==false&&!scope.IsEnabled,"Source menu family/no-selection default incorrect.");
                var expected=family==0?1:family==1?2:1;Expect(table.Items.Count==expected&&table.Items.Cast<ObjectManagerWindow.ManagerRow>().All(r=>r.Entry.Thing.Position.X<document.Width),"Whole-map manager exposed an out-of-map record.");
                window.Cancel();window=null;
            }
            workspace.SetSelection(new(2,3,2,1));
            ((MenuItem)shell.FindName(menus[1])).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            var selectedScope=(CheckBox)window!.FindName("ManagerSelectionOnly");var selectedTable=(DataGrid)window.FindName("ManagerObjects");
            Expect(selectedScope.IsChecked==true&&selectedScope.IsEnabled&&selectedTable.Items.Count==1,"Opening with a selection did not choose the source selected-area list.");
            selectedScope.IsChecked=false;Expect(selectedTable.Items.Count==2,"Whole-map switch did not include both door/trap rows.");
            window.Cancel();window=null;((MenuItem)shell.FindName(menus[1])).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Expect(((CheckBox)window!.FindName("ManagerSelectionOnly")).IsChecked==true,"Reopening retained old scope instead of reading the current selection.");
            LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(((MenuItem)shell.FindName(menus[0])).Header as string==LocalizationService.Get("Manager.Creatures"),"Source menu label did not refresh live.");
            window.Cancel();window=null;var count=presented;
            using(var stroke=document.BeginLivePenStroke(new(4,2,1)))
            {((MenuItem)shell.FindName(menus[0])).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Expect(presented==count,"Live stroke opened a manager.");}
            Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditRevision==revision,"Manager menu/filter mutated map bytes/history.");
            shell.Close();((MenuItem)shell.FindName(menus[0])).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Expect(presented==count,"Closed owner reopened a manager.");
        }
        finally{window?.Cancel();workspace.Document=null;shell.Close();LocalizationService.SetLanguage(language);}
    }
}
