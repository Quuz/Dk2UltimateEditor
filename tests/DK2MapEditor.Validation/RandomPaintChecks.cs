using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DK2MapEditor.Core.Editing;

internal static class RandomPaintChecks
{
    private static readonly EditorPaintShape[] Shapes=[EditorPaintShape.Line,EditorPaintShape.Rectangle,EditorPaintShape.FilledRectangle,EditorPaintShape.Ellipse,EditorPaintShape.FilledEllipse];
    private static void Expect(bool condition,string message) { if(!condition) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch(Exception exception) when(exception is ArgumentException or InvalidOperationException) { return; }
        throw new Exception("Invalid random painting accepted.");
    }
    public static void Run()
    {
        var repo=AppContext.BaseDirectory; while(!Directory.Exists(Path.Combine(repo,"Source"))) repo=Path.GetDirectoryName(repo)!;
        var display=File.ReadAllText(Path.Combine(repo,"Source/Sources/Map/Map_Display.c"));
        var mouse=File.ReadAllText(Path.Combine(repo,"Source/Sources/Map/Map_Mouse.c"));
        var config=File.ReadAllText(Path.Combine(repo,"Source/Sources/Properties/Editor/Properties_Editor_Config.c"));
        var paint=display[display.IndexOf("void Map_PaintElement(",StringComparison.Ordinal)..];
        Expect(config.Contains("Config.RandomFreq = 2;") && RandomPaint.DefaultFrequency==2,"Original random default mismatch.");
        Expect(paint.IndexOf("rnd = random(Config.RandomFreq)",StringComparison.Ordinal)<paint.IndexOf("if (rnd) continue;",StringComparison.Ordinal)
            && paint.Contains("if (!GetPixel(Map.hDC,X,Y)) continue;") && mouse.Contains("if (Shift)")
            && mouse.Contains("if ((MapCursorMinWidth != 1)||(MapCursorMinHeight != 1))"),"Original random order/Shift/footprint evidence changed.");
        foreach(var suffix in new[]{"RECTRND","RECTFILLRND","CIRCLERND","CIRCLEFILLRND","LINERND"}) Expect(mouse.Contains("FillID = PAINTMODE_"+suffix+";"),"Missing source random mode.");
        CheckMasks();
        var directory=Path.Combine(Path.GetTempPath(),"DK2MapEditor.RandomPaint",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try { foreach(var native in new[]{false,true}) CheckEdits(native,directory); }
        finally { Directory.Delete(directory,true); }
    }
    private static void CheckMasks()
    {
        using var process=Process.GetCurrentProcess();
        OriginalPaintRaster.Points(EditorPaintShape.Ellipse,new(2,3),new(4,5),new(2,3,3,3));
        var handles=GetGuiResources(process.Handle,0); var count=0; var oldDifferences=0;
        foreach(var shape in Shapes) for(var width=1;width<=12;width++) for(var height=1;height<=12;height++)
            foreach(var reversed in new[]{false,true})
            {
                var bounds=new TileRect(2,3,width,height); var start=new TilePoint(2,3); var end=new TilePoint(1+width,2+height);
                if(reversed) (start,end)=(end,start);
                var expected=OriginalGdiReference.Points(shape,start,end,bounds); var actual=OriginalPaintRaster.Points(shape,start,end,bounds);
                Expect(expected.SetEquals(actual),$"Source GDI mask differs: {shape} {width}x{height} reverse={reversed}."); count++;
                if(shape is EditorPaintShape.Ellipse or EditorPaintShape.FilledEllipse && !expected.SetEquals(MapDrawing.Ellipse(bounds,shape==EditorPaintShape.FilledEllipse))) oldDifferences++;
            }
        for(var x=0;x<9;x++) for(var y=0;y<9;y++) foreach(var reversed in new[]{false,true})
        {
            var start=new TilePoint(10,10); var end=new TilePoint(2+x,2+y); if(reversed) (start,end)=(end,start);
            var bounds=TileRect.Between(start,end); Expect(OriginalGdiReference.Points(EditorPaintShape.Line,start,end,bounds).SetEquals(OriginalPaintRaster.Points(EditorPaintShape.Line,start,end,bounds)),"Reversed diagonal GDI line differs."); count++;
        }
        Expect(GetGuiResources(process.Handle,0)<=handles+1,"Source shape raster leaked GDI handles.");
        Expect(oldDifferences>0,"Independent oracle did not distinguish the former approximate ellipse raster.");
        Console.WriteLine($"      original GDI masks={count}, former ellipse differences={oldDifferences}, retained GDI handles={GetGuiResources(process.Handle,0)-handles}");
    }
    private static void CheckEdits(bool native,string directory)
    {
        var document=NativeMapDocument.CreateNewMap(new("Random","Random",20,20,NativeFormat:native));
        document.Paint(MapDrawing.Rectangle(new(0,0,20,20),true),new(2,2,1,213));
        var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray()); var changes=0; document.Changed+=(_,_)=>changes++;
        void Exact()=>Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Random rejection/undo changed source bytes.");
        foreach(var shape in Shapes) foreach(var reversed in new[]{false,true}) foreach(var frequency in new[]{1,2,3,17,int.MaxValue})
        {
            var start=new TilePoint(0,0); var end=new TilePoint(8,6); if(reversed) (start,end)=(end,start);
            var bounds=TileRect.Between(start,end); var mask=OriginalGdiReference.Points(shape,start,end,bounds); var selection=new TileRect(2,1,5,4);
            var expected=before.ToDictionary(pair=>pair.Key,pair=>pair.Value.ToArray()); var count=0;
            for(var y=0;y<7;y++) for(var x=0;x<9;x++)
            {
                var accepted=(count++%5)%frequency==0;
                if(accepted && mask.Contains(new(x,y)) && selection.Contains(x,y) && x>0 && y>0)
                    BinaryPrimitives.WriteUInt32LittleEndian(expected[document.TileFile].AsSpan(36+(y*20+x)*4),new MapTile(4,2,1,223).Packed);
            }
            var samples=0; var calls=changes;
            document.PaintEditorGesture(start,end,new(4,7,2,223),shape,selection,randomized:true,randomFrequency:frequency,
                randomSample: maximum=> { Expect(maximum==frequency,"Sampler range wrong."); return (samples++%5)%maximum; });
            Expect(samples==63 && expected.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Random row-major prefilter order, source mask or unrelated bytes wrong.");
            var modified=!expected[document.TileFile].AsSpan().SequenceEqual(before[document.TileFile]);
            Expect(changes==calls+(modified?1:0),"Random edit was not atomic/no-op.");
            if(modified)
            {
                Expect(document.GetHistory().Undo[^1].Description=="Random "+shape,"Random history label wrong.");
                document.Undo(); Exact(); var priorSamples=samples; document.Redo(); Expect(samples==priorSamples && expected.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Redo resampled/changed random writes.");
                var saved=document.SaveCopy(Path.Combine(directory,$"{native}-{shape}-{reversed}-{frequency}")); var reopened=NativeMapDocument.Load(saved);
                Expect(reopened.TileFile.Data.Span.SequenceEqual(expected[document.TileFile]),"Saved random mask changed.");
                if(!native) { var compiled=document.CompileLegacyProject("Compiled",NativeMapDocument.Create(20,20,"Basis")); for(var y=1;y<19;y++) for(var x=1;x<19;x++) Expect(compiled.GetTile(x,y).Terrain==document.GetTile(x,y).Terrain,"Random terrain lost during compilation."); }
                document.Undo(); Exact();
            }
        }
        var redo=document.GetHistory().Redo; var events=changes;
        document.PaintEditorGesture(new(2,2),new(8,8),new(4,2,1),EditorPaintShape.FilledRectangle,randomized:true,randomSample:_=>1);
        Exact(); Expect(changes==events && document.GetHistory().Redo.SequenceEqual(redo),"Empty random draw cleared redo.");
        Reject(()=>document.PaintEditorGesture(new(2,2),new(8,8),new(4,2,1),EditorPaintShape.FilledRectangle,randomized:true,randomFrequency:0)); Exact();
        var iterations=0; Reject(()=>document.PaintEditorGesture(new(2,2),new(8,8),new(4,2,1),EditorPaintShape.FilledRectangle,randomized:true,
            randomSample:_=>++iterations==7?throw new InvalidOperationException("Sample failure"):0)); Exact();
        foreach(var invalid in new[]{-1,2}) { Reject(()=>document.PaintEditorGesture(new(2,2),new(8,8),new(4,2,1),EditorPaintShape.FilledRectangle,randomized:true,randomSample:_=>invalid)); Exact(); }
        foreach(var selected in new[]{new MapTile(14,3,1),new MapTile(12,2,1),new MapTile(40,1,1),new MapTile(28,1,1),new MapTile(33,1,1),new MapTile(34,1,1),new MapTile(37,1,1),new MapTile(37,1,2)})
        {
            document.PaintEditorGesture(new(3,3),new(9,9),selected,EditorPaintShape.Ellipse,randomized:true,randomFrequency:0,randomSample:_=>throw new Exception("Bounded placement sampled."));
            var rectangle=EditorTerrainRules.Footprint(selected).GestureRectangle(new(3,3),new(9,9),20,20)!.Value;
            foreach(var point in MapDrawing.Rectangle(rectangle,true)) Expect(document.GetTile(point.X,point.Y)==EditorTerrainRules.NormalizePaint(selected),"Special random footprint not filled.");
            document.Undo(); Exact();
        }
        document.PaintEditorGesture(new(2,2),new(4,4),new(4,2,1),EditorPaintShape.Brush,randomized:true,randomFrequency:0,randomSample:_=>throw new Exception("Brush sampled.")); document.Undo(); Exact();
        // Approximate frequency independently, on a repeatable injected PRNG.
        var random=new Random(137); var callsCount=0; document.PaintEditorGesture(new(1,1),new(18,18),new(4,2,1),EditorPaintShape.FilledRectangle,
            randomized:true,randomSample:max=> { callsCount++; return random.Next(max); });
        var painted=MapDrawing.Rectangle(new(1,1,18,18),true).Count(point=>document.GetTile(point.X,point.Y).Terrain==4);
        Expect(callsCount==324 && painted is >110 and <210,"Default random probability is not approximately 1/2."); document.Undo(); Exact();
        document.PaintEditorGesture(new(1,1),new(18,18),new(4,2,1),EditorPaintShape.FilledRectangle,randomized:true);
        var productionCount=MapDrawing.Rectangle(new(1,1,18,18),true).Count(point=>document.GetTile(point.X,point.Y).Terrain==4);
        Expect(productionCount is >0 and <324,"Production default sampler did not make a nontrivial random subset."); document.Undo(); Exact();
    }
    public static void CheckLoaded(NativeMapDocument document)
    {
        if(document.Width<3 || document.Height<3) return;
        var before=document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        var end=new TilePoint(Math.Min(8,document.Width-1),Math.Min(6,document.Height-1)); var bounds=TileRect.Between(new(0,0),end);
        foreach(var shape in Shapes)
        {
            var expected=before[document.TileFile].ToArray(); var mask=OriginalGdiReference.Points(shape,new(0,0),end,bounds); var counter=0;
            for(var y=0;y<bounds.Height;y++) for(var x=0;x<bounds.Width;x++)
                if((counter++%3)==0 && x>0 && y>0 && x<document.Width-1 && y<document.Height-1 && mask.Contains(new(x,y)))
                    BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(36+(y*document.Width+x)*4),new MapTile(4,2,1,223).Packed);
            var samples=0; document.PaintEditorGesture(new(0,0),end,new(4,7,2,223),shape,randomized:true,randomSample:_=>(samples++%3)==0?0:1);
            Expect(samples==bounds.Width*bounds.Height && document.TileFile.Data.Span.SequenceEqual(expected),"Loaded native random painting differs from independent source projection.");
            if(!expected.AsSpan().SequenceEqual(before[document.TileFile])) document.Undo();
            Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Loaded random paint Undo changed any source buffer.");
        }
    }
    [DllImport("user32.dll")] private static extern uint GetGuiResources(IntPtr process,uint flags);
}
