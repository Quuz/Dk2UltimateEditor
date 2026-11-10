using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public enum WorkspaceManagerRoute { Bands,Areas,Gates,Effects,Patrols,Scenarios }

public partial class EditorWorkspace
{
    private const string BandManagerCategory="Manager.Bands";
    public Action<string>? SourceManagerNoticePresenter{get;set;}
    public bool RequestSourceManager(WorkspaceManagerRoute route,NativeMapDocument? expectedOwner=null)
    {
        if(!Enum.IsDefined(route))throw new ArgumentOutOfRangeException(nameof(route));
        if(document is not {} source||source.HasActivePenStroke||expectedOwner is not null&&!ReferenceEquals(source,expectedOwner)
            ||!CommitFields()||!ReferenceEquals(document,source)||source.HasActivePenStroke)return false;
        try
        {
            if(route==WorkspaceManagerRoute.Patrols&&source.PatrolAreas().Count==0)
            {SourceManagerNotice(route,LocalizationService.Get("Manager.NoPatrolAreas"));return false;}
            CloseHeroBandWindow();CloseRegionPropertiesWindow();CancelRegionEditing();ResetPatrolEditing();
            if(!ReferenceEquals(document,source))return false;
            switch(route)
            {
                case WorkspaceManagerRoute.Bands:
                    RefreshRecords();RecordSearch.Text="";
                    CategoryPicker.SelectedItem=((CategoryView[])CategoryPicker.ItemsSource).Single(v=>v.ID==BandManagerCategory);
                    TemplatePicker.SelectedItem=((TemplateView[])TemplatePicker.ItemsSource).FirstOrDefault(v=>v.LegacyKind=="Band"||v.Template?.Structure=="GAMEBAND");
                    WorkspaceTabs.SelectedIndex=1;RecordList.Focus();break;
                case WorkspaceManagerRoute.Areas:
                case WorkspaceManagerRoute.Gates:
                case WorkspaceManagerRoute.Effects:
                    regionManagerKind=route==WorkspaceManagerRoute.Areas?MapRegionKind.Area:route==WorkspaceManagerRoute.Gates?MapRegionKind.Gate:MapRegionKind.Effect;
                    RegionKindPicker.SelectedValue=(int)regionManagerKind.Value;RefreshRegions();
                    WorkspaceTabs.SelectedIndex=0;RegionToolsExpander.IsExpanded=true;RegionPanel.BringIntoView();RegionPicker.Focus();break;
                case WorkspaceManagerRoute.Patrols:
                    RefreshPatrol();WorkspaceTabs.SelectedIndex=3;PatrolRouteList.Focus();break;
                case WorkspaceManagerRoute.Scenarios:
                    RefreshScriptTree();ScriptOwnerSearch.Text="";ScriptScopePicker.SelectedValue=32;
                    WorkspaceTabs.SelectedIndex=2;ScriptOwnerPicker.Focus();break;
            }
            return true;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.IOException or System.IO.InvalidDataException or OverflowException or FormatException)
        {SourceManagerNotice(route,LocalizationService.Get("Editor.OperationFailed")+"\n"+error.Message);return false;}
    }
    private void SourceManagerNotice(WorkspaceManagerRoute route,string text)
    {
        if(SourceManagerNoticePresenter is {} present)present(text);
        else DK2MapEditor.App.Editing.OriginalMessageBox.Show(Window.GetWindow(this),text,LocalizationService.Get("Manager."+route),MessageBoxButton.OK,MessageBoxImage.Information);
    }
    private void RegionShowAll_Click(object sender,RoutedEventArgs args)
    {
        if(document is null||document.HasActivePenStroke||!CommitFields())return;
        regionManagerKind=null;RefreshRegions();
    }
}
