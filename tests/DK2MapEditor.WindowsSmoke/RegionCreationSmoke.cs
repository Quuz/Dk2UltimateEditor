using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckRegionCreationWindow(bool native,string language)
    {
        var doc=NativeMapDocument.CreateNewMap(new("CreationUI","Creation UI",24,24,NativeFormat:native));
        var workspace=new EditorWorkspace{Document=doc};workspace.Measure(new Size(1400,950));workspace.Arrange(new Rect(0,0,1400,950));workspace.UpdateLayout();
        var canvas=Control<MapCanvas>(workspace,"Canvas");var picker=Control<ComboBox>(workspace,"RegionKindPicker");
        var panel=Control<StackPanel>(workspace,"RegionPanel");canvas.SetSelection(new(3,4,2,3));
        foreach(var kind in Enum.GetValues<MapRegionKind>())
        {
            picker.SelectedValue=(int)kind;var before=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
            void Exact()=>Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count,"Unadded UI draft changed live state.");
            workspace.RegionPropertiesPresenter=window=>
            {
                Expect(window.IsCreationMode&&window.IsDraftCurrent,"Add button did not open an independent current draft.");
                Exact();((TextBox)window.FindName("RegionX1")).Text="6";((TextBox)window.FindName("RegionY1")).Text="7";
                if(kind!=MapRegionKind.Gate){((TextBox)window.FindName("RegionX2")).Text="8";((TextBox)window.FindName("RegionY2")).Text="9";}
                var name=(TextBox)window.FindName("RegionName");Expect(name.IsEnabled==!native,"Native label fields became writable.");
                if(!native){name.Text="Custom UI";((TextBox)window.FindName("RegionInformation")).Text="Retained draft";}
                LocalizationService.SetLanguage(language=="en"?"ru":"en");workspace.RefreshLanguage();Exact();
                Expect(((TextBox)window.FindName("RegionX1")).Text=="6"&&window.IsDraftCurrent,"Language change lost the creation draft.");
                LocalizationService.SetLanguage(language);workspace.RefreshLanguage();
                Expect(window.TryApply()&&window.IsDraftCurrent&&((Button)window.FindName("RegionApply")).IsEnabled,"Add closed/disabled the next draft.");
                Expect(doc.EditorChangeCount==unchecked(count+1)&&doc.GetHistory().Undo.Count==history+1,"First UI Add split history.");
                if(!native)Expect(name.Text=="Custom UI","Next Add lost custom name.");
                Expect(window.TryApply()&&doc.EditorChangeCount==unchecked(count+2),"Repeated Add failed.");window.Close();
            };
            Control<Button>(workspace,"RegionAddButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(doc.MapRegions().Count(r=>r.Kind==kind)==2,"UI Add did not publish two regions.");
            var expected=kind==MapRegionKind.Gate?new TileRect(6,7,1,1):new(6,7,3,3);
            Expect(doc.MapRegions().Where(r=>r.Kind==kind).All(r=>r.Bounds==expected),"Full draft geometry was not applied.");
            doc.Undo();doc.Undo();Exact();
            workspace.RegionPropertiesPresenter=window=>{Exact();window.Close();};
            Click(panel,"Editor.RegionAddSelection");Exact();
            workspace.RegionPropertiesPresenter=window=>
            {
                Expect(window.BeginPointPick()&&canvas.IsPickingRegion,"Creation geometry picker was not connected.");
                canvas.CompleteRegionPick(new(9,10),new(9,10));Exact();
                Expect(!window.HasPendingPointPick&&((TextBox)window.FindName("RegionX1")).Text=="9","Picked creation geometry was not retained privately.");
                window.Close();
            };
            Control<Button>(workspace,"RegionAddButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Exact();
        }
        workspace.RegionPropertiesPresenter=window=>
        {
            var replacement=NativeMapDocument.CreateNewMap(new("Replacement","Replacement",24,24,NativeFormat:native));
            workspace.Document=replacement;Expect(!window.IsDraftCurrent&&!window.TryApply(),"Document replacement retained Add authority.");
        };
        Control<Button>(workspace,"RegionAddButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));workspace.Document=null;
    }
}
