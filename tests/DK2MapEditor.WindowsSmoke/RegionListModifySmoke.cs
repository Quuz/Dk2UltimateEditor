using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Compiled only in this stage. Running requires an explicit manual UI run.
    private static void CheckRegionListModify(bool native,string language)
    {
        var doc=NativeMapDocument.CreateNewMap(new("RegionList","Region list",24,24,NativeFormat:native));
        foreach(var kind in Enum.GetValues<MapRegionKind>())
        {
            doc.AddMapRegion(kind,new(3,4,1,1));doc.AddMapRegion(kind,new(9,10,1,1));
        }
        var workspace=new EditorWorkspace{Document=doc};var host=new Window{Content=workspace};ShowManagerCallerWindow(host);
        var list=Control<ListBox>(workspace,"RegionPicker");var kinds=Control<ComboBox>(workspace,"RegionKindPicker");
        var tabs=Control<TabControl>(workspace,"WorkspaceTabs");RegionPropertiesWindow? opened=null;
        var presentations=0;workspace.RegionPropertiesPresenter=w=>{presentations++;opened=w;ShowManagerCallerWindow(w);};
        var bytes=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;var revision=doc.EditRevision;
        void Exact()=>Expect(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditorChangeCount==count&&doc.GetHistory().Undo.Count==history&&doc.EditRevision==revision,"Opening/cancelling a list draft changed the live map.");
        object Item(int x)=>list.Items.Cast<object>().Single(v=>Get<MapRegion>(v,"Region").Bounds.X==x);
        ListBoxItem Row(object item)
        {
            list.ScrollIntoView(item);list.UpdateLayout();
            return list.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem??throw new Exception("Region row was not realized.");
        }
        MouseButtonEventArgs Double(DependencyObject origin,MouseButton button=MouseButton.Left)
        {
            var args=new MouseButtonEventArgs(Mouse.PrimaryDevice,0,button){RoutedEvent=System.Windows.Controls.Control.MouseDoubleClickEvent,Source=origin};
            list.RaiseEvent(args);return args;
        }
        void CloseDraft(){opened?.Close();opened=null;}
        try
        {
            foreach(var kind in Enum.GetValues<MapRegionKind>())
            {
                Expect(workspace.RequestSourceManager(kind==MapRegionKind.Area?WorkspaceManagerRoute.Areas:kind==MapRegionKind.Gate?WorkspaceManagerRoute.Gates:WorkspaceManagerRoute.Effects),"Region manager entry refused.");
                Expect(list.Items.Count==2&&kinds.SelectedValue is int k&&k==(int)kind,"List kind filtering changed.");
                list.SelectedItem=Item(3);var clicked=Item(9);var row=Row(clicked);var previous=presentations;
                Expect(Double(row).Handled&&presentations==previous+1&&opened is not null&&!opened.IsCreationMode,"Row double-click did not open Modify.");
                Expect(((TextBox)opened!.FindName("RegionX1")).Text=="9"&&((TextBox)opened.FindName("RegionY1")).Text=="10","Double-click opened the selected neighbour instead of the clicked row.");
                Exact();
                // A second request must not discard a different live draft or
                // activate that old draft as though it belonged to the new row.
                previous=presentations;Double(Row(Item(3)));
                Expect(presentations==previous&&opened.IsDraftCurrent&&Control<TextBlock>(workspace,"RegionStatus").Text==LocalizationService.Get("RegionProperties.FinishCurrent"),"Other-row request replaced the private draft or lost its refusal notice.");
                CloseDraft();Exact();
                previous=presentations;Double(list);Double(Row(Item(9)),MouseButton.Right);Double(new ListBoxItem{Content=Item(9)});
                Expect(presentations==previous,"Blank, right-button or foreign-container event opened Modify.");Exact();
                // Button and row route share the same private editing session.
                list.SelectedItem=Item(3);Control<Button>(workspace,"RegionPropertiesButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Expect(opened is not null&&((TextBox)opened.FindName("RegionX1")).Text=="3","Properties button lost its selected-row route.");
                ((TextBox)opened!.FindName("RegionX1")).Text="5";((TextBox)opened.FindName("RegionX2")).Text="5";
                Expect(opened.TryApply(),"Shared Modify refused the current row.");opened=null;
                Expect(doc.MapRegions().Count(r=>r.Kind==kind&&r.Bounds.X==5)==1&&doc.MapRegions().Count(r=>r.Kind==kind&&r.Bounds.X==9)==1
                    &&doc.EditorChangeCount==unchecked(count+1)&&doc.GetHistory().Undo.Count==history+1,"Modify changed a neighbour or split history.");
                doc.Undo();bytes=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());revision=doc.EditRevision;Exact();
                previous=presentations;
                using(var pen=doc.BeginLivePenStroke(new(4,1,1)))Double(Row(Item(9)));
                Expect(presentations==previous,"Double-click interrupted a live pen.");Exact();
                var stale=Item(9);LocalizationService.SetLanguage(language=="en"?"ru":"en");workspace.RefreshLanguage();
                LocalizationService.SetLanguage(language);workspace.RefreshLanguage();
                row=Row(Item(9));var current=row.Content;row.Content=stale;Double(row);row.Content=current;
                Expect(presentations==previous,"Stale row from a previous projection opened Modify.");Exact();
                // Simulate an unannounced byte mutation in the record itself.
                var target=Get<MapRegion>(Item(9),"Region").Record;Expect(MemoryMarshal.TryGetArray(target.File.Data,out var raw),"No raw fixture.");
                var offset=raw.Offset+target.Offset+target.Length-1;raw.Array![offset]^=1;
                Double(Row(Item(9)));raw.Array[offset]^=1;
                Expect(presentations==previous,"Changed row payload retained Modify authority.");Exact();
            }
            var source=Row(Item(9));var calls=presentations;tabs.SelectedIndex=1;Double(source);
            Expect(presentations==calls,"Hidden map-list event opened a region draft.");tabs.SelectedIndex=0;
            Double(Row(Item(9)));Expect(opened is not null,"Final current row did not open.");
            workspace.Document=null;Expect(!opened!.IsDraftCurrent&&!opened.TryApply(),"Closed document retained Modify authority.");opened=null;
        }
        finally{CloseDraft();workspace.Document=null;host.Close();}
    }
}
