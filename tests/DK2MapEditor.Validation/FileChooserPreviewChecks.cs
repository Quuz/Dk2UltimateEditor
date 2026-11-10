using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;
using N=DK2MapEditor.App.Editing.NativePreviewDialogInterop;

internal static class FileChooserPreviewChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject(Action action){var rejected=false;try{action();}catch(Exception error)when(error is ArgumentException or IOException or OperationCanceledException or ObjectDisposedException){rejected=true;}Expect(rejected,"Invalid/cancelled preview accepted.");}
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(FileChooserPreviewChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.FileChooserPreviewSource.json")!);
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"File preview source changed.");
        var layout=fixture.RootElement.GetProperty(IntPtr.Size==8?"NativeLayout64":"NativeLayout32");
        Expect(Marshal.SizeOf<N.OpenFileName>()==layout.GetProperty("OpenFileNameSize").GetInt32()&&Marshal.OffsetOf<N.OpenFileName>(nameof(N.OpenFileName.FilterIndex)).ToInt32()==layout.GetProperty("FilterIndexOffset").GetInt32()&&Marshal.OffsetOf<N.OpenFileName>(nameof(N.OpenFileName.Flags)).ToInt32()==layout.GetProperty("FlagsOffset").GetInt32(),"Native OPENFILENAME layout differs from SDK.");
        Expect(Marshal.SizeOf<N.DrawItem>()==layout.GetProperty("DrawItemSize").GetInt32()&&Marshal.SizeOf<N.Notification>()==layout.GetProperty("NotificationSize").GetInt32()&&Marshal.SizeOf<N.BitmapInfo>()==40,"Native drawing/notification ABI differs.");
        var template=FilePreviewDialogPlan.Template();Expect(template.Length==24&&BinaryPrimitives.ReadUInt32LittleEndian(template)==0x44000404&&BinaryPrimitives.ReadUInt16LittleEndian(template.AsSpan(8))==0&&BinaryPrimitives.ReadInt16LittleEndian(template.AsSpan(14))==340&&BinaryPrimitives.ReadInt16LittleEndian(template.AsSpan(16))==110,"Child template style/size/alignment differs.");
        Expect(FilePreviewDialogPlan.NativeFilter("Project|*.dkm|Text|*.map")=="Project\0*.dkm\0Text\0*.map\0\0"&&FilePreviewDialogPlan.Extension("Project|*.dkm|Text|*.map",2,".dkm")=="map"&&FilePreviewDialogPlan.Extension("All|*.*",1,".dkz")=="dkz","Filter pairs or changing Save extension differs.");Reject(()=>FilePreviewDialogPlan.NativeFilter("broken"));
        for(var mask=0;mask<8;mask++){var flags=FilePreviewDialogPlan.Flags((mask&1)!=0,(mask&2)!=0,(mask&4)!=0);Expect((flags&0x000800A0)==0x000800A0&&(flags&8)!=0&&((flags&0x1000)!=0)==((mask&2)!=0)&&((flags&2)!=0)==((mask&5)==5),"Chooser validation/overwrite/no-CWD flags differ.");}
        using(var epoch=new FilePreviewEpoch())
        {
            var first=epoch.Begin();var second=epoch.Begin();var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var stale=Task.Run(async()=>{await release.Task;return epoch.IsCurrent(first);});
            Expect(epoch.IsCurrent(second),"Latest preview lost ownership.");release.SetResult();Expect(!stale.GetAwaiter().GetResult(),"Late old read can replace current preview.");epoch.Dispose();Expect(!epoch.IsCurrent(second),"Closed dialog accepts queued preview.");Reject(()=>epoch.Begin());
        }
        var temp=Path.Combine(Path.GetTempPath(),"FileChooserPreview-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);var previews=0;
        try
        {
            foreach(var native in new[]{true,false})
            {
                var d=NativeMapDocument.CreateNewMap(new("FilePreview","FilePreview",24,24,NativeFormat:native));
                d.Paint([new(2,2)],new(6,2,1));d.Paint([new(3,2)],new(4,2,1));d.Paint([new(4,2)],new(14,3,1));
                foreach(var (legacy,structure,id) in new[]{("Creature","GAMEKEEPER",6u),("Door","GAMEDOOR",1u),("Trap","GAMETRAP",1u),("Object","GAMEOBJECT",1u),("MagicalObject","GAMEOBJECT",121u)})
                {var record=native?d.AddRecord(RecordTemplate.All.Single(t=>t.Structure==structure),new(8,8),3):d.AddLegacyRecord(legacy,new(8,8),3);var field=native?ValueCatalogues.PrimaryField(record)!:record.Fields.Single(f=>f.Name=="id");d.SetField(record,field,id.ToString(System.Globalization.CultureInfo.InvariantCulture));}
                var path=d.SaveCopy(Path.Combine(temp,native?"native":"project"));var area=Path.Combine(temp,native?"native.dkz":"project.dkz");SelectionFile.SaveConfirmed(area,d.CopySelection(new(0,0,24,24)));
                var protectedFiles=Directory.GetFiles(temp,"*",SearchOption.AllDirectories).ToDictionary(f=>f,f=>SHA256.HashData(File.ReadAllBytes(f)));var bytes=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo,d.HasChanges);var notices=0;d.Changed+=(_,_)=>notices++;
                var image=EditorFilePreview.Read(path,EditorFilePreviewKind.Map);var selection=EditorFilePreview.Read(area,EditorFilePreviewKind.Area);var areaViaMap=EditorFilePreview.Read(area,EditorFilePreviewKind.Map);var expected=d.CalculateStatistics().Things;
                Expect(image.Width==24&&image.Height==24&&!image.Bitmap&&image.Raster is { } raster&&raster.Bgra.Span.SequenceEqual(MapOverviewPalette.RenderBgra(d)),"Map preview dimensions/top-down BGRA differ.");
                var counts=new EditorFilePreviewCounts(expected.Creatures,expected.Doors,expected.Traps,expected.Objects,expected.SpecialItems);Expect(image.Counts==counts&&selection.Counts==counts&&areaViaMap.Counts==counts,"Source map/AREA object counts differ.");
                Expect(selection.Raster!.Bgra.Span.SequenceEqual(image.Raster!.Bgra.Span),"AREA image differs from full saved selection.");
                foreach(var input in protectedFiles)Expect(SHA256.HashData(File.ReadAllBytes(input.Key)).AsSpan().SequenceEqual(input.Value),"Preview wrote input/sidecar.");
                Expect(protectedFiles.Count==Directory.GetFiles(temp,"*",SearchOption.AllDirectories).Length&&bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&state==(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo,d.HasChanges)&&notices==0,"Preview changed live document/history/dirty/notifications or created files.");
                using var cancelled=new CancellationTokenSource();cancelled.Cancel();Reject(()=>EditorFilePreview.Read(path,EditorFilePreviewKind.Map,cancelled.Token));previews+=3;
                if(!native)
                {var textPath=Path.Combine(temp,"level.map");File.WriteAllText(textPath,TextMapFile.Project(d,TextMapScriptMode.OriginalNoScenarios).Text,Encoding.UTF8);var text=EditorFilePreview.Read(textPath,EditorFilePreviewKind.Map);Expect(text.Width==24&&text.Height==24&&text.Raster is not null,"Historical text map preview missing.");previews++;}
            }
            var bmp=Path.Combine(temp,"thumbnail.bmp");var pixels=Enumerable.Repeat((byte)255,2*3*4).ToArray();File.WriteAllBytes(bmp,MapThumbnail.EncodeBmp(new(2,3,pixels)));var bitmap=EditorFilePreview.Read(bmp,EditorFilePreviewKind.Bitmap);Expect(bitmap.Width==2&&bitmap.Height==3&&bitmap.Bitmap&&bitmap.Raster is null&&bitmap.Counts is null,"BMP metadata differs.");
            var topdown=File.ReadAllBytes(bmp);BinaryPrimitives.WriteInt32LittleEndian(topdown.AsSpan(22),-3);var top=Path.Combine(temp,"top.bmp");File.WriteAllBytes(top,topdown);Expect(EditorFilePreview.Read(top,EditorFilePreviewKind.Bitmap).Height==3,"Top-down BMP metadata rejected.");
            var broken=Path.Combine(temp,"broken.bmp");File.WriteAllBytes(broken,new byte[12]);Reject(()=>EditorFilePreview.Read(broken,EditorFilePreviewKind.Bitmap));Reject(()=>EditorFilePreview.Read(Path.Combine(temp,"missing.dkz"),EditorFilePreviewKind.Area));previews+=2;
        }
        finally{Directory.Delete(temp,true);}
        Console.WriteLine($"FILE CHOOSER PREVIEW DETAIL {previews}native/DKM/AREA/text/BMP detached previews;5object categories/BGRA/source bytes/sidecars/live history/dirty/notifications read-only;cancelled/malformed/missing inputs;latest-wins/closed epochs;SDK {IntPtr.Size*8}bit ABI/template/filter/type extension/overwrite flags. Native chooser/GDI/WPF not executed.");
    }
}
