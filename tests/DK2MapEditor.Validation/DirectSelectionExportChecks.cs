using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class DirectSelectionExportChecks
{
    private static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(byte[] bytes)
    {try{SelectionFile.Parse(bytes);}catch(InvalidDataException){return;}throw new InvalidOperationException("Altered direct projection was accepted.");}
    private static byte[] Envelope(IEnumerable<LegacySection> sections)
    {
        var bytes=LegacySectionFile.Create(sections).ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes,LegacySectionFile.AreaFileSignature);return bytes;
    }
    public static void Run()
    {
        using var fixture=JsonDocument.Parse(typeof(DirectSelectionExportChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.DirectSelectionExportSource.json")!);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root is not null&&!File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;
        foreach(var hash in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName,hash.Name)))).Equals(hash.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Direct selection source changed.");
        Expect(fixture.RootElement.GetProperty("AllCreaturesThenFirstCategories").EnumerateArray().Select(value=>value.GetString()).SequenceEqual(new[]{"Creature","Door","Trap","Object","MagicalObject"}),"Source direct category order differs.");
        foreach(var native in new[]{false,true})
        {
            var doc=NativeMapDocument.CreateNewMap(new("DirectSelection","DirectSelection",16,16,NativeFormat:native));
            NativeRecord Add(string kind,int id,int x,int y)
            {
                var record=native?doc.AddRecord(RecordTemplate.All.Single(template=>template.Structure==(kind switch{"Creature"=>"GAMEKEEPER","Door"=>"GAMEDOOR","Trap"=>"GAMETRAP",_=>"GAMEOBJECT"})),new(x,y))
                    :doc.AddLegacyRecord(kind,new(x,y));
                var name=native?kind switch{"Creature"=>"bGlobalCreatureID","Door"=>"bDoorID","Trap"=>"bTrapID",_=>"bObjectID"}:"id";
                doc.SetField(record,record.Fields.Single(field=>field.Name==name),id.ToString());return record;
            }
            // Insertion order deliberately differs from Y/X/category order.
            Add("Object",4,7,6);
            Add("MagicalObject",117,4,5);Add("MagicalObject",118,4,5);
            Add("Trap",4,4,5);Add("Trap",5,4,5);
            Add("Object",1,4,5);Add("Object",3,4,5);
            Add("Creature",17,4,5);Add("Creature",14,4,5);
            Add("Door",6,4,5);Add("Door",7,4,5);
            Add("Creature",18,3,5);
            Add("Creature",19,2,2); // outside
            foreach(var owner in doc.ScriptOwners().Where(owner=>owner.Scope is 2 or 128).Take(3).ToArray())
            {var binding=doc.EnsureScriptScope(owner);doc.InsertScriptEvent(binding.Owner,binding.Field,LegacyEventSchemas.All.Single(type=>type.Name=="TRIGGER_TIMEELAPSED"));}
            var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var revision=doc.EditRevision;var history=doc.GetHistory().Undo.Count;var count=doc.EditorChangeCount;
            var selection=doc.CopySelection(new(3,4,6,4));
            byte[] Buffer(SelectionClipboard value)=>SelectionFile.Serialize(value);
            uint[] IDs(byte[] bytes)=>LegacySectionFile.Parse(bytes).Sections.Single(section=>section.Signature==NativeMapDocument.LegacyThings)
                .Payload.ToArray().Chunk(64).Select(item=>BinaryPrimitives.ReadUInt32LittleEndian(item.AsSpan(20))).ToArray();
            var complete=Buffer(selection);var direct=SelectionFile.SerializeDirectSelection(selection);
            Expect(IDs(complete).SequenceEqual(new uint[]{18,17,14,6,7,4,5,1,3,117,118,4}),"Buffer projection no longer clones all category members.");
            Expect(IDs(direct).SequenceEqual(new uint[]{18,17,14,6,4,1,117,4}),"Direct source writer did not keep all creatures/first other members in row/category order.");
            var container=LegacySectionFile.Parse(direct);
            var things=container.Sections.Single(section=>section.Signature==NativeMapDocument.LegacyThings);
            var actual=things.Payload.ToArray();
            var expectedTypes=new uint[]{0,0,0,1,2,3,4,3};
            var expectedPoints=new[]{(0,1),(1,1),(1,1),(1,1),(1,1),(1,1),(1,1),(4,2)};
            for(var index=0;index<expectedTypes.Length;index++)
            {
                Expect(BinaryPrimitives.ReadUInt32LittleEndian(actual.AsSpan(index*64+8))==expectedTypes[index],"Direct source category differs.");
                Expect(actual.AsSpan(index*64,8).IndexOfAnyExcept((byte)0)<0,"Historical projection leaked pointer bytes.");
                Expect(BinaryPrimitives.ReadInt32LittleEndian(actual.AsSpan(index*64+12))==expectedPoints[index].Item1
                    &&BinaryPrimitives.ReadInt32LittleEndian(actual.AsSpan(index*64+16))==expectedPoints[index].Item2,"Direct source coordinates are not relative to the selection.");
            }
            Expect(things.Payload.Length==8*64&&container.Sections.Single(section=>section.Signature==SelectionFile.ClipboardSection).Version==6,"Direct KTGS header/marked extension size differs.");
            var reopened=SelectionFile.Parse(direct);
            Expect(reopened.ObjectCount==12&&reopened.ScriptCount==selection.ScriptCount&&Buffer(reopened).AsSpan().SequenceEqual(complete),"Direct projection lost raw duplicates/graphs or changed buffer serialization.");
            Expect(SelectionFile.SerializeDirectSelection(reopened).AsSpan().SequenceEqual(direct),"Direct explicit round-trip differs.");
            var historical=Envelope(container.Sections.Where(section=>section.Signature!=SelectionFile.ClipboardSection));
            Expect(SelectionFile.Parse(historical).ObjectCount==8,"Original-reader projection gained omitted raw objects.");
            var changed=actual.ToArray();changed[20]^=1;
            Reject(Envelope(container.Sections.Select(section=>section==things?new LegacySection(section.Signature,section.Version,changed):section)));
            var extension=container.Sections.Single(section=>section.Signature==SelectionFile.ClipboardSection);var mode=extension.Payload.ToArray();
            BinaryPrimitives.WriteInt32LittleEndian(mode.AsSpan(mode.Length-4),0);
            Reject(Envelope(container.Sections.Select(section=>section==extension?new LegacySection(section.Signature,section.Version,mode):section)));
            var wrong=LegacySectionFile.Parse(complete).Sections.Single(section=>section.Signature==NativeMapDocument.LegacyThings);
            Reject(Envelope(container.Sections.Select(section=>section==things?wrong:section)));
            foreach(var transformed in new[]{reopened.Mirror(true),reopened.RotateClockwise()})
                Expect(IDs(Buffer(transformed)).SequenceEqual(IDs(complete)),"Saving transformed imported raw buffer collapsed duplicate objects.");
            var directory=Path.Combine(Path.GetTempPath(),"DK2DirectSelection-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                var path=Path.Combine(directory,"direct.dkz");File.WriteAllBytes(path,[1,2,3]);SelectionFile.SaveDirectSelectionConfirmed(path,selection);
                Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(direct)&&Directory.GetFiles(directory).Length==1,"Atomic confirmed direct export differs or leaked temporary files.");
                using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))
                {
                    var refused=false;
                    try{SelectionFile.SaveDirectSelectionConfirmed(path,selection);}
                    catch(Exception error) when(error is IOException or UnauthorizedAccessException){refused=true;}
                    Expect(refused,"Locked direct export destination was replaced.");
                }
                Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(direct)&&Directory.GetFiles(directory).Length==1,"Failed direct replacement changed bytes or leaked a temporary file.");
                var empty=doc.CopySelection(new(10,10,2,2));var emptyBytes=SelectionFile.SerializeDirectSelection(empty);var emptyFile=LegacySectionFile.Parse(emptyBytes);
                Expect(emptyFile.Sections.Single(section=>section.Signature==NativeMapDocument.LegacyThings).Payload.IsEmpty&&SelectionFile.Parse(emptyBytes).ObjectCount==0,"Extended empty export must keep supported KTGS before DKCP.");
            }
            finally
            {
                var resolved=Path.GetFullPath(directory);var temporaryRoot=Path.GetFullPath(Path.GetTempPath());
                if(!resolved.StartsWith(temporaryRoot,StringComparison.OrdinalIgnoreCase)
                    ||!Path.GetFileName(resolved).StartsWith("DK2DirectSelection-",StringComparison.Ordinal)
                    ||(File.GetAttributes(resolved)&FileAttributes.ReparsePoint)!=0)
                    throw new InvalidOperationException("Refusing to remove an unverified test directory.");
                Directory.Delete(resolved,true);
            }
            Expect(doc.EditRevision==revision&&doc.GetHistory().Undo.Count==history&&doc.EditorChangeCount==count&&before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Direct export mutated source bytes/history/counters.");
            Console.WriteLine($"DIRECT SELECTION DETAIL native={native} source first-member projection,12raw/8legacy objects, graphs/relative coordinates/pointers, explicit mode/tamper guards, empty KTGS and atomic save; buffer remains lossless");
        }
    }
}
