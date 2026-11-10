using System.Buffers.Binary;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckRandomPaint(NativeMapDocument document,string language)
    {
        if(document.IsLegacyProject) document.AddLegacyRecord("Creature",new(11,11));
        else document.AddRecord(RecordTemplate.All.Single(item=>item.Structure=="GAMEKEEPER"),new(11,11));
        var script=document.EnsureScriptScope(document.ScriptOwners().Single(owner=>owner.Scope==2));
        document.InsertScriptEvent(script.Owner,script.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));
        document.Paint(MapDrawing.Rectangle(new(0,0,20,20),true),new(2,2,1,213));
        var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray()); var changes=0; document.Changed+=(_,_)=>changes++;
        var workspace=new EditorWorkspace { Document=document }; var host=new Window { Content=workspace };
        var canvas=Control<MapCanvas>(workspace,"Canvas"); var checkbox=Control<CheckBox>(workspace,"RandomPaintCheck");
        var tool=Control<ComboBox>(workspace,"ToolPicker"); var terrain=Control<ComboBox>(workspace,"TerrainPicker"); var owner=Control<ComboBox>(workspace,"OwnerPicker"); var variant=Control<ComboBox>(workspace,"VariantPicker");
        void Layout() { workspace.Measure(new Size(1400,900)); workspace.Arrange(new Rect(0,0,1400,900)); workspace.UpdateLayout(); }
        void Exact()=>Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"WPF random paint/cancel/Undo changed unrelated source bytes.");
        void Toggle(bool state) { checkbox.IsChecked=state; checkbox.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent)); }
        void Palette(int id,int v=1) { terrain.SelectedValue=id; owner.SelectedValue=7; variant.SelectedIndex=v-1; }
        workspace.SetSelection(new(1,1,8,6)); workspace.Copy(); var buffer=workspace.BufferedSelection;
        var selection=canvas.Selection; Palette(4); Layout();
        foreach(var drawing in new[]{DrawingTool.Line,DrawingTool.Rectangle,DrawingTool.FilledRectangle,DrawingTool.Ellipse,DrawingTool.FilledEllipse})
        {
            tool.SelectedValue=(int)drawing; Expect(checkbox.IsEnabled,"Random control disabled for an ordinary shape.");
            foreach(var shift in new[]{false,true})
            {
                Toggle(!shift); var calls=changes; var samples=0;
                Expect(canvas.RandomPainting==!shift && calls==changes,"Random toggle edited map or was not bound.");
                var shape=Enum.Parse<EditorPaintShape>(drawing.ToString()); var mask=OriginalGdiReference.Points(shape,new(8,6),new(0,0),new(0,0,9,7));
                var expected=before.ToDictionary(pair=>pair.Key,pair=>pair.Value.ToArray()); var index=0;
                for(var y=0;y<7;y++) for(var x=0;x<9;x++)
                {
                    var paint=(index++%3)==0 && mask.Contains(new(x,y)) && selection!.Value.Contains(x,y) && x>0 && y>0;
                    if(paint) BinaryPrimitives.WriteUInt32LittleEndian(expected[document.TileFile].AsSpan(36+(y*20+x)*4),new MapTile(4,2,1).Packed);
                }
                canvas.DrawGesture(new(8,6),new(0,0),shift:shift,randomSample:max=> { Expect(max==2,"UI did not use source probability."); return (samples++%3)==0?0:1; }); Layout();
                Expect(samples==63 && expected.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)) && changes==calls+1
                    && canvas.Selection==selection && ReferenceEquals(buffer,workspace.BufferedSelection),$"Actual checkbox/Shift draw differs: {drawing}, shift={shift}, samples={samples}, changed={changes-calls}, tile={canvas.PaintTile}, exact={expected.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))}, selection={canvas.Selection==selection}, clipboard={ReferenceEquals(buffer,workspace.BufferedSelection)}.");
                var label=HistoryLabels.Description(document.GetHistory().Undo[^1].Description);
                Expect(label==LocalizationService.Get("Paint.Random."+shape),"Random undo description not translated.");
                if(drawing==DrawingTool.FilledEllipse && !shift && Environment.GetEnvironmentVariable("DK2_RANDOM_PAINT_UI_ARTIFACTS") is { Length:>0 } directory)
                {
                    Directory.CreateDirectory(directory); var bitmap=new RenderTargetBitmap(1400,900,96,96,PixelFormats.Pbgra32); bitmap.Render(workspace);
                    var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var output=new FileStream(Path.Combine(directory,$"random-{language}-{document.IsLegacyProject}.png"),FileMode.CreateNew); encoder.Save(output);
                }
                document.Undo(); Exact(); var count=samples; document.Redo(); Expect(samples==count,"WPF Redo resampled random cells."); document.Undo(); Exact();
            }
        }
        Toggle(true); tool.SelectedValue=(int)DrawingTool.FilledEllipse;
        var prior=changes; LocalizationService.SetLanguage(language=="en"?"ru":"en"); workspace.RefreshLanguage(); Layout();
        Expect(checkbox.IsChecked==true && canvas.RandomPainting && checkbox.Content as string==LocalizationService.Get("Paint.RandomToggle")
            && checkbox.ToolTip as string==LocalizationService.Get("Paint.RandomHint") && canvas.Selection==selection && ReferenceEquals(buffer,workspace.BufferedSelection)
            && changes==prior,"Live random control translation discarded mode/selection or changed map.");
        LocalizationService.SetLanguage(language); workspace.RefreshLanguage();
        foreach(var drawing in new[]{DrawingTool.Brush,DrawingTool.Fill,DrawingTool.Select,DrawingTool.Sample})
        {
            tool.SelectedValue=(int)drawing; Expect(!checkbox.IsEnabled && checkbox.IsChecked==true && canvas.RandomPainting,"Disabled tool lost retained random preference.");
        }
        workspace.ClearSelection(); Palette(14); tool.SelectedValue=(int)DrawingTool.Ellipse;
        Expect(!checkbox.IsEnabled,"Random control enabled for a forced special footprint.");
        canvas.DrawGesture(new(3,3),new(10,10),shift:true,randomSample:_=>throw new Exception("Special UI draw sampled."));
        foreach(var point in MapDrawing.Rectangle(new(3,3,5,5),true)) Expect(document.GetTile(point.X,point.Y).Terrain==14,"Random mode punched holes in a heart.");
        document.Undo(); Exact();
        canvas.DrawGesture(new(0,0),new(10,10),shift:true,randomSample:_=>throw new Exception("Rejected footprint sampled.")); Exact();
        Expect(Control<TextBlock>(workspace,"PlacementStatus").Text==LocalizationService.Get("Editor.Placement.ProtectedFootprint"),"Special random rejection not localized.");
        Palette(4); tool.SelectedValue=(int)DrawingTool.Brush; canvas.DrawGesture(new(2,2),new(4,4),shift:true,randomSample:_=>throw new Exception("Brush UI draw sampled.")); document.Undo(); Exact();
        tool.SelectedValue=(int)DrawingTool.FilledRectangle; canvas.BeginRegionPick(true); Expect(!checkbox.IsEnabled,"Random toggle active during point picking.");
        canvas.CompleteRegionPick(new(2,2),new(2,2)); Exact(); Expect(checkbox.IsEnabled,"Random toggle not restored after picking.");
        // Original mouse allocates rectangle Undo before painting; all five
        // accepted random calls increment MapChanges and UsageOfPaintRnd even
        // if every sample misses. Generic raw no-op APIs retain their own policy.
        void StatisticsExact(ActivityStatisticsPayload? expected)
        {
            var current=document.ReadActivityStatistics();
            Expect(expected is null?current is null:current is not null&&expected.Encode().AsSpan().SequenceEqual(current.Encode()),"No-hit random gesture/replay changed another complete statistics field.");
        }
        foreach(var emptyShape in new[]{DrawingTool.Line,DrawingTool.Rectangle,DrawingTool.FilledRectangle,DrawingTool.Ellipse,DrawingTool.FilledEllipse})
        {
            tool.SelectedValue=(int)emptyShape;var emptyHistory=document.GetHistory();var emptyCounter=document.EditorChangeCount;
            var emptyRevision=document.EditRevision;var emptyDirty=document.HasChanges;var emptyNotices=changes;var emptySamples=0;
            var emptyStats=document.ReadActivityStatistics();var afterStats=emptyStats is null?null:emptyStats with{UsageOfPaintRnd=unchecked(emptyStats.UsageOfPaintRnd+1)};
            canvas.DrawGesture(new(2,2),new(8,8),randomSample:max=>{Expect(max==2,"No-hit source probability changed.");emptySamples++;return 1;});Exact();
            Expect(emptySamples==49&&changes==emptyNotices+1&&document.GetHistory().Undo.Count==emptyHistory.Undo.Count+1
                &&document.GetHistory().ChangeCount==emptyHistory.ChangeCount+1&&document.EditorChangeCount==unchecked(emptyCounter+1)
                &&document.GetHistory().EditorChangeCount==document.EditorChangeCount&&document.HasChanges&&document.EditRevision==emptyRevision+1,
                "Accepted no-hit source random gesture lost exact one Undo/count/dirty/revision/notification.");StatisticsExact(afterStats);
            Expect(HistoryLabels.Description(document.GetHistory().Undo[^1].Description)==LocalizationService.Get("Paint.Random."+emptyShape),"No-hit random history label not localized.");
            document.Undo();Exact();var afterUndo=afterStats is null?null:afterStats with{UsageOfUndo=unchecked(afterStats.UsageOfUndo+1)};StatisticsExact(afterUndo);
            Expect(document.EditorChangeCount==emptyCounter&&document.GetHistory().ChangeCount==emptyHistory.ChangeCount
                &&document.GetHistory().Undo.Count==emptyHistory.Undo.Count&&document.HasChanges==emptyDirty&&document.CanRedo&&changes==emptyNotices+2,
                "No-hit random Undo lost original counter/history/dirty state.");
            document.Redo();Exact();StatisticsExact(afterUndo);
            Expect(emptySamples==49&&document.EditorChangeCount==unchecked(emptyCounter+1)&&document.GetHistory().Undo.Count==emptyHistory.Undo.Count+1
                &&document.HasChanges&&changes==emptyNotices+3,"No-hit random Redo resampled or lost source counter/history.");
            document.Undo();Exact();StatisticsExact(afterUndo is null?null:afterUndo with{UsageOfUndo=unchecked(afterUndo.UsageOfUndo+1)});
            Expect(emptySamples==49&&document.EditorChangeCount==emptyCounter&&document.HasChanges==emptyDirty&&document.EditRevision==emptyRevision+4&&changes==emptyNotices+4,
                "Final no-hit random Undo did not retain exact original state/lifetime.");
        }
        Console.WriteLine($"SOURCE EMPTY RANDOM {language} {(document.IsLegacyProject?"project":"native")} shapes=5 samples=49 one-undo one-counter dirty exact-buffers full-statistics localized-history undo-redo-no-resample");
        workspace.Document=null; Expect(!checkbox.IsEnabled,"Closed document retained random control.");
        canvas.DrawGesture(new(2,2),new(8,8),shift:true,randomSample:_=>throw new Exception("Closed UI draw sampled.")); Exact();
        workspace.Document=document; Expect(canvas.RandomPainting && checkbox.IsChecked==true && checkbox.IsEnabled,"Reopening document lost session random mode.");
        workspace.Document=null; host.Content=null; host.Close();
        Console.WriteLine($"PASS {language} {(document.IsLegacyProject?"project":"native")} all five original random shapes, actual checkbox/Shift path, masks/order, EN/RU history, Undo/Redo, special/brush/picking and source lifetime");
    }
}
