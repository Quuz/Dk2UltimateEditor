using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;
internal static partial class Program
{
    // Compile only. Native input, mouse capture and scroll raster require manual acceptance.
    private static void CheckSpecialKeyboard(bool native,string language)
    {
        var doc=NativeMapDocument.CreateNewMap(new("SpecialKeysUI","Special keys UI",64,64,NativeFormat:native));
        doc.AddMapRegion(MapRegionKind.Area,new(3,3,1,1));
        var workspace=new EditorWorkspace{Document=doc};var host=new Window{Content=workspace};
        var canvas=Control<MapCanvas>(workspace,"Canvas");var scroll=Control<ScrollViewer>(workspace,"MapScroll");
        CreatureBrushQuickWindow? quick=null;ObjectBatchWindow? brush=null;
        workspace.BrushQuickPresenter=w=>quick=w;workspace.ObjectBrushPresenter=w=>brush=w;
        void Layout(){workspace.Measure(new Size(1100,850));workspace.Arrange(new Rect(0,0,1100,850));workspace.UpdateLayout();workspace.Dispatcher.Invoke(()=>{},DispatcherPriority.Background);workspace.UpdateLayout();}
        try
        {
            workspace.SetToolbarsVisibility(false);workspace.SetOverviewVisibility(false);workspace.ApplyUserZoom(17,63);Layout();
            var before=doc.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var history=doc.GetHistory();var revision=doc.EditRevision;
            workspace.SetSelection(new(2,2,3,3));workspace.Copy();var selection=canvas.Selection;var clipboard=workspace.BufferedSelection;
            void Exact()=>Expect(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&doc.EditRevision==revision&&doc.EditorChangeCount==history.EditorChangeCount
                &&doc.GetHistory().Undo.Count==history.Undo.Count&&canvas.Selection==selection&&ReferenceEquals(workspace.BufferedSelection,clipboard),"Special keyboard view/template action changed map/history/selection/buffer.");
            var end=EditorSpecialKeyboard.Target(doc.Width,scroll.ViewportWidth,canvas.CellWidth,0,EditorScrollStep.End);
            Expect(end is not null&&workspace.RequestMapScroll(new(Scroll:EditorScrollStep.End)),"Map did not accept source horizontal End.");Layout();
            Expect(Math.Abs(scroll.HorizontalOffset-end!.Value)<0.01&&canvas.Width==doc.Width*17,"WPF extent lost source last-cell origin or changed canvas tile dimensions.");Exact();
            workspace.RequestMapScroll(new(Scroll:EditorScrollStep.Start));Layout();workspace.RequestMapScroll(new(Scroll:EditorScrollStep.NextLine));Layout();
            Expect(scroll.HorizontalOffset==17,"Source line scroll did not move one cell.");Exact();
            var tool=canvas.Tool;
            workspace.RequestCreatureBrushQuick(BrushQuickPage.CreatureOptions);var boxes=((StackPanel)quick!.Content).Children.OfType<CheckBox>().ToArray();Expect(boxes.Length==6,"Creature quick page exposed source-excluded bits.");boxes[0].IsChecked=false;quick.Close();
            Expect(quick.Accepted&&canvas.Tool==tool,"Quick Close did not accept or changed the active tool.");Exact();
            workspace.RequestObjectBrush(PlacedThingKind.Creature);Expect(((TextBox)brush!.FindName("BatchCreatureOptions")).Text=="8","Quick creature choices did not reach the source brush template.");brush.Cancel();
            // Full brush factory switches only after acceptance, so cancelling keeps the old tool.
            workspace.RequestCreatureBrushQuick(BrushQuickPage.HeroOptions);boxes=((StackPanel)quick!.Content).Children.OfType<CheckBox>().ToArray();Expect(boxes.Length==4,"Hero quick page lost a source bit.");boxes[1].IsChecked=true;
            LocalizationService.SetLanguage(language=="en"?"ru":"en");Expect(quick.Title==LocalizationService.Get("QuickBrush.HeroOptions"),"Quick title did not refresh language.");quick.Close();Exact();
            workspace.RequestCreatureBrushQuick(BrushQuickPage.Objective);var panel=(StackPanel)quick!.Content;var radios=panel.Children.OfType<RadioButton>().ToArray();Expect(radios.Length==10,"Objective quick page changed source order/count.");
            radios[8].IsChecked=true;var choices=panel.Children.OfType<ComboBox>().ToArray();choices[0].SelectedValue=3u;Expect(choices[1].IsEnabled,"Area objective did not enable its area picker.");quick.Close();Exact();
            workspace.RequestObjectBrush(PlacedThingKind.Creature);Expect(((TextBox)brush!.FindName("BatchHeroOptions")).Text=="4"&&((TextBox)brush.FindName("BatchObjective")).Text=="23"&&((TextBox)brush.FindName("BatchEnemy")).Text=="3","Quick objective/options publication changed the wrong template fields.");brush.Cancel();
            workspace.RequestCreatureBrushQuick(BrushQuickPage.HeroOptions);var held=quick!;workspace.Document=null;Expect(!held.Accepted&&!held.CommitClose(),"Document replacement committed a stale quick window.");
            workspace.Document=doc;using(var pen=doc.BeginLivePenStroke(new(4,2,0)))Expect(!workspace.RequestCreatureBrushQuick(BrushQuickPage.Objective),"Insert quick dialog interrupted live pen.");
        }
        finally{quick?.Abort();brush?.Cancel();workspace.Document=null;host.Close();LocalizationService.SetLanguage(language);}
    }
}
