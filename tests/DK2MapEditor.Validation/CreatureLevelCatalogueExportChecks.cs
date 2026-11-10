using System.Buffers.Binary;
using System.Runtime.InteropServices;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

internal static class CreatureLevelCatalogueExportChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Reject(Action action)
    {try{action();}catch(Exception e)when(e is ArgumentException or InvalidOperationException or IOException){return;}throw new Exception("Invalid catalogue publication was accepted.");}
    public static void Run(string root)
    {
        var path=Path.GetFullPath(Path.Combine(root,"..","Dungeon Keeper 2","Data","editor","GlobalVariables.kwd"));var original=File.ReadAllBytes(path);
        var catalogue=CreatureLevelModifierCatalogue.ReadFile(path);Check(catalogue.HasCompleteVisibleTable,"Official catalogue has no complete visible table.");
        var copy=catalogue.CopyBytes();copy[0]=0;Check(catalogue.CopyBytes().AsSpan().SequenceEqual(original),"Catalogue snapshot aliases returned bytes.");
        var d=NativeMapDocument.CreateNewMap(new("CatalogueCopy","Catalogue copy",20,20));var before=d.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var revision=d.EditRevision;var count=d.EditorChangeCount;
        using var session=d.CreateCreatureLevelModifierSession(catalogue.Records);
        Check(session.CreateSharedCatalogueCopy(catalogue).AsSpan().SequenceEqual(original),"An unedited catalogue was normalized.");
        var expected=original.ToArray();var rows=catalogue.Records;
        foreach(var stat in CreatureLevelModifierControls.Statistics)
        {
            var id=uint.Parse(stat.Value);session.Draft.SelectStatistic(id,session.Draft.Current);
            var levels=Enumerable.Range(0,9).Select(i=>i%2==0?-10000+(int)id+i:0).ToArray();session.Draft.Store(new(levels,10000-(int)id));
            foreach(var row in rows.Where(r=>r.Statistic==id&&(r.IsDocumentedLevelKey||r.IsObservedPossessionKey)))
            {
                var value=row.RecordType==224?10000-(int)id:levels[(int)row.Level-2];var bits=unchecked((uint)(value==0?100:value));
                if(unchecked((short)bits)!=unchecked((short)row.PercentBits))BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(row.Offset+8),bits);
            }
        }
        var output=session.CreateSharedCatalogueCopy(catalogue);
        Check(output.AsSpan().SequenceEqual(expected)&&output.AsSpan(9992).SequenceEqual(original.AsSpan(9992)),"Export escaped visible DWORDs or lost the following native section.");
        Check(before.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&d.EditRevision==revision&&d.EditorChangeCount==count,"Catalogue preview applied map changes.");
        var changed=original.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(changed.AsSpan(rows[0].Offset+8),321);
        Reject(()=>session.CreateSharedCatalogueCopy(CreatureLevelModifierCatalogue.Capture("different defaults",changed)));
        var directory=Path.Combine(Path.GetTempPath(),"CreatureCatalogueExport-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var target=Path.Combine(directory,"copy.kwd");File.WriteAllBytes(target,[1,2,3]);var prior=File.ReadAllBytes(target);
            Reject(()=>CreatureCatalogueFileOutput.SaveConfirmed(target,output,()=>false));Check(File.ReadAllBytes(target).AsSpan().SequenceEqual(prior),"A stale export overwrote its destination.");
            Reject(()=>CreatureCatalogueFileOutput.SaveConfirmed(target,output,()=>true,p=>new FailingStream(p)));
            Check(File.ReadAllBytes(target).AsSpan().SequenceEqual(prior),"A partial output overwrote its destination.");
            var checks=0;Reject(()=>CreatureCatalogueFileOutput.SaveConfirmed(target,output,()=>++checks==1));
            Check(checks==2&&File.ReadAllBytes(target).AsSpan().SequenceEqual(prior),"Late invalidation published a catalogue.");
            var supplied=output.ToArray();CreatureCatalogueFileOutput.SaveConfirmed(target,supplied,()=>true,p=>{supplied[0]=0;return new FileStream(p,FileMode.CreateNew,FileAccess.Write,FileShare.None);});
            Check(File.ReadAllBytes(target).AsSpan().SequenceEqual(output),"The publisher retained mutable caller data.");
            Check(!Directory.EnumerateFiles(directory,".dk2-catalogue-*").Any(),"Staging catalogue files leaked.");
            var loaded=CreatureLevelModifierCatalogue.ReadFile(target);Check(loaded.HasCompleteVisibleTable&&loaded.CopyBytes().AsSpan().SequenceEqual(output),"The exported catalogue cannot be reopened.");
            var game=NativeMapDocument.Load(Path.GetFullPath(Path.Combine(root,"..","Dungeon Keeper 2","Data","editor","maps","DK2Preview_6a88240eaf814f78.kwd")));
            Check(game.CaptureCreatureLevelModifierCatalogue()!.SHA256==catalogue.SHA256,"Loaded-map catalogue capture changed its data.");
            using var save=new EditorSaveSession(game.CreateCompilationSnapshot());var projectPath=Path.Combine(directory,"captured.dkm");Check(save.Save(save.PrepareSaveAs(projectPath),true),"Native source capture Save failed.");
            Check(save.Document.CaptureCreatureLevelModifierCatalogue()!.SHA256==catalogue.SHA256,"DKM archive catalogue capture changed its data.");
            var conversion=NativeMapDocument.CreateNewMap(new("PercentageIssue","PercentageIssue",20,20,NativeFormat:true));
            foreach(var (type,stat,level) in new (uint,uint,uint)[]{(65,0,2),(224,31,10),(65,8,2),(65,0,1),(224,0,9),(999,0,2)})
            {
                var record=conversion.AddRecord(new("GAMEGLOBALVARIABLE",0xe6,type,16),new(0,0));
                Check(MemoryMarshal.TryGetArray(record.File.Data,out var memory),"No conversion fixture buffer.");
                var span=memory.Array!.AsSpan(memory.Offset+record.Offset,16);
                BinaryPrimitives.WriteUInt32LittleEndian(span[4..],stat);BinaryPrimitives.WriteUInt32LittleEndian(span[8..],100);BinaryPrimitives.WriteUInt32LittleEndian(span[12..],level);
            }
            var issues=conversion.PrepareProjectConversion().Issues;
            Check(issues.Count(i=>i.Code=="CreatureLevelModifiers")==2&&issues.Count(i=>i.Code=="UnknownVariable")==4,
                "Conversion reports hid an unsupported statistic/key as editable percentages.");
        }
        finally{Directory.Delete(directory,true);}
        d.SetExistingCreatureLevelModifiers([]);d.Edit("Invalidate export",()=>d.Paint([new(5,5)],new(4,1,1)));
        Reject(()=>session.CreateSharedCatalogueCopy(catalogue));
        Check(File.ReadAllBytes(path).AsSpan().SequenceEqual(original),"Catalogue export changed installed source data.");
        Console.WriteLine("CREATURE CATALOGUE EXPORT DETAIL private all230 values, exact hidden/header/following-section retention, source snapshot isolation/mismatch/stale guards, native/archive capture, atomic partial-I/O and late-context failure preserve target, independent output and reopen; no map Apply, installed write or UI");
    }
    private sealed class FailingStream(string path):FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)
    {public override void Write(ReadOnlySpan<byte> bytes){base.Write(bytes[..Math.Min(32,bytes.Length)]);throw new IOException("Injected partial catalogue output.");}}
}
