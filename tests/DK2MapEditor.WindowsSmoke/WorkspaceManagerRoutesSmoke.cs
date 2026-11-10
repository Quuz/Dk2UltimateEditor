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
    // Prepared callers only: compile with the solution; do not launch editor UI.
    private static void CheckWorkspaceManagerRoutes(bool native,string language,string directory)
    {
        var fixture=NativeMapDocument.CreateNewMap(new("WorkspaceRoutes","Workspace routes",18,16,NativeFormat:native));
        var path=fixture.SaveCopy(Path.Combine(directory,$"workspace-routes-{language}-{native}"));
        var shell=new MainWindow(new AppSettings(),persistSettings:false){DiscardChangesPrompt=()=>true,SaveNoticePresenter=_=>{}};
        var workspace=(EditorWorkspace)shell.FindName("Editor");
        var names=new[]{"BandManagerMenuItem","AreaManagerMenuItem","GateManagerMenuItem","EffectManagerMenuItem","PatrolManagerMenuItem","ScenarioManagerMenuItem"};
        var menus=names.Select(n=>(MenuItem)shell.FindName(n)).ToArray();
        var tabs=Control<TabControl>(workspace,"WorkspaceTabs");
        var canvas=Control<MapCanvas>(workspace,"Canvas");
        var categories=Control<ComboBox>(workspace,"CategoryPicker");var list=Control<ListBox>(workspace,"RecordList");
        var regions=Control<ListBox>(workspace,"RegionPicker");var kinds=Control<ComboBox>(workspace,"RegionKindPicker");
        var notices=new List<string>();workspace.SourceManagerNoticePresenter=notices.Add;
        void Menu(int index)=>menus[index].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        void RegionActions(bool enabled)=>Expect(new[]{"RegionShowButton","RegionPropertiesButton","RegionDeleteButton"}.All(n=>Control<Button>(workspace,n).IsEnabled==enabled),"Empty or populated region manager has incorrect row actions.");
        try
        {
            Expect(menus.All(m=>!m.IsEnabled)&&!workspace.RequestSourceManager(WorkspaceManagerRoute.Bands),"Workspace manager enabled without a document.");
            shell.OpenMapFromPath(path);var document=workspace.Document!;
            Expect(menus.All(m=>m.IsEnabled),"A loaded workspace manager menu is disabled.");
            var before=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=document.EditRevision;
            var history=document.GetHistory().Undo.Count;workspace.SetSelection(new(2,2,3,3));
            var selection=canvas.Selection;var clipboard=workspace.BufferedSelection;
            Menu(0);
            Expect(tabs.SelectedIndex==1&&Get<string>(categories.SelectedItem,"ID")=="Manager.Bands"&&list.Items.Count==0,"Empty band manager did not open an empty, stable band list.");
            var template=Control<ComboBox>(workspace,"TemplatePicker").SelectedItem;
            Expect(template is not null&&Control<Button>(workspace,"AddRecordButton").IsEnabled,"Known new-map band manager has no Add template.");
            Menu(4);Expect(notices.SequenceEqual(new[]{LocalizationService.Get("Manager.NoPatrolAreas")})&&tabs.SelectedIndex==1,"Missing patrol area did not stop navigation with one notice.");
            for(var index=1;index<=3;index++)
            {Menu(index);Expect(tabs.SelectedIndex==0&&regions.Items.Count==0,"Empty region manager created a record or opened another tab.");RegionActions(false);}
            Control<TextBox>(workspace,"ScriptOwnerSearch").Text="missing owner";Menu(5);
            Expect(tabs.SelectedIndex==2&&Control<TextBox>(workspace,"ScriptOwnerSearch").Text==""&&(int)Control<ComboBox>(workspace,"ScriptScopePicker").SelectedValue==32,"Scenario entry did not open the global owner view.");
            Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditRevision==revision&&document.GetHistory().Undo.Count==history
                &&canvas.Selection==selection&&ReferenceEquals(workspace.BufferedSelection,clipboard),"Manager navigation changed data/history/map selection/clipboard.");
            var band=native?document.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEBAND"),new(0,0)):document.AddLegacyRecord("Band",new(0,0));
            Menu(0);Expect(list.Items.Count==1&&list.Items.Cast<object>().All(r=>NativeMapDocument.IsBand(Get<NativeRecord>(r,"Record"))),"Band manager includes unrelated records.");
            document.DeleteRecord(band);Expect(Get<string>(categories.SelectedItem,"ID")=="Manager.Bands"&&list.Items.Count==0,"Deleting the last band lost the band manager category.");
            foreach(var kind in Enum.GetValues<MapRegionKind>())document.AddMapRegion(kind,new(4+(int)kind*3,5,1,1));
            foreach(var (index,kind) in new[]{(1,MapRegionKind.Area),(2,MapRegionKind.Gate),(3,MapRegionKind.Effect)})
            {
                Menu(index);Expect((int)kinds.SelectedValue==(int)kind&&regions.Items.Count==1&&Get<MapRegion>(regions.Items[0],"Region").Kind==kind,"Region manager did not restrict its actual list by kind.");RegionActions(true);
            }
            kinds.SelectedValue=(int)MapRegionKind.Area;Expect(regions.Items.Count==1&&Get<MapRegion>(regions.Items[0],"Region").Kind==MapRegionKind.Area,"Changing kind retained the old region manager filter.");
            Control<Button>(workspace,"RegionShowAllButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Expect(regions.Items.Count==3,"All-regions command did not restore the generic list.");
            Menu(4);Expect(tabs.SelectedIndex==3&&notices.Count==1,"Existing area did not allow patrol editor navigation.");
            var other=NativeMapDocument.CreateNewMap(new("OtherOwner","Other owner",18,16,NativeFormat:native));
            Expect(!workspace.RequestSourceManager(WorkspaceManagerRoute.Areas,other)&&tabs.SelectedIndex==3,"Stale owner navigated the current document.");
            using(var stroke=document.BeginLivePenStroke(new(4,2,1)))
            {Menu(5);Expect(tabs.SelectedIndex==3,"Live pen allowed a source manager entry.");}
            LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(menus[0].Header as string==LocalizationService.Get("Manager.Bands"),"Band manager menu retained the old language.");
            shell.Close();tabs.SelectedIndex=1;Menu(5);Expect(tabs.SelectedIndex==1,"Closed main-window owner allowed manager navigation.");
        }
        finally{workspace.Document=null;shell.Close();LocalizationService.SetLanguage(language);}
    }
}
