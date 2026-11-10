using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class EditorResizePlayerChecks
{
    private static readonly string[] Coordinates = ["CameraX", "CameraY", "CPUAreaX1", "CPUAreaY1", "CPUAreaX2", "CPUAreaY2"];
    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject(Action action) { try { action(); } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException) { return; } throw new InvalidOperationException("Invalid source player resize accepted."); }
    private static NativeMapDocument New(bool native) => NativeMapDocument.CreateNewMap(new("PlayerResize", "PlayerResize", 32, 30, Players: 5, NativeFormat: native));
    private static string Name(NativeRecord record, bool project, string coordinate) => project ? record.Category == "Project.MAPOPTIONSV1" ? coordinate : "Player" + coordinate
        : "bPlayer" + (coordinate.StartsWith("Camera", StringComparison.Ordinal) ? coordinate : "BuildArea" + coordinate[7..]);
    private static int[] Seed(uint id) => id switch {
        <= 3 => [8,9,10,11,12,13], 4 => [5,7,8,9,11,13], 5 => [-1,7,-1,6,21,22],
        6 => [2,-1,2,-1,-1,12], _ => [int.MaxValue,int.MinValue,int.MaxValue,int.MinValue,-2,4] };
    private static void SeedPlayers(NativeMapDocument doc)
    {
        foreach (var row in doc.PlayerSettingsRows())
        {
            var computer = row.Record.Fields.Single(field => field.Name == (doc.IsLegacyProject ? "PlayerComputer" : "bPlayerCPU"));
            doc.SetField(row.Record, computer, (row.ID == 3 ? 0 : row.ID == 5 ? 2 : 1).ToString(CultureInfo.InvariantCulture));
            foreach (var (name, value) in Coordinates.Zip(Seed(row.ID)))
                doc.SetField(row.Record, row.Record.Fields.Single(field => field.Name == Name(row.Record, doc.IsLegacyProject, name)),
                    (doc.IsLegacyProject ? value : unchecked((ushort)value)).ToString(CultureInfo.InvariantCulture));
        }
    }
    private static Dictionary<uint, byte[]> PlayerBytes(NativeMapDocument doc) => doc.PlayerSettingsRows().ToDictionary(row => row.ID, row => row.Record.File.Data.Span.Slice(row.Record.Offset,row.Record.Length).ToArray());
    private static void WriteExpected(byte[] bytes, NativeField field, int value, bool project)
    { if (project) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(field.Offset),value); else BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(field.Offset),unchecked((ushort)value)); }
    private static void CheckPlayers(NativeMapDocument doc, Dictionary<uint, byte[]> expected) => Expect(doc.PlayerSettingsRows().All(row => row.Record.File.Data.Span.Slice(row.Record.Offset,row.Record.Length).SequenceEqual(expected[row.ID])), "Resize changed an unexpected player byte.");
    public static void Run()
    {
        using var capture=JsonDocument.Parse(typeof(EditorResizePlayerChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.EditorResizePlayersSource.json")!);
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!File.Exists(Path.Combine(root.FullName,"DK2MapEditor.slnx")))root=root.Parent;
        foreach(var input in capture.RootElement.GetProperty("InputHashes").EnumerateObject())
            Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root!.FullName,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Original player relocation source changed.");
        Expect(capture.RootElement.GetProperty("FirstKeeperID").GetInt32()==3&&capture.RootElement.GetProperty("LastKeeperID").GetInt32()==7
            &&capture.RootElement.GetProperty("SourceCounterIncrements").GetInt32()==0&&capture.RootElement.GetProperty("NativeFFFFIsNotProjectSentinel").GetBoolean(),"Unverified original player recipe.");
        foreach(var native in new[]{false,true})foreach(var size in new[]{(35,33),(29,27),(35,27)})foreach(var anchor in Enumerable.Range(0,9))
        {
            var doc=New(native);SeedPlayers(doc);var originalPlayers=PlayerBytes(doc);var expected=PlayerBytes(doc);
            var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var counter=doc.EditorChangeCount;var history=doc.GetHistory().Undo.Count;
            var dx=(size.Item1-32)*(anchor%3)/2;var dy=(size.Item2-30)*(anchor/3)/2;
            foreach(var row in doc.PlayerSettingsRows().Where(row=>row.ID>=4)) // heroes1/2 and human3 stay byte-exact
            {
                var values=Seed(row.ID);if(native)values=values.Select(value=>(int)unchecked((ushort)value)).ToArray();
                foreach(var index in Enumerable.Range(0,6))
                {
                    if(!native&&values[index<2?0:2]==-1)continue; // one X sentinel controls its entire group
                    var field=row.Record.Fields.Single(field=>field.Name==Name(row.Record,!native,Coordinates[index]));
                    WriteExpected(expected[row.ID],field,unchecked(values[index]+(index%2==0?dx:dy)),!native);
                }
            }
            var session=doc.CreateMapPropertiesSession();var draftBefore=session.WorkingDocument.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            var plan=doc.PrepareEditorMapProperties(session,new(size.Item1,size.Item2,anchor));
            Expect(!plan.NeedsCropConsent&&doc.EditorMapPropertiesChangeIncrements(plan)==2,"Player fields invented crop consent or per-player counts.");
            Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&draftBefore.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Player resize preview leaked bytes.");
            doc.ApplyEditorMapProperties(plan);CheckPlayers(doc,expected);
            Expect(doc.EditorChangeCount==unchecked(counter+2)&&doc.GetHistory().Undo.Count==history+1,"Player relocation was not one common Undo/two parent source counts.");
            var after=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());doc.Undo();CheckPlayers(doc,originalPlayers);
            Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&doc.EditorChangeCount==counter,"Player resize Undo lost complete buffers/count.");
            doc.Redo();Expect(after.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Player resize Redo lost complete buffers.");
            var saved=SaveProjectionChecks.SaveCopy(doc,Path.Combine(Path.GetTempPath(),"DK2PlayerResize",Guid.NewGuid().ToString("N")));
            CheckPlayers(NativeMapDocument.Load(saved),expected);
        }
        LiteralAndGeneric();Historical();WarningsAndCompilation();
        CompositionAndGuards();
        Console.WriteLine("EDITOR RESIZE PLAYER DETAIL 54 native/project nine-anchor matrices x seven players; literal LONG/WORD wrap and paired sentinels, nativeFFFF import, human/heroes retention, V1/V2 opaque cameras/heart bytes, preview/cancel/full UndoRedo/save/reopen, generic clamp retained and nonblocking source compiler warnings");
    }
    private static void LiteralAndGeneric()
    {
        foreach(var native in new[]{false,true})
        {
            var doc=New(native);SeedPlayers(doc);using var session=doc.CreateMapPropertiesSession();doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(session,new(36,34,8)));
            int[][] project=[[8,9,10,11,12,13],[8,9,10,11,12,13],[8,9,10,11,12,13],[9,11,12,13,15,17],[-1,7,-1,6,21,22],[6,3,6,3,3,16],[-2147483645,-2147483644,-2147483645,-2147483644,2,8]];
            int[][] game=[[8,9,10,11,12,13],[8,9,10,11,12,13],[8,9,10,11,12,13],[9,11,12,13,15,17],[3,11,3,10,25,26],[6,3,6,3,3,16],[3,4,3,4,2,8]];
            foreach(var row in doc.PlayerSettingsRows())foreach(var index in Enumerable.Range(0,6))
                Expect(int.Parse(row.Record.Read(row.Record.Fields.Single(field=>field.Name==Name(row.Record,!native,Coordinates[index]))),CultureInfo.InvariantCulture)==(native?game:project)[row.ID-1][index],"Literal source player relocation differs.");
            doc=New(native);SeedPlayers(doc);doc.Resize(36,34,4,4,new(2,2,1));
            var human=doc.PlayerGeometries().Single(row=>row.ID==3);Expect(human.Camera==new TilePoint(12,13)&&human.BuildStart==new TilePoint(14,15),"Generic resize lost its prior all-player behavior.");
            var extreme=doc.PlayerGeometries().Single(row=>row.ID==7);
            Expect(extreme.Camera==new TilePoint(native?65535:35,native?4:0),"Generic resize lost independent FFFF retention/clamping.");
            var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            using(var canceled=doc.CreateMapPropertiesSession())_=doc.PrepareEditorMapProperties(canceled,new(32,30));
            Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Canceled player resize changed the owner.");
        }
    }
    private static void Historical()
    {
        var seed=New(false);SeedPlayers(seed);var file=LegacySectionFile.Read(SaveProjectionChecks.SaveCopy(seed,Path.Combine(Path.GetTempPath(),"DK2PlayerVersions",Guid.NewGuid().ToString("N"))));
        foreach(var version in new[]{1,2})
        {
            var fields=LegacyProjectSchemas.Fields("MAPOPTIONSV"+version);var stride=LegacyProjectSchemas.Size("MAPOPTIONSV"+version);var bytes=new byte[stride*7];
            foreach(var row in seed.PlayerSettingsRows())foreach(var field in fields)
            {
                var sourceName=version==1?field.Name switch {"Gold"=>"PlayerGold","Health"=>"PlayerHealth","ComputerPlayer"=>"PlayerComputer","AI"=>"PlayerAI",_ when field.Name.StartsWith("CPUArea",StringComparison.Ordinal)=>"Player"+field.Name,_=>field.Name}:field.Name;
                if(row.Record.Fields.SingleOrDefault(item=>item.Name==sourceName) is { } from&&from.Size==field.Size)
                    row.Record.File.Data.Span.Slice(row.Record.Offset+from.Offset,from.Size).CopyTo(bytes.AsSpan(((int)row.ID-1)*stride+field.Offset));
                if(field.Name is "HeartX" or "HeartY" or "PlayerCameraX" or "PlayerCameraY")BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(((int)row.ID-1)*stride+field.Offset),777);
            }
            var path=Path.Combine(Path.GetTempPath(),"DK2PlayerVersions",Guid.NewGuid().ToString("N")+".dkm");Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path,LegacySectionFile.Create(file.Sections.Select(section=>section.Signature==NativeMapDocument.LegacyPlayers?new LegacySection(section.Signature,version,bytes):section)).ToArray());
            var doc=NativeMapDocument.Load(path);var expected=PlayerBytes(doc);var before=doc.Files.ToDictionary(item=>item,item=>item.Data.ToArray());
            foreach(var row in doc.PlayerSettingsRows().Where(row=>row.ID>=4))
            {
                var values=Seed(row.ID);if(values[2]==-1)continue;
                foreach(var index in Enumerable.Range(2,4))WriteExpected(expected[row.ID],row.Record.Fields.Single(field=>field.Name==Name(row.Record,true,Coordinates[index])),unchecked(values[index]+4),true);
            }
            using var session=doc.CreateMapPropertiesSession();doc.ApplyEditorMapProperties(doc.PrepareEditorMapProperties(session,new(36,34,8)));CheckPlayers(doc,expected);
            Expect(doc.PlayerSettingsRows().All(row=>row.Record.File.Data.Span[4]==version)&&doc.PlayerGeometries().All(row=>!row.CameraEditable),"Historical player resize upgraded or enabled ignored cameras.");
            doc.Undo();Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Historical player resize Undo lost ignored bytes.");
        }
    }
    private static void WarningsAndCompilation()
    {
        foreach(var native in new[]{false,true})
        {
            var doc=New(native);SeedPlayers(doc);var bytes=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
            var diagnostics=NativeMapValidator.Validate(doc,includeScripts:false).Where(item=>item.Code is "DK2N030" or "DK2N031").ToArray();
            Expect(diagnostics.Length==(native?6:2)&&diagnostics.All(item=>item.Severity==DiagnosticSeverity.Warning),"Player source upper-bound warnings became blocking errors or checked heroes/human CPU area.");
            Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Player warnings modified data.");
        }
        var project=New(false);project.SetPlayerCamera(3,new(28,27));project.SetPlayerBuildArea(3,new(22,21,8,8));
        using(var session=project.CreateMapPropertiesSession())project.ApplyEditorMapProperties(project.PrepareEditorMapProperties(session,new(24,22)));
        var warnings=NativeMapValidator.Validate(project).Where(item=>item.Code is "DK2N030" or "DK2N031").ToArray();
        Expect(warnings.Length==1&&warnings[0].Code=="DK2N030","Human camera warning/source CPU filter differs.");
        _=project.CompileLegacyProject("PlayerWarningOnly",NativeMapDocument.Create(24,22,"Definitions"));
        using(var session=project.CreateMapPropertiesSession())
        {
            project.SetHistoryDepth(0);project.ApplyEditorMapProperties(project.PrepareEditorMapProperties(session,new(28,26,8)));
            Expect(!project.CanUndo&&project.GetHistory().SnapshotBytes==0,"Zero-depth player resize retained Undo data.");
        }
    }
    private static void CompositionAndGuards()
    {
        foreach(var native in new[]{false,true})
        {
            var doc=New(native);var before=doc.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var baseline=doc.EditorChangeCount;
            using var session=doc.CreateMapPropertiesSession();var working=session.WorkingDocument;var player=working.PlayerSettingsRows().Single(row=>row.ID==3);
            working.SetPlayerSettings([new(player.ID,player.Name,(int)player.Gold,true,player.Health is null?null:(int)player.Health)]);
            working.SetPlayerCamera(3,new(8,9));working.SetPlayerBuildArea(3,new(4,5,8,8));
            var plan=doc.PrepareEditorMapProperties(session,new(36,34,8));Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Private CPU choice or resize leaked into live player.");
            doc.ApplyEditorMapProperties(plan);var geometry=doc.PlayerGeometries().Single(row=>row.ID==3);
            Expect(geometry.Camera==new TilePoint(12,13)&&geometry.BuildStart==new TilePoint(8,9)&&doc.EditorChangeCount==unchecked(baseline+2),"Common resize ignored the private CPU choice or added child counts.");
            doc.Undo();Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Composed CPU player resize did not undo atomically.");
        }
        var malformed=New(false);var keeper=malformed.PlayerSettingsRows().Single(row=>row.ID==3);
        foreach(var (suffix,value) in new[]{("Computer",1),("CameraX",-1),("CameraY",99),("CPUAreaX1",-1),("CPUAreaY1",99),("CPUAreaX2",99),("CPUAreaY2",99)})
            malformed.SetField(keeper.Record,keeper.Record.Fields.Single(field=>field.Name=="Player"+suffix),value.ToString(CultureInfo.InvariantCulture));
        var expected=PlayerBytes(malformed);using(var session=malformed.CreateMapPropertiesSession())malformed.ApplyEditorMapProperties(malformed.PrepareEditorMapProperties(session,new(36,34,8)));
        CheckPlayers(malformed,expected);Expect(NativeMapValidator.Validate(malformed).Count(item=>item.Code is "DK2N030" or "DK2N031")==2,"Malformed sentinel pairs lost independent source warning checks.");
        var duplicate=New(true);SeedPlayers(duplicate);var record=duplicate.PlayerSettingsRows().Single(row=>row.ID==4).Record;
        Expect(MemoryMarshal.TryGetArray(record.File.Data,out var backing),"Player fixture backing unavailable.");
        backing.Array![backing.Offset+record.Offset+record.Fields.Single(field=>field.Name=="bGlobalPlayer").Offset]=3;
        var bytes=duplicate.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
        Reject(()=>{using var session=duplicate.CreateMapPropertiesSession();_=duplicate.PrepareEditorMapProperties(session,new(36,34,8));});
        Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value)),"Ambiguous native keeper relocation changed the owner.");
    }
}
