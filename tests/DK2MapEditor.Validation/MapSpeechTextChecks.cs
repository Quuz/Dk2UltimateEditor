using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

internal static class MapSpeechTextChecks
{
    private static void Expect(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject(Action action, string message) { var refused = false; try { action(); } catch (Exception e) when (e is ArgumentException or InvalidOperationException or FormatException or OverflowException) { refused = true; } Expect(refused, message); }
    private static Dictionary<NativeFile, byte[]> Snapshot(NativeMapDocument doc) => doc.Files.ToDictionary(file => file, file => file.Data.ToArray());
    private static void Exact(Dictionary<NativeFile, byte[]> bytes) => Expect(bytes.All(row => row.Key.Data.Span.SequenceEqual(row.Value)), "Speech/text changed unrelated bytes or history.");
    private static readonly string[] keys = ["SpeechFile","TextTableID","SpeechPreLevelID","SpeechWinID","SpeechLooseID","SpeechNewsID","SpeechGenericPreLevelID","TextTitleID","TextPlotID","TextDebriefingID","TextObjectiveID","TextSubObjective1ID","TextSubObjective2ID","TextSubObjective3ID"];
    private static readonly string[] nativeNames = ["szMainSpeech","bMainTextTableID","bMainPreLevelSpeechID","bMainWinSpeechID","bMainLooseSpeechID","bMainNewsSpeechID","bMainGenPreLevelSpeechID","bMainTitleTextID","bMainTitlePlotTextID","bMainDebriefingTextID","bMainObjectiveTextID","bMainSubObjective1TextID","bMainSubObjective2TextID","bMainSubObjective3TextID"];
    private static NativeField Field(NativeMapDocument doc, MapSpeechTextDraft draft, string key)
    {
        var index = Array.IndexOf(keys,key); var name = doc.IsLegacyProject ? key : index >= 0 ? nativeNames[index] : key.Replace("TextIntro", "bMainIntro", StringComparison.Ordinal);
        return draft.Record!.Fields.Single(field => field.Name == name);
    }
    private static IEnumerable<MapSpeechTextInput> Inputs(MapSpeechTextDraft draft, string key, string value) => draft.Values.Select(row => new MapSpeechTextInput(row.Key,row.Key==key?value:row.Value));
    public static string ReadOnlyProjection(NativeMapDocument doc)
    {
        var bytes = Snapshot(doc); var revision = doc.EditRevision; var dirty = doc.HasChanges; var history = doc.GetHistory(); var draft = doc.CreateMapSpeechTextDraft();
        Expect(revision == doc.EditRevision && dirty == doc.HasChanges && history.Undo.Count == doc.GetHistory().Undo.Count && history.Redo.Count == doc.GetHistory().Redo.Count,"Speech/text projection mutated history."); Exact(bytes);
        return JsonSerializer.Serialize(new { draft.Editable, draft.Partial, Values=draft.Values.OrderBy(row=>row.Key) });
    }
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(),"DK2SpeechText",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        foreach (var native in new[] { false,true }) Check(native,directory); Historical(directory);
    }
    private static void Check(bool native,string directory)
    {
        var doc = NativeMapDocument.CreateNewMap(new("SpeechText","SpeechText",24,24,PlaceHearts:true,NativeFormat:native));
        MapSpeechTextDraft Draft(int limit=1505) => doc.CreateMapSpeechTextDraft(["SPEECH_LEVEL1","SPEECH_ÉTÉ",new string('X',32),"SPEECH_../bad"],limit);
        var draft=Draft(); Expect(draft.Values.Count==30 && !draft.Partial,"Thirty mapped speech/text fields required.");
        Expect(Field(doc,draft,"TextTableID").Size==(native?1:2) && Field(doc,draft,"SpeechFile").Kind==NativeValueKind.WesternAnsi,"Native byte table ID / ANSI bank layout.");
        doc.SetHistoryDepth(1); var initial=Snapshot(doc); var notifications=0; doc.Changed+=(_,_)=>notifications++;
        var changes=draft.Values.ToDictionary(row=>row.Key,row=>row.Value);
        changes["SpeechFile"]="SPEECH_ÉTÉ"; changes["TextTableID"]="26";
        foreach(var key in MapSpeechText.SpeechIds) changes[key]="1505";
        foreach(var key in MapSpeechText.TextIds) changes[key]="9999";
        for(var i=0;i<8;i++){changes["TextIntroTextID"+i]="9999";changes["TextIntroCreatureID"+i]=MapTextCatalogues.IntroCreatures[^(i+1)].Value;}
        var expected=initial.ToDictionary(row=>row.Key,row=>row.Value.ToArray()); Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        foreach(var row in changes)
        {
            var field=Field(doc,draft,row.Key); var target=expected[draft.Record!.File].AsSpan(draft.Record.Offset+field.Offset,field.Size);
            if(row.Key=="SpeechFile"){target.Clear();Encoding.GetEncoding(1252).GetBytes(row.Value).CopyTo(target);}
            else if(field.Size==1)target[0]=byte.Parse(row.Value,CultureInfo.InvariantCulture);
            else System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(target,ushort.Parse(row.Value,CultureInfo.InvariantCulture));
        }
        doc.ApplyMapSpeechText(doc.UpdateMapSpeechText(draft,changes.Select(row=>new MapSpeechTextInput(row.Key,row.Value))));
        Expect(notifications==1 && doc.GetHistory().Undo.Count==1,"Whole speech/text edit must be one notification/history entry."); Exact(expected);
        doc.Undo(); Exact(initial); doc.Redo(); Exact(expected); doc.Undo();
        // Every real table and creature choice, not only ordinary creatures.
        foreach(var table in MapTextCatalogues.Tables.Where(row=>row.Identifier!=0))
        { draft=Draft();doc.ApplyMapSpeechText(doc.UpdateMapSpeechText(draft,Inputs(draft,"TextTableID",table.Identifier.ToString(CultureInfo.InvariantCulture))));Expect(doc.CreateMapSpeechTextDraft().Values["TextTableID"]==table.Identifier.ToString(CultureInfo.InvariantCulture),"Nonordinal text-table selection.");doc.Undo();Exact(initial); }
        foreach(var creature in MapTextCatalogues.IntroCreatures.Where(row=>row.Value!="0")) foreach(var i in Enumerable.Range(0,8))
        {draft=Draft();doc.ApplyMapSpeechText(doc.UpdateMapSpeechText(draft,Inputs(draft,"TextIntroCreatureID"+i,creature.Value)));Expect(doc.CreateMapSpeechTextDraft().Values["TextIntroCreatureID"+i]==creature.Value,"All eight ordinary/elite/hero replacements.");doc.Undo();Exact(initial);}
        foreach(var key in MapSpeechText.SpeechIds.Concat(MapSpeechText.TextIds).Concat(Enumerable.Range(0,8).Select(i=>"TextIntroTextID"+i)))
        {
            draft=Draft(); Reject(()=>doc.UpdateMapSpeechText(draft,Inputs(draft,key,"-1")),"Negative ID accepted."); Reject(()=>doc.UpdateMapSpeechText(draft,Inputs(draft,key,MapSpeechText.SpeechIds.Contains(key)?"1506":"10000")),"Original ID bound ignored.");
            Reject(()=>doc.UpdateMapSpeechText(draft,Inputs(draft,key,"65536")),"Overflow ID accepted.");
        }
        draft=Draft(); foreach(var (key,value) in new[]{("SpeechFile",new string('X',32)),("SpeechFile","SPEECH_../bad"),("SpeechFile","SPEECH_UNKNOWN"),("TextTableID","40"),("TextIntroCreatureID0","255")}) Reject(()=>doc.UpdateMapSpeechText(draft,Inputs(draft,key,value)),"Unknown/unsafe named value invented."); Exact(initial);
        Reject(()=>doc.UpdateMapSpeechText(draft,[new("SpeechFile","")]),"Partial field request accepted."); Reject(()=>doc.UpdateMapSpeechText(draft,draft.Values.Select(row=>new MapSpeechTextInput("SpeechFile",row.Value))),"Duplicate fields accepted.");
        // Existing invalid IDs/padding/full-width bank strings are immutable on unrelated apply.
        doc.SetField(draft.Record!,Field(doc,draft,"TextTableID"),native?"250":"65000"); doc.SetField(draft.Record!,Field(doc,draft,"SpeechWinID"),"65000"); doc.SetField(draft.Record!,Field(doc,draft,"TextIntroCreatureID0"),"255");
        Expect(MemoryMarshal.TryGetArray(draft.Record!.File.Data,out var backing),"Fixture backing absent."); var speechOffset=backing.Offset+draft.Record.Offset+Field(doc,draft,"SpeechFile").Offset;
        Array.Fill(backing.Array!,(byte)'Z',speechOffset,32); var malformed=Snapshot(doc); draft=Draft();
        var revision=doc.EditRevision;doc.ApplyMapSpeechText(doc.UpdateMapSpeechText(draft,draft.Values.Select(row=>new MapSpeechTextInput(row.Key,row.Value))));Expect(doc.EditRevision==revision,"No-op modified imported values.");
        doc.ApplyMapSpeechText(doc.UpdateMapSpeechText(draft,Inputs(draft,"TextTitleID","123")));Expect(doc.CreateMapSpeechTextDraft().Values["SpeechFile"]==new string('Z',32) && doc.CreateMapSpeechTextDraft().Values["SpeechWinID"]=="65000","Unrelated edit coerced imported bank/ID.");doc.Undo();Exact(malformed);
        draft=Draft();var noneRevision=doc.EditRevision;doc.ApplyMapSpeechText(doc.UpdateMapSpeechText(draft,Inputs(draft,"TextTableID","0")));Exact(malformed);Expect(doc.EditRevision==noneRevision,"Source None changed metadata.");
        draft=Draft(3); Reject(()=>doc.UpdateMapSpeechText(draft,Inputs(draft,"SpeechPreLevelID","4")),"Mentor count must bound changed IDs.");
        var foreign=NativeMapDocument.Create(24,24,"Foreign");Reject(()=>foreign.ApplyMapSpeechText(draft),"Foreign speech draft accepted.");
        doc.Paint([new(20,20)],new(4,2,1));doc.Undo();Reject(()=>doc.ApplyMapSpeechText(draft),"Edit+Undo revived authority.");
        draft=Draft();Expect(MemoryMarshal.TryGetArray(draft.Record!.File.Data,out backing),"Current raw fixture backing absent.");speechOffset=backing.Offset+draft.Record.Offset+Field(doc,draft,"SpeechFile").Offset;backing.Array![speechOffset]^=1;Reject(()=>doc.ApplyMapSpeechText(draft),"Raw mutation revived authority.");backing.Array[speechOffset]^=1;
        var redo=doc.GetHistory().Redo.Count;draft=Draft();var prepared=doc.UpdateMapSpeechText(draft,Inputs(draft,"TextPlotID","123"));
        Reject(()=>doc.Edit("Mixed speech fixture",()=>{doc.ApplyMapSpeechText(prepared);doc.Paint([new(20,20)],new(4,2,1));throw new InvalidOperationException("rollback");}),"Mixed rollback swallowed.");Exact(malformed);Expect(doc.GetHistory().Redo.Count==redo,"Rollback consumed redo.");
        var saved=SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory,"speech-saved-"+native));Expect(ReadOnlyProjection(doc)==ReadOnlyProjection(NativeMapDocument.Load(saved)),"Speech/text changed after save/reopen.");
        if(!native)
        {
            var compiled=doc.CompileLegacyProject("SpeechCompiled",NativeMapDocument.Create(24,24,"Basis"));var nativeDraft=compiled.CreateMapSpeechTextDraft();
            Expect(nativeDraft.Values["SpeechFile"]==new string('Z',32),"Compiler failed to copy full 32-byte bank.");
            Expect(MemoryMarshal.TryGetArray(draft.Record!.File.Data,out backing),"Current compiler fixture backing absent.");speechOffset=backing.Offset+draft.Record.Offset+Field(doc,draft,"SpeechFile").Offset;backing.Array![speechOffset+6]=0;backing.Array[speechOffset+7]=0xE9;compiled=doc.CompileLegacyProject("SpeechPadding",NativeMapDocument.Create(24,24,"Basis"));nativeDraft=compiled.CreateMapSpeechTextDraft();
            Expect(compiled.MainFile.Data.Span.Slice(nativeDraft.Record!.Offset+Field(compiled,nativeDraft,"SpeechFile").Offset,32).SequenceEqual(backing.Array.AsSpan(speechOffset,32)),"Compiler rewrote speech bytes after NUL.");backing.Array[speechOffset+6]=(byte)'Z';backing.Array[speechOffset+7]=(byte)'Z';
        }
        doc.SetHistoryDepth(0);draft=Draft();doc.ApplyMapSpeechText(doc.UpdateMapSpeechText(draft,Inputs(draft,"SpeechFile","SPEECH_ÉTÉ")));Expect(!doc.CanUndo && doc.CreateMapSpeechTextDraft().Values["SpeechFile"]=="SPEECH_ÉTÉ","Zero history lost ANSI edit.");
    }
    private static void Historical(string directory)
    {
        var source=NativeMapDocument.CreateNewMap(new("SpeechVersions","SpeechVersions",24,24));var path=SaveProjectionChecks.SaveCopy(source, Path.Combine(directory,"versions-source"));var file=LegacySectionFile.Read(path);var info=file.Sections.Single(row=>row.Signature==NativeMapDocument.LegacyInfo);
        foreach(var version in Enumerable.Range(1,8))
        {
            var structure=version switch{1=>"MAPPROPERTIESV1",2 or 3=>"MAPPROPERTIESV2",4=>"MAPPROPERTIESV4",5=>"MAPPROPERTIESV5",6=>"MAPPROPERTIESV6",_=>"MAPPROPERTIES"};
            var replacement=new LegacySection(info.Signature,version,info.Payload[..LegacyProjectSchemas.Size(structure)]);var bytes=LegacySectionFile.Create(file.Sections.Select(row=>row==info?replacement:row)).ToArray();var name=Path.Combine(directory,$"speech-v{version}.dkm");File.WriteAllBytes(name,bytes);
            var doc=NativeMapDocument.Load(name);var draft=doc.CreateMapSpeechTextDraft(["SPEECH_LEVEL1"]);Expect(draft.Values.Count==(version is >=2 and <=4?1:version is >=5 and <=7?30:0),"Historical speech/text padding guessed.");
            if(draft.Editable){doc.ApplyMapSpeechText(doc.UpdateMapSpeechText(draft,Inputs(draft,"SpeechFile","SPEECH_LEVEL1")));doc.Undo();}else Reject(()=>doc.ApplyMapSpeechText(draft),"Unsupported metadata edited.");
            if(version<=6){doc.UpgradeLegacyProject();Expect(doc.CreateMapSpeechTextDraft().Values.Count==30,"Upgrade lost metadata fields.");doc.Undo();}
            var saved=SaveProjectionChecks.SaveCopy(doc, Path.Combine(directory,$"speech-v{version}-exact"));Expect(SaveProjectionChecks.BeforeEquals(doc,bytes),"Historical speech exact Undo/save.");
        }
    }
}
