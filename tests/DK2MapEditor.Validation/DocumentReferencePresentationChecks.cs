using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class DocumentReferencePresentationChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static void Imported(NativeRecord record,string name,string value)
    {Expect(MemoryMarshal.TryGetArray(record.File.Data,out var bytes),"Fixture buffer missing.");var field=record.Fields.Single(f=>f.Name==name);field.Encode(value).CopyTo(bytes.Array!,bytes.Offset+record.Offset+field.Offset);}
    private static void ReadOnly(NativeMapDocument d,Action read)
    {
        var bytes=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var state=(d.EditRevision,d.EditorChangeCount,d.HasChanges,d.CanUndo,d.CanRedo);var notices=0;void Changed(object? s,EventArgs e)=>notices++;d.Changed+=Changed;
        try{read();Expect(bytes.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&state==(d.EditRevision,d.EditorChangeCount,d.HasChanges,d.CanUndo,d.CanRedo)&&notices==0,"Presentation changed bytes/history/dirty/counters/notifications.");}finally{d.Changed-=Changed;}
    }
    private static void General(NativeMapDocument d,string name,string author)
    {
        var draft=d.CreateMapGeneralDraft();d.ApplyMapGeneralSettings(d.UpdateMapGeneralSettings(draft,draft.Values.Select(pair=>new MapGeneralInput(pair.Key,pair.Key=="Name"?name:pair.Value))));
        var record=d.CreateMapGeneralDraft().Record!;d.SetField(record,record.Fields.Single(f=>f.Name==(d.IsLegacyProject?"Author":"wstrMainAuthor")),author);
    }
    public static void Run()
    {
        var dir=new DirectoryInfo(AppContext.BaseDirectory);while(dir is not null&&!File.Exists(Path.Combine(dir.FullName,"DK2MapEditor.slnx")))dir=dir.Parent;var root=dir?.FullName??throw new Exception("Canonical root missing.");
        using var fixture=JsonDocument.Parse(typeof(DocumentReferencePresentationChecks).Assembly.GetManifestResourceStream("DK2MapEditor.Validation.DocumentReferencePresentationSource.json")!);
        foreach(var input in fixture.RootElement.GetProperty("InputHashes").EnumerateObject())Expect(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,input.Name)))).Equals(input.Value.GetString(),StringComparison.OrdinalIgnoreCase),"Document/reference source changed.");
        var temp=Path.Combine(Path.GetTempPath(),"DocumentReference-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);var referenceFields=0;
        try
        {
            foreach(var native in new[]{true,false})
            {
                var d=NativeMapDocument.CreateNewMap(new("FileKey","Internal café",16,14,NativeFormat:native));General(d,"Internal café","Old author é");var path=Path.Combine(temp,native?"original.kwd":"original.dkm");var initial=EditorDocumentPresentation.Read(d,path);
                ReadOnly(d,()=>{var card=EditorDocumentPresentation.Read(d,path);Expect(card.DisplayName=="Internal café"&&card.Author=="Old author é"&&card.Width==16&&card.Height==14&&card.FileName==Path.GetFileName(path),"Card mixed filename/internal name/author/dimensions.");});
                using(var session=d.CreateMapPropertiesSession())
                {
                    General(session.WorkingDocument,"Changed name","Changed author");Expect(EditorDocumentPresentation.Read(d,path)==initial,"Private draft leaked into live card.");
                    d.ApplyEditorMapProperties(d.PrepareEditorMapProperties(session,new(18,16,4,RemoveOutside:true)),true,true);
                }
                var changed=EditorDocumentPresentation.Read(d,path);Expect(changed.DisplayName=="Changed name"&&changed.Author=="Changed author"&&changed.Width==18&&changed.Height==16,"Applied properties card is stale.");
                d.Undo();Expect(EditorDocumentPresentation.Read(d,path)==initial,"Undo metadata card is stale.");d.Redo();Expect(EditorDocumentPresentation.Read(d,path)==changed,"Redo metadata card is stale.");
                using(var save=new EditorSaveSession(d))
                {var destination=Path.Combine(temp,native?"saved-native-as.dkm":"saved-project-as.dkm");Expect(save.Save(save.PrepareSaveAs(destination),true),"SaveAs fixture failed.");var card=EditorDocumentPresentation.Read(save.LastSave!.Document,save.TargetPath);Expect(card.FileName==Path.GetFileName(destination)&&card.DisplayName=="Changed name"&&card.Author=="Changed author"&&card.Width==18&&card.Height==16,"SaveAs renamed the internal name or kept stale card metadata.");}
                var refs=NativeMapDocument.CreateNewMap(new("References","References",20,20,NativeFormat:native));
                NativeRecord Add(string legacy,string structure)=>native?refs.AddRecord(RecordTemplate.All.Single(t=>t.Structure==structure),new(3,3)):refs.AddLegacyRecord(legacy,new(3,3));
                var area=Add("Area","GAMEAREA");var band=Add("Band","GAMEBAND");var effect=Add("Effect","GAMEEFFECT");
                foreach(var (record,legacy,key) in new[]{(area,"id","bAreaID"),(band,"id","bBandID"),(effect,"id","bEffectID")})Imported(record,native?key:legacy,"7");
                if(!native){var gate=refs.AddLegacyRecord("Gate",new(3,3));Imported(gate,"id","7");}
                if(!native){Imported(area,"name","Area café\r\n\tname");Imported(area,"info","Line1\r\nLine2");Imported(band,"name","Band\r\nliteral");}
                var duplicate=Add("Area","GAMEAREA");Imported(duplicate,native?"bAreaID":"id","7");if(!native)Imported(duplicate,"name","Second target");
                ReadOnly(refs,()=>
                {
                    var identities=refs.Records().Select(EntityReferences.Identity).OfType<EntityIdentity>().ToArray();
                    foreach(var language in new[]{"en","ru"})
                    {
                        var ambiguous=EntityReferences.DisplayIdentity(identities.First(row=>row.Kind==EntityKind.Area),language,2);Expect(ambiguous.Contains(language=="ru"?"неоднозначно":"ambiguous",StringComparison.Ordinal)&&!ambiguous.Contains("café",StringComparison.Ordinal)&&!ambiguous.Contains("Second",StringComparison.Ordinal),"Ambiguous ID presents a guessed first record name.");
                        var unique=EntityReferences.DisplayIdentity(identities.Single(row=>row.Kind==EntityKind.Effect),language);Expect(unique.StartsWith(language=="ru"?"Генератор эффектов 7":"Effect generator 7",StringComparison.Ordinal),"Effect generator reference has a technical/incorrect kind caption.");
                    }
                    if(!native){Expect(EntityReferences.DisplayDetails(area)==" · Area café |  name · Line1 | Line2","RectEx single-line display changed.");Expect(EntityReferences.DisplayDetails(band)==" · Band\r\nliteral","Literal source band name was sanitized.");}
                });
                foreach(var rule in EntityReferences.EventRules)
                {
                    var action=rule.Event.Name.StartsWith("ACTION_",StringComparison.Ordinal);var record=Add(action?"Action":"Trigger",action?"Action":"Trigger");refs.SetEventType(record,rule.Event);record=refs.Records().Single(r=>r.File==record.File&&r.Offset==record.Offset);
                    if(native&&rule.Kind==EntityKind.Gate){Expect(!EntityReferences.From(record).Any(value=>value.Kind==EntityKind.Gate),"Native gate coordinates were invented as an ID link.");continue;}
                    var field=record.Fields.SingleOrDefault(f=>f.Name==(native?"Param"+rule.Parameter[5..]:rule.Parameter))??throw new Exception("Missing fixture operand: "+rule.Event.Name+" "+rule.Parameter+"; "+string.Join(",",record.Fields.Select(f=>f.Name)));
                    ReadOnly(refs,()=>
                    {
                        var values=EntityReferences.Choices(refs.Records(),record,field);
                        Expect(values is not null&&values.Count(v=>v.Value=="7")==1&&!values.Any(v=>v.Value=="99"),"Reference choices lost physical ID or invented a missing ID.");
                        var chosen=values!.Single(v=>v.Value=="7");if(rule.Kind==EntityKind.Area)Expect(chosen.English.Contains("ambiguous: 2",StringComparison.Ordinal)&&chosen.Russian.Contains("неоднозначно",StringComparison.Ordinal)&&chosen.Russian.Contains("записей: 2",StringComparison.Ordinal),"Generic choices hide duplicate destination ambiguity.");
                        var named=ScenarioNamedCatalogues.Choices(rule.Event.Name,int.Parse(rule.Parameter[5..],CultureInfo.InvariantCulture),refs);if(rule.Kind==EntityKind.Area&&named.Any(v=>v.Value=="7"))Expect(named.Where(v=>v.Value=="7").All(v=>v.English.Contains("ambiguous: 2",StringComparison.Ordinal)),"Named catalogue hides duplicate destination ambiguity.");referenceFields++;
                    });
                }
                var hero=Add("Creature","GAMEHERO");Imported(hero,native?"bHeroBandID":"id",native?"255":"14");
                if(native){var bandField=hero.Fields.Single(f=>f.Name=="bHeroBandID");var choices=EntityReferences.Choices(refs.Records(),hero,bandField)!;Expect(choices.Single(v=>v.Value=="255").English=="Not in a band","Band sentinel changed.");}
            }
        }
        finally{Directory.Delete(temp,true);}
        var nativeBytes=NativeMapDocument.Create(16,14,"Metadata").MainFile.Data.ToArray();var author=LegacySchemas.Field("GAMEINFO","wstrMainAuthor");Expect(author.Offset==2232&&author.Size==128,"Native author source layout anchor differs.");author.Encode("Автор é").CopyTo(nativeBytes,author.Offset);Expect(EditorWindowCaption.ReadNativeMapAuthor(nativeBytes)=="Автор é","Native Unicode author decoded as name/ASCII.");
        Expect(EditorWindowCaption.ReadNativeMapAuthor(nativeBytes.AsSpan(0,author.Offset+author.Size-1)) is null,"Truncated author accepted.");BinaryPrimitives.WriteUInt32LittleEndian(nativeBytes.AsSpan(52),0);Expect(EditorWindowCaption.ReadNativeMapAuthor(nativeBytes) is null,"Author outside declared native block accepted.");
        foreach(var version in Enumerable.Range(1,7))
        {
            var structure=version switch{1=>"MAPPROPERTIESV1",2 or 3=>"MAPPROPERTIESV2",4=>"MAPPROPERTIESV4",5=>"MAPPROPERTIESV5",6=>"MAPPROPERTIESV6",_=>"MAPPROPERTIES"};var bytes=new byte[12+LegacyProjectSchemas.Size(structure)];BinaryPrimitives.WriteUInt32LittleEndian(bytes,NativeMapDocument.LegacyInfo);BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4),(uint)version);BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8),(uint)bytes.Length-12);
            if(version<=4)Expect(!LegacyProjectSchemas.Fields(structure).Any(f=>f.Name=="Author")&&EditorWindowCaption.ReadProjectMapAuthor(bytes) is null,"Author invented in source layout1..4 without this field.");
            else{var field=LegacyProjectSchemas.Fields(structure).Single(f=>f.Name=="Author");field.Encode("Auteur café").CopyTo(bytes,12+field.Offset);Expect(EditorWindowCaption.ReadProjectMapAuthor(bytes)=="Auteur café","Historical author codec/offset differs.");bytes.AsSpan(12+field.Offset,64).Fill(0xE9);Expect(EditorWindowCaption.ReadProjectMapAuthor(bytes)==new string('é',64),"Full-width imported author lost bytes.");}
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4),99);Expect(EditorWindowCaption.ReadProjectMapAuthor(bytes) is null,"Future project author guessed.");
        }
        Console.WriteLine($"DOCUMENT REFERENCE DETAIL live native/DKM name/author/dimensions/path;private draft/apply/Undo/Redo/actualSaveAs;Unicode+7historical metadata layouts (author5..7 only)/full-width/future guards;{referenceFields}source typed reference fields/namespaces/ambiguous labels/sentinels/RectEx versus literal band names;read-only bytes/history/dirty/counters;no WPF execution.");
    }
}
