using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class ScenarioPointBandPreviewChecks
{
    private static void Expect(bool value,string message) { if (!value) throw new Exception(message); }
    private static byte[] Bytes(NativeFile file)
    { Expect(MemoryMarshal.TryGetArray(file.Data,out var raw),"Independent point band fixture inaccessible.");return raw.Array!; }
    private static void Reject(Action action)
    { try{action();}catch(InvalidOperationException){return;}throw new Exception("Closed/foreign/stale point band snapshot accepted."); }
    public static void Run()
    {
        using var fixture=JsonDocument.Parse(typeof(ScenarioPointBandPreviewChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.GlobalDefinitionImagesSource.json")!);
        var creatures=fixture.RootElement.GetProperty("Entries").EnumerateArray().Where(row=>row.GetProperty("Family").GetString()=="Creatures")
            .Select(row=>(ID:row.GetProperty("ID").GetUInt32(),Resource:row.GetProperty("Resource").GetInt32())).ToArray();
        Expect(creatures.Length==45,"Source45creature fixture missing.");
        foreach(var native in new[]{true,false})
        {
            var document=NativeMapDocument.CreateNewMap(new("PointBandDetails","PointBandDetails",20,20,NativeFormat:native));
            NativeRecord Add(string id,string name)
            {
                var record=native?document.AddRecord(RecordTemplate.All.Single(t=>t.Structure=="GAMEBAND"),new(2,2)):document.AddLegacyRecord("Band",new(2,2));
                var identity=EntityReferences.Identity(record)!.Field;identity.Encode(id).CopyTo(Bytes(record.File),record.Offset+identity.Offset);
                if(record.Fields.FirstOrDefault(f=>f.Name=="name") is {} label)label.Encode(name).CopyTo(Bytes(record.File),record.Offset+label.Offset);
                return record;
            }
            void Seed(NativeRecord record,int index,uint id,int level,uint behavior,uint objective,uint area,bool active=true)
            {
                var raw=Bytes(record.File);var offset=record.Offset+(native?51+index*32:12+index*44);
                if(native){raw[offset+14]=checked((byte)level);raw[offset+15]=active?checked((byte)behavior):(byte)0;raw[offset+16]=checked((byte)area);raw[offset+27]=checked((byte)objective);raw[offset+28]=checked((byte)id);}
                else
                {
                    BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(offset),active?1:0);BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(offset+4),level);
                    foreach(var (at,value) in new[]{(12,id),(28,objective),(32,behavior),(36,area)})BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(offset+at),value);
                }
            }
            var bands=new List<NativeRecord>();
            for(var group=0;group<3;group++)
            {
                var band=Add(group.ToString(),"Physical "+group);bands.Add(band);
                for(var slot=0;slot<16&&group*16+slot<45;slot++)Seed(band,slot,creatures[group*16+slot].ID,slot%10+1,slot%2==0?3u:1u,23,47);
            }
            var gap=Add("3","Gap");bands.Add(gap);Seed(gap,0,250,native?255:-3,3,255,41);Seed(gap,1,17,1,1,19,0,false);Seed(gap,2,17,9,3,23,77);
            var duplicate=Add("0","Duplicate\nname");bands.Add(duplicate);Seed(duplicate,0,17,2,1,23,88);
            bands.Add(Add("5","Empty"));
            var bytes=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(document.EditRevision,document.HasChanges,document.CanUndo,document.CanRedo,document.EditorChangeCount);
            using var session=document.CreateEditorScenarioPointSession(initialScope:8);
            var snapshots=document.ReadScenarioPointBandPreviews(session);
            Expect(snapshots.Count==6&&snapshots.Select(s=>s.Record.Offset).SequenceEqual(bands.Select(b=>b.Offset)),"Physical band order/duplicate records merged.");
            Expect(snapshots.Count(s=>s.Preview.ID=="0")==2,"Duplicate/zero source band IDs renumbered.");
            Expect(snapshots.All(s=>s.Preview.Complete),"Verified point band layout not completely read.");
            for(var i=0;i<45;i++)
            {
                var member=snapshots[i/16].Preview.Members[i%16];
                Expect(member.CreatureID==creatures[i].ID&&member.ImageResource==creatures[i].Resource&&member.Creature is not null,"Member source image/name ID mapping lost.");
                Expect(member.IsLeader==(i%16%2==0)&&member.Objective==23&&member.Area==47,"Leader objective/target-area bytes misread.");
            }
            Expect(snapshots[3].Preview.Members.Count==1&&snapshots[3].Preview.Members[0] is {CreatureID:250,Creature:null,ImageResource:null,Objective:255,Area:41},"Gap/unknown member data normalized or tail shown.");
            Expect(snapshots[3].Preview.Members[0].Level==(native?255:-3),"Imported raw level clamped by read-only preview.");
            Expect(native?snapshots[4].Name=="":snapshots[4].Name=="Duplicate\nname","Native invented a name or project name changed.");
            var choices=session.Choices;var rows=ScenarioPointBandRows.Build(8,snapshots,choices);
            Expect(rows.Count==53&&rows.Count(r=>r.IsHeader)==6&&rows.Where(r=>!r.IsHeader).All(r=>r.Choice is null&&!r.CanSelect),"Band page granted choice authority to details.");
            Expect(rows.Where(r=>r.IsHeader).All(r=>r.Choice is null||choices.Contains(r.Choice)),"Band header invented selection authority.");
            session.SelectScope(16);choices=session.Choices;rows=ScenarioPointBandRows.Build(16,snapshots,choices);
            Expect(rows.Count==53&&rows.Where(r=>r.IsHeader).All(r=>r.Choice is null&&!r.CanSelect),"Member page granted choice authority to headers.");
            foreach(var row in rows.Where(r=>!r.IsHeader))
            {
                Expect(row.Choice is not null&&choices.Contains(row.Choice)&&row.Choice.Owner.MemberIndex==row.Member!.Index,"Member choice attached to another slot.");
                var physical=row.Choice!.Owner.BandRecord??row.Choice.Owner.Record;
                Expect(physical.File==row.Band.Record.File&&physical.Offset==row.Band.Record.Offset,"Duplicate IDs attached member to wrong physical band.");
                Expect(row.CanSelect==row.Choice.CanSelect,"Display changed semantic owner eligibility.");
            }
            Expect(rows.Where(r=>r.Band.Record.Offset==gap.Offset&&!r.IsHeader).Count()==1,"Member page exposed an imported tail after inactive slot.");
            var oldName=snapshots[4].Name;var oldMember=snapshots[0].Preview.Members[0];
            foreach(var file in document.Files)Expect(file.Data.Span.SequenceEqual(bytes[file]),"Band group display changed map bytes.");
            Expect(state==(document.EditRevision,document.HasChanges,document.CanUndo,document.CanRedo,document.EditorChangeCount),"Band group display changed history/count.");
            var foreign=NativeMapDocument.CreateNewMap(new("OtherPoint","OtherPoint",20,20,NativeFormat:native));Reject(()=>foreign.ReadScenarioPointBandPreviews(session));
            using(var closed=document.CreateEditorScenarioPointSession(initialScope:8)){closed.Dispose();Reject(()=>document.ReadScenarioPointBandPreviews(closed));}
            var rawFirst=Bytes(bands[0].File);var levelAt=bands[0].Offset+(native?51+14:12+4);rawFirst[levelAt]^=1;
            Reject(()=>document.ReadScenarioPointBandPreviews(session));
            Expect(snapshots[4].Name==oldName&&snapshots[0].Preview.Members[0]==oldMember,"Cached preview reread mutable document after capture.");
        }
        Console.WriteLine("POINT BAND DETAIL45 source creatures/both owner pages/native+DKM/physical duplicates+empty+gap+unknown/frozen snapshots+eligibility/read-only state verified; no App/WPF execution.");
    }
}
