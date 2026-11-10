using System.Reflection;
using DK2MapEditor.Core.Editing;
using DK2MapEditor.Core.Formats;

if (args.Length != 1 || Directory.Exists(args[0])) throw new ArgumentException("A new private fixture directory is required.");
var directory=Path.GetFullPath(args[0]);Directory.CreateDirectory(directory);var passed=0;
void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
void Refuses(Action action)
{
    try{action();}catch(Exception exception) when(exception is IOException or UnauthorizedAccessException){return;}
    throw new InvalidOperationException("A blocked archive destination was accepted.");
}
foreach(var native in new[]{false,true})
{
    var format=native?"native":"project";var area=Path.Combine(directory,format);Directory.CreateDirectory(area);
    var doc=NativeMapDocument.CreateNewMap(new("ArchiveWrite","ArchiveWrite",16,16,NativeFormat:native));
    if(native)doc.AddRecord(RecordTemplate.All.Single(template=>template.Structure=="GAMEKEEPER"),new(3,3));
    else doc.AddLegacyRecord("Creature",new(3,3));
    doc.Paint([new(2,2)],new(4,2,1));var clipboard=doc.CopySelection(new(2,2,4,4));var expected=SelectionFile.Serialize(clipboard);
    var before=doc.Files.ToDictionary(file=>file.ExportPath,file=>file.Data.ToArray());var revision=doc.EditRevision;var counter=doc.EditorChangeCount;var dirty=doc.HasChanges;var history=doc.GetHistory();var statistics=doc.ReadActivityStatistics()?.Encode();
    Expect(clipboard.ObjectCount==1,"Archive fixture has no object to preserve.");
    void SourceExact()
    {
        Expect(doc.Files.Count==before.Count&&doc.Files.All(file=>file.Data.Span.SequenceEqual(before[file.ExportPath])),"Archive write changed source buffers.");
        var now=doc.GetHistory();Expect(doc.EditRevision==revision&&doc.EditorChangeCount==counter&&doc.HasChanges==dirty&&now.Undo.Count==history.Undo.Count&&now.Redo.Count==history.Redo.Count&&now.ChangeCount==history.ChangeCount,"Archive write changed source history/counters/dirty state.");
        var current=doc.ReadActivityStatistics()?.Encode();Expect(statistics is null?current is null:current is not null&&statistics.AsSpan().SequenceEqual(current),"Selection archive write changed full activity statistics.");
        Expect(!Directory.EnumerateFiles(area,"*.tmp").Any(),"Archive write leaked a staging file.");
    }
    void Exact(string path)
    {
        Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(expected),"Written archive differs from all expected bytes.");
        var reopened=SelectionFile.Read(path);Expect(reopened.ObjectCount==1&&reopened.IsLegacyProject==clipboard.IsLegacyProject&&SelectionFile.Serialize(reopened).AsSpan().SequenceEqual(expected),"Archive reopen lost data or exact metadata.");SourceExact();
    }
    void Pass(string name){SourceExact();Console.WriteLine($"PASS {format} {name}");passed++;}
    var path=Path.Combine(area,"no-consent.dkz");SelectionFile.SaveNew(path,clipboard);Exact(path);
    Refuses(()=>SelectionFile.SaveNew(path,clipboard));Exact(path);Pass("new archive and unconfirmed replacement refusal preserve every byte");
    path=Path.Combine(area,"confirmed-existing.dkz");File.WriteAllBytes(path,[9,8,7,6]);SelectionFile.SaveConfirmed(path,clipboard);Exact(path);Pass("confirmed existing destination contains exact complete archive");
    path=Path.Combine(area,"confirmed-new.dkz");SelectionFile.SaveConfirmed(path,clipboard);Exact(path);Pass("confirmed new destination contains exact complete archive");
    var original=new byte[]{10,20,30,40,50};path=Path.Combine(area,"locked.dkz");File.WriteAllBytes(path,original);
    using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))Refuses(()=>SelectionFile.SaveConfirmed(path,clipboard));
    Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(original),"Failed locked replacement damaged existing bytes.");Pass("locked replacement preserves existing bytes and removes staging file");
    path=Path.Combine(area,"readonly.dkz");File.WriteAllBytes(path,original);var attributes=File.GetAttributes(path);File.SetAttributes(path,attributes|FileAttributes.ReadOnly);
    try
    {
        Refuses(()=>SelectionFile.SaveConfirmed(path,clipboard));Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(original)&&(File.GetAttributes(path)&FileAttributes.ReadOnly)!=0,"Failed read-only replacement changed bytes or attributes.");
    }
    finally{File.SetAttributes(path,attributes);}
    Pass("read-only replacement preserves existing bytes and attributes");
    var missing=Path.Combine(area,"absent");Refuses(()=>SelectionFile.SaveConfirmed(Path.Combine(missing,"archive.dkz"),clipboard));Expect(!Directory.Exists(missing),"Missing-parent save created a directory.");
    var targetDirectory=Path.Combine(area,"directory.dkz");Directory.CreateDirectory(targetDirectory);Refuses(()=>SelectionFile.SaveConfirmed(targetDirectory,clipboard));Expect(Directory.Exists(targetDirectory)&&!Directory.EnumerateFileSystemEntries(targetDirectory).Any(),"Directory target was altered.");Pass("invalid destinations preserve directories and leave no staging file");
    var payload=typeof(SelectionClipboard).GetNestedType("Payload",BindingFlags.NonPublic)!;var empty=Array.CreateInstance(payload,0);
    var constructor=typeof(SelectionClipboard).GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).Single();
    var invalid=(SelectionClipboard)constructor.Invoke([new TileClipboard(0,1,Array.Empty<MapTile>()),new TileRect(0,0,0,1),!native,empty,empty,null]);
    path=Path.Combine(area,"invalid-archive.dkz");File.WriteAllBytes(path,original);
    try{SelectionFile.SaveConfirmed(path,invalid);throw new InvalidOperationException("Invalid archive was accepted.");}catch(InvalidDataException){}
    Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(original),"Failed archive validation replaced existing bytes.");Pass("archive validation finishes before existing destination can change");
}
Expect(passed==14,"Incomplete archive write checks.");Console.WriteLine($"SELECTION ARCHIVE WRITE CHECKS PASSED: {passed}");return 0;
