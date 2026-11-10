using System.Runtime.InteropServices;
using System.Buffers.Binary;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class Program
{
    private static string directory="";
    private static int groups;
    private static uint Version(NativeFile file)=>BinaryPrimitives.ReadUInt32LittleEndian(file.Data.Span[4..]);
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static Dictionary<NativeFile,byte[]> Bytes(NativeMapDocument d)=>d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());
    private static void Exact(Dictionary<NativeFile,byte[]> bytes)=>Check(bytes.All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Live source buffers changed during private preview/refusal/Cancel/Undo.");
    private static void Pass(int version,string name){groups++;Console.WriteLine("PASS project-v"+version+" "+name);}
    private static void Reject(Action action)
    {
        try{action();}catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or OverflowException){return;}
        throw new Exception("Expected historical guard refusal.");
    }
    private static int Main(string[] args)
    {
        if(args.Length!=1 || Directory.Exists(args[0]))throw new ArgumentException("Require fresh private fixtures.");
        directory=args[0];Directory.CreateDirectory(directory);
        try
        {
            for(var version=1;version<=5;version++){Preview(version);Modify(version);New(version);DeleteGuards(version);}
            Check(groups==20,"Expected twenty historical/current reference groups.");Console.WriteLine("HISTORICAL HERO BAND MODEL CHECKS PASSED: 20");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
    }
    private static NativeMapDocument Fixture(int version,string tag)
    {
        var path=Path.Combine(directory,$"v{version}-{tag}");Directory.CreateDirectory(path);
        var seed=NativeMapDocument.CreateNewMap(new("HistoricalBand","HistoricalBand",16,16));
        var saved=seed.SaveCopy(Path.Combine(path,"seed"));var container=LegacySectionFile.Read(saved);
        var entry=Path.Combine(path,"historical.dkm");
        File.WriteAllBytes(entry,LegacySectionFile.Create(container.Sections.Select(s=>s.Signature==NativeMapDocument.LegacyBands?new(s.Signature,version,ReadOnlyMemory<byte>.Empty):s)).ToArray());
        var d=NativeMapDocument.Load(entry);
        for(var ordinal=0;ordinal<2;ordinal++)
        {
            var band=d.AddLegacyRecord("Band",new(1,1));d.AddBandMember(band,17,4,1);d.AddBandMember(band,6,5,3);
            void Set(string name,string value){var field=band.Fields.FirstOrDefault(f=>f.Name==name);if(field is not null)d.SetField(band,field,value);}
            Set("name","Band "+ordinal);Set("heros[0].objective","17");Set("heros[1].objective","23");Set("heros[1].area","41");
            Set("heros[0].options2","43");Set("heros[1].options2","45");Set("heros[0].health","73");Set("heros[1].goldheld","22");
        }
        var result=d.SaveCopy(Path.Combine(path,"fixture"));return NativeMapDocument.Load(result);
    }
    private static NativeRecord[] Bands(NativeMapDocument d)=>d.Records().Where(NativeMapDocument.IsBand).ToArray();
    private static void Preview(int version)
    {
        var d=Fixture(version,"preview");var before=Bytes(d);var history=d.GetHistory();var revision=d.EditRevision;var selected=Bands(d)[1];
        using(var s=d.CreateEditorHeroBandSession(selected))
        {
            Check(s.Name=="Band 1" && s.Members.Count==2,"Historical selected ordinal/name lost.");
            var values=s.Members.Select(m=>m.Values).ToArray();
            Check(values[0].Options2==43 && values[1].Options2==45 && values[0].Health==73,"Source default leader/flags/health differ.");
            Check(values[0].Objective==(version<=2?19u:17u) && values[1].Objective==(version<=2?19u:23u),"Source historical objective defaults differ.");
            Check(values[1].GoldHeld==(version==5?22u:0u),"Missing historical carried gold must become source zero.");
            Check(values[1].Area==(version==1?0u:41u),"Historical source area lost.");
            Exact(before);var child=s.CreateMemberDraft(1);child.Values=child.Values with{Level=9};child.Dispose();Check(s.Members[1].Values.Level==5,"Nested Cancel leaked.");
            s.Replace([1],values[1] with{Level=8});s.SetName("Private name");Exact(before);
        }
        Check(d.EditRevision==revision && d.GetHistory().ChangeCount==history.ChangeCount,"Historical private conversion counted main edit.");Exact(before);
        Pass(version,"source conversion defaults, second-band ordinal, private nested Cancel and whole Cancel preserve all live bytes");
    }
    private static void Modify(int version)
    {
        var d=Fixture(version,"modify");var before=Bytes(d);var h=d.GetHistory();var revision=d.EditRevision;var bands=Bands(d);var firstId=bands[0].Read(bands[0].Fields.Single(f=>f.Name=="id"));
        using var s=d.CreateEditorHeroBandSession(bands[1]);var identity=s.Members[1].ScenarioIdentity;var child=s.CreateMemberDraft(1);child.Values=child.Values with{Level=9};s.ApplyMemberDraft(child);
        var result=d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(s,"Default"));
        Check(result.Offset==12+LegacyProjectSchemas.Size("MTHBAND") && Version(result.File)==5,"Converted second-band offset/header wrong.");
        Check(d.BandMembers(result)[1].Level==9 && Bands(d)[0].Read(Bands(d)[0].Fields.Single(f=>f.Name=="id"))==firstId,"Selected Save altered other band's identity.");
        Check(result.Read(result.Fields.Single(f=>f.Name=="heros[1].uniqueid"))==identity.ToString(),"Historical member scenario identity changed.");
        Check(before.Where(p=>p.Key!=result.File).All(p=>p.Key.Data.Span.SequenceEqual(p.Value)),"Parent upgraded unrelated project sections/scripts.");
        Check(d.GetHistory().Undo.Count==h.Undo.Count+1 && d.EditRevision==revision+1 && d.EditorChangeCount==unchecked(h.EditorChangeCount+1),"Historical conversion+Save must own exactly one source count/Undo.");
        var after=Bytes(d);d.Undo();Exact(before);d.Redo();Exact(after);
        var saved=d.SaveCopy(Path.Combine(directory,$"v{version}-modify-saved"));var reopened=NativeMapDocument.Load(saved);
        Check(Bands(reopened).Length==2 && reopened.BandMembers(Bands(reopened)[1])[1].Level==9,"Historical parent Save/reopen lost selected member.");
        Pass(version,"atomic selected Modify/source count, band-only conversion, identity retention, complete Undo/Redo and actual Save/reopen");
    }
    private static void New(int version)
    {
        var d=Fixture(version,"new");var before=Bytes(d);var h=d.GetHistory();using var s=d.CreateEditorHeroBandSession();
        s.Add(new(17,Options2:43));s.Add(new(6));s.SetName("New band");Exact(before);
        var added=d.ApplyEditorHeroBand(d.PrepareEditorHeroBand(s,"Default"));
        Check(Bands(d).Length==3 && Version(added.File)==5 && d.BandMembers(added).Count(m=>m.Active)==2,"Historical New did not append current band.");
        Check(before.Where(p=>p.Key!=added.File).All(p=>p.Key.Data.Span.SequenceEqual(p.Value)) && d.GetHistory().Undo.Count==h.Undo.Count+1,"Historical New changed other sections/history.");d.Undo();Exact(before);
        using var stale=d.CreateEditorHeroBandSession(Bands(d)[1]);d.Paint([new(3,3)],new(5,2,1));var changed=Bytes(d);Reject(()=>d.PrepareEditorHeroBand(stale,"Default"));Exact(changed);
        Pass(version,"private historical New with source defaults, single Undo and stale historical parent guard");
    }
    private static void DeleteGuards(int version)
    {
        var d=Fixture(version,"delete");var band=Bands(d)[1];var before=Bytes(d);var history=d.GetHistory();var file=band.File;
        d.DeleteEditorHeroBand(band);Check(Version(file)==version && Bands(d).Length==1,"Delete must preserve original historical layout.");
        Check(before.Where(p=>p.Key!=file).All(p=>p.Key.Data.Span.SequenceEqual(p.Value)) && d.GetHistory().Undo.Count==history.Undo.Count+1,"Delete changed other source buffers/history.");d.Undo();Exact(before);
        band=Bands(d)[1];var sentinel=LegacyProjectSchemas.Fields(band.Category[8..]).Single(f=>f.Name=="heros[16].isvalid");Check(MemoryMarshal.TryGetArray(file.Data,out var raw),"Mutable generated fixture.");
        sentinel.Encode("1").CopyTo(raw.Array!,raw.Offset+band.Offset+sentinel.Offset);var invalid=Bytes(d);Reject(()=>d.CreateEditorHeroBandSession(band));
        if(version<5)Reject(()=>d.CreateEditorHeroBandSession());
        else {using var unrelated=d.CreateEditorHeroBandSession();Exact(invalid);}
        Reject(()=>d.DeleteEditorHeroBand(band));Exact(invalid);
        Pass(version,"literal historical Delete/Undo, unrelated bytes retained and malformed seventeenth-slot refusal remains atomic");
    }
}
