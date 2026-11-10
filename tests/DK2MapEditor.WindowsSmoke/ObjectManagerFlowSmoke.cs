using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared actual-caller acceptance; compilation is not a GUI execution.
    private static void CheckObjectManagerFlow(bool native,string language)
    {
        var d=NativeMapDocument.CreateNewMap(new("ManagerFlow","ManagerFlow",16,14,NativeFormat:native));
        var workspace=new EditorWorkspace{Document=d};var host=new Window{Content=workspace};
        var manager=workspace.CreateObjectManagerWindow()!;ShowManagerCallerWindow(manager);
        T Field<T>(string name)=>(T)manager.FindName(name);
        void Flag(string name,bool value)=>Field<CheckBox>(name).IsChecked=value;
        manager.ConfirmationPresenter=window=>Expect(window.Accept(),"Used-object confirmation failed.");
        var options=0;var previews=0;
        manager.OptionsPresenter=window=>{options++;Expect(window.Apply(),"Source options OK failed.");};
        manager.CommandWindowPresenter=window=>{previews++;Expect(window.ApplyPreview(),"Source preview OK failed.");};
        var config=manager.CreateOptionsWindow(false)!;
        ((CheckBox)config.FindName("ManagerOptionsFlagIgnoreTerrain")).IsChecked=true;
        ((CheckBox)config.FindName("ManagerOptionsFlagIgnoreEnemies")).IsChecked=true;Expect(config.Apply(),"Addition options setup failed.");
        Field<TextBox>("ManagerDestinationX").Text="4";Field<TextBox>("ManagerDestinationY").Text="5";
        Flag("ManagerAddAskOptions",false);Flag("ManagerAddPreview",false);
        var before=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
        Expect(manager.RequestAddition()&&d.ObjectManagerEntries().Count==1&&options==0&&previews==0,"No-dialog Add did not execute once.");
        Expect(manager.UndoManagerCommand()&&before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Direct Add did not retain one local Undo.");
        Flag("ManagerAddPreview",true);
        Expect(manager.RequestAddition()&&previews==1,"Preview flag did not open decision UI.");Expect(manager.UndoManagerCommand(),"Preview Add Undo failed.");
        Flag("ManagerAddPreview",false);Flag("ManagerAddAskOptions",true);
        manager.OptionsPresenter=window=>{options++;window.Cancel();};
        Expect(!manager.RequestAddition()&&d.ObjectManagerEntries().Count==0&&previews==1,"Canceled options applied data or opened preview.");
        manager.OptionsPresenter=window=>{options++;Expect(window.Apply(),"Source options OK failed.");};
        Expect(manager.RequestAddition()&&options==2&&previews==1,"Accepted options did not continue to direct Add.");Expect(manager.UndoManagerCommand(),"Options Add Undo failed.");
        config=manager.CreateOptionsWindow(false)!;((ComboBox)config.FindName("ManagerOptionsMode")).SelectedValue=1;Expect(config.Apply(),"Rectangle setup failed.");
        workspace.SetSelection(new(7,6,2,2));Field<CheckBox>("ManagerSelectionOnly").IsChecked=true;
        Flag("ManagerAddAskOptions",false);
        Expect(manager.RequestAddition()&&d.ObjectManagerEntries().Count==4&&d.ObjectManagerEntries().All(e=>e.Thing.Position.X is 7 or 8&&e.Thing.Position.Y is 6 or 7),"Manager selection bounds were not used by rectangle Add.");
        Expect(Field<TextBox>("ManagerDestinationX").Text=="4"&&Field<TextBox>("ManagerDestinationY").Text=="5","Rectangle overwrote point destination preferences.");
        Expect(manager.UndoManagerCommand(),"Rectangle Add Undo failed.");Field<CheckBox>("ManagerSelectionOnly").IsChecked=false;
        var template=manager.CreateTemplateWindow()!;((TextBox)template.FindName("BatchHealth")).Text="77";
        Expect(!template.BuildPreview()&&!template.ApplyPreview()&&d.ObjectManagerEntries().Count==0,"Template editor acquired map authority.");template.Cancel();
        foreach(var x in new[]{2,3})d.ApplyObjectBatchInsertion(d.PlanObjectBatchInsertion(ObjectBatchTemplate.Default(PlacedThingKind.Creature) with{Owner=3,Health=72},new(Position:new(x,3),Flags:(ObjectBatchFlags)127)),[0]);
        Field<DataGrid>("ManagerObjects").SelectAll();Flag("ManagerModifyAskOptions",true);Flag("ManagerModifyPreview",false);
        options=0;previews=0;
        manager.OptionsPresenter=window=>
        {
            options++;
            foreach(var mask in Enum.GetValues<ObjectModificationMask>())if(window.FindName("ManagerOptionsMask"+mask) is CheckBox box)box.IsChecked=mask==ObjectModificationMask.Health;
            ((CheckBox)window.FindName("ManagerOptionsFlagIgnoreTerrain")).IsChecked=true;
            Expect(window.Apply(),"Modification options setup failed.");
        };
        Expect(manager.RequestModification()&&options==1&&previews==0,"Multi Modify did not follow optional options/direct path.");
        Expect(d.ObjectManagerEntries().All(e=>d.ObjectTemplateForModification(e.Record).Health==77),"Multi Modify ignored the shared template.");
        Expect(manager.UndoManagerCommand(),"Direct multi Modify Undo failed.");
        var table=Field<DataGrid>("ManagerObjects");table.UnselectAll();table.SelectedIndex=0;
        options=0;previews=0;
        manager.CommandWindowPresenter=window=>
        {
            previews++;((TextBox)window.FindName("BatchX")).Text="9";((TextBox)window.FindName("BatchY")).Text="8";
            Expect(window.ApplyPreview(),"Single property editor failed to accept coordinates.");
        };
        Expect(manager.RequestModification()&&options==0&&previews==1&&d.ObjectManagerEntries().Any(e=>e.Thing.Position==new TilePoint(9,8)),"Single Modify used options or lost absolute coordinates.");
        Expect(manager.UndoManagerCommand(),"Single property Undo failed.");
        manager.Cancel();host.Close();LocalizationService.SetLanguage(language);
    }
}
