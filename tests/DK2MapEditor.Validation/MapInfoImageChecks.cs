using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class MapInfoImageChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static void ReadOnly(NativeMapDocument d,Action check)
    {
        var bytes=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo,d.HasChanges);var notices=0;void Changed(object? s,EventArgs e)=>notices++;d.Changed+=Changed;
        try{check();Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&state==(d.EditRevision,d.EditorChangeCount,d.CanUndo,d.CanRedo,d.HasChanges)&&notices==0,"Info query changed bytes/history/dirty/notifications.");}finally{d.Changed-=Changed;}
    }
    public static void Run()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"DK2MapEditor.slnx")))directory=directory.Parent;var root=directory?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(MapInfoImageChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.MapInfoImagesSource.json")!);var source=fixture.RootElement;
        foreach(var input in source.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Info image source changed.");
        var cases=0;var temp=Path.Combine(Path.GetTempPath(),"MapInfoGold-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try
        {
            foreach(var native in new[]{true,false})
            {
                var d=NativeMapDocument.CreateNewMap(new("InfoGold","InfoGold",20,18,NativeFormat:native));d.Paint(MapDrawing.Rectangle(new(0,0,20,18),true),new(2,2,1));
                var gold=(from y in Enumerable.Range(0,18) from x in Enumerable.Range(0,20) where (x*3+y*5)%7==0 select new TilePoint(x,y)).ToArray();d.Paint(gold,new(6,2,1));d.Paint([new(1,1)],new(7,2,1)); // Gem is never finite gold.
                gold=gold.Where(point=>point!=new TilePoint(1,1)).ToArray();var snapshot=d.CaptureMapInfoGold();Expect(snapshot.Total==gold.Length*3000&&snapshot.Total==d.CalculateStatistics().TerrainGold,"Source default map gold differs.");
                ReadOnly(d,()=>
                {
                    foreach(var x in Enumerable.Range(-2,25))foreach(var y in Enumerable.Range(-2,23))foreach(var size in new[]{0,1,4,23,int.MaxValue,-1})
                    {
                        var rect=new TileRect(x,y,size,size);var expected=gold.LongCount(point=>size>0&&rect.Contains(point.X,point.Y))*3000;
                        Expect(snapshot.Within(rect)==expected,"Corrected selection prefix gold differs from independent point oracle.");cases++;
                    }
                    Expect(snapshot.Within(new(int.MinValue,int.MinValue,int.MaxValue,int.MaxValue))==0&&snapshot.Within(new(int.MaxValue,int.MaxValue,1,1))==0,"Extreme rectangle overflowed.");
                });
                var before=snapshot.Total;d.Paint([new(19,17)],new(6,255,1));var after=d.CaptureMapInfoGold().Total;Expect(after==before+(gold.Contains(new(19,17))?0:3000),"World gold incorrectly depends on display owner.");Expect(snapshot.Total==before,"Snapshot changed after edit.");d.Undo();Expect(d.CaptureMapInfoGold().Total==before,"Undo left stale map gold.");
                // Independent legacy definition payload and literal field offsets/order.
                if(!native)
                {
                    var path=d.SaveCopy(Path.Combine(temp,"base"));var sections=LegacySectionFile.Read(path);var stride=LegacyProjectSchemas.Size("GLOBALTERRAIN");var payload=new byte[4+38*stride];BinaryPrimitives.WriteUInt32LittleEndian(payload,1);
                    var field=LegacyProjectSchemas.Fields("GLOBALTERRAIN").Single(f=>f.Name=="GoldValue");for(var index=0;index<38;index++)field.Encode("222").CopyTo(payload,4+index*stride+field.Offset);
                    NativeMapDocument Load(int version,string name){var file=Path.Combine(temp,name+".dkm");File.WriteAllBytes(file,LegacySectionFile.Create(sections.Sections.Where(s=>s.Signature!=NativeMapDocument.LegacyGlobals).Append(new(NativeMapDocument.LegacyGlobals,version,payload))).ToArray());return NativeMapDocument.Load(file);}
                    var project=Load(1,"overrides");ReadOnly(project,()=>Expect(project.CaptureMapInfoGold().Total==gold.Length*222,"Loaded project gold override ignored."));
                    var future=Load(99,"future");ReadOnly(future,()=>{var info=future.CaptureMapInfoGold();Expect(info.Total is null&&info.Within(new(1,1,1,1))==0,"Unsupported gold definition guessed, or empty region became unknown.");});
                    var basisPath=NativeMapDocument.Create(20,18,"Basis").SaveCopy(Path.Combine(temp,"basis"));var terrainBytes=new byte[56+41*552];void Header(int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(terrainBytes.AsSpan(offset),value);
                    Header(0,0x6E);Header(4,4);Header(8,(uint)terrainBytes.Length);Header(12,0x6F);Header(16,28);Header(20,41);Header(48,0x70);Header(52,(uint)terrainBytes.Length-56);File.WriteAllBytes(Path.Combine(temp,"basis","Data","Editor","Terrain.kwd"),terrainBytes);
                    var compiled=project.CompileLegacyProject("InfoGold",NativeMapDocument.Load(basisPath));ReadOnly(compiled,()=>Expect(compiled.CaptureMapInfoGold().Total==gold.Length*222,"Loaded native gold override ignored."));
                }
            }
        }
        finally{Directory.Delete(temp,true);}
        var configuration=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;using var stream=File.OpenRead(Path.Combine(root,"src/DK2MapEditor.App/bin",configuration,"net10.0-windows/DK2MapEditor.App.dll"));using var pe=new PEReader(stream);var metadata=pe.GetMetadataReader();var section=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);var embedded=new Dictionary<string,byte[]>();
        foreach(var handle in metadata.ManifestResources){var resource=metadata.GetManifestResource(handle);if(!resource.Implementation.IsNil)continue;var reader=section.GetReader(checked((int)resource.Offset),section.Length-checked((int)resource.Offset));var length=checked((int)reader.ReadUInt32());embedded.Add(metadata.GetString(resource.Name),reader.ReadBytes(length));}
        foreach(var image in source.GetProperty("InfoImages").EnumerateArray().Concat(source.GetProperty("ExistingLegendImages").EnumerateArray()))
        {
            var bytes=File.ReadAllBytes(Path.Combine(root,image.GetProperty("Source").GetString()!));Expect(Convert.ToHexString(SHA256.HashData(bytes)).Equals(image.GetProperty("SHA256").GetString(),StringComparison.OrdinalIgnoreCase),"Original Info/legend image changed.");Expect(embedded.TryGetValue(image.GetProperty("LogicalName").GetString()!,out var actual)&&bytes.AsSpan().SequenceEqual(actual),"Embedded Info/legend image differs.");
        }
        Console.WriteLine($"MAP INFO DETAIL {cases}native/DKM selection rectangles/default and loaded project+native gold/future unknown/empty region/extreme clipping/owner-independent world gold/immutable snapshot/edit+Undo/read-only bytes/history;3original32px ICO+30existinglegend BMP/PE identities; no WPF execution.");
    }
}
