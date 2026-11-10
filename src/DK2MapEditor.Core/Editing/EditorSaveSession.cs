using DK2MapEditor.Core.Formats;

namespace DK2MapEditor.Core.Editing;

public enum EditorSaveFormat { Dkm, Map }

/// <summary>One immutable consent boundary for Save/Save As. A native DKM projection is private until publication succeeds.</summary>
public sealed class EditorSavePlan
{
    internal EditorSavePlan(EditorSaveSession owner, string path, EditorSaveFormat format, NativeMapDocument document,
        EditorTextMapSaveSession? text, EditorTextMapSavePlan? map, PreparedProjectConversion? conversion, EditorSourceArchive? archive)
    {
        Owner=owner; Path=path; Format=format; Source=document; Revision=document.EditRevision;
        Fingerprint=document.SaveWorkingFingerprint(); Statistics=document.ReadActivityStatistics()?.Encode();
        Text=text; Map=map; Conversion=conversion; Archive=archive;
    }
    internal EditorSaveSession Owner { get; }
    internal NativeMapDocument Source { get; }
    internal long Revision { get; }
    internal byte[] Fingerprint { get; }
    internal byte[]? Statistics { get; }
    internal EditorTextMapSaveSession? Text { get; }
    internal EditorTextMapSavePlan? Map { get; }
    internal PreparedProjectConversion? Conversion { get; }
    internal EditorSourceArchive? Archive { get; }
    public string Path { get; }
    public EditorSaveFormat Format { get; }
    public IReadOnlyList<TextMapLoss> TextLosses => Map?.Losses ?? [];
    public IReadOnlyList<ProjectConversionIssue> ConversionIssues => Conversion?.Issues ?? [];
    public bool HasBlockingIssues => Conversion?.HasBlockingIssues == true;
    public bool NeedsLossConsent => Map?.NeedsLossConsent == true || Conversion?.HasReportedLosses == true;
    public bool RetainsNativeSource => Archive is not null;
}

public sealed record EditorSaveResult(string Path, bool Temporary, NativeMapDocument Document, IReadOnlyList<string> SourceArchives);

/// <summary>Unified normal DKM/MAP target and temporary DKM writes. Window commands bind Document after successful conversion.</summary>
public sealed class EditorSaveSession : IDisposable
{
    private bool closed, writing;
    private readonly List<EditorSourceArchive> archives=[];
    public NativeMapDocument Document { get; private set; }
    public string? TargetPath { get; private set; }
    public EditorSaveFormat? TargetFormat => TargetPath is null ? null : Format(TargetPath);
    public EditorAutoSaveTimer AutoSave { get; } = new();
    public EditorSaveResult? LastSave { get; private set; }
    public bool CanSave => !closed && !writing && TargetPath is not null && Document.EditorChangeCount != 0;
    public event EventHandler<EditorSaveResult>? Saved;

    public EditorSaveSession(NativeMapDocument document, string? loadedPath=null, bool recovered=false)
    {
        ArgumentNullException.ThrowIfNull(document); Document=document;
        if (loadedPath is not null)
        {
            var path=CheckedPath(loadedPath); var format=Format(path);
            if (format==EditorSaveFormat.Dkm && (!document.IsLegacyProject || document.MainFile.Signature!=LegacySectionFile.MapFileSignature))
                throw new ArgumentException("Native or AREA imports have no ordinary DKM target.",nameof(loadedPath));
            if (!recovered) TargetPath=path;
            if (format==EditorSaveFormat.Dkm) { archives.AddRange(EditorSourceArchive.Discover(path)); Document.BindNativeSourceArchives(archives); }
        }
    }
    public EditorSavePlan PrepareSave()
    {
        Current();
        if (TargetPath is null) throw new InvalidOperationException("Choose a DKM or MAP target with Save As first.");
        return PrepareSaveAs(TargetPath);
    }
    public EditorSavePlan PrepareSaveAs(string path) => Prepare(path, temporary: false);
    private EditorSavePlan Prepare(string path, bool temporary)
    {
        Current(); if (writing || (temporary ? Document.IsTemporarySaveLocked : Document.IsSaveLocked)) throw new InvalidOperationException("Finish the current edit or save first.");
        var target=CheckedPath(path); var format=Format(target);
        if (format==EditorSaveFormat.Map)
        {
            if (temporary) throw new ArgumentException("Temporary recovery uses DKM.",nameof(path));
            var text=new EditorTextMapSaveSession(Document);
            return new(this,target,format,Document,text,text.PrepareSaveAs(target),null,null);
        }
        PreparedProjectConversion? conversion=null; EditorSourceArchive? archive=null;
        if (!Document.IsLegacyProject || Document.MainFile.Signature==LegacySectionFile.AreaFileSignature)
        { conversion=temporary ? Document.PrepareTemporaryProjectConversion(target) : Document.PrepareProjectConversion(target);
          archive=temporary ? EditorSourceArchive.CaptureTemporary(conversion) : EditorSourceArchive.Capture(conversion); }
        return new(this,target,format,Document,null,null,conversion,archive);
    }
    public bool IsCurrent(EditorSavePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        try { Validate(plan); return true; }
        catch (InvalidOperationException) { return false; }
    }
    public bool Save(EditorSavePlan plan, bool acceptLosses, Func<string,Stream>? createOutput=null, Func<string,Stream>? createArchiveOutput=null)
    {
        ArgumentNullException.ThrowIfNull(plan); Current(); Validate(plan);
        if (plan.HasBlockingIssues) throw new InvalidDataException("Resolve the blocking conversion issues before normal Save.");
        if (plan.NeedsLossConsent && !acceptLosses) return false;
        Write(plan,false,createOutput,createArchiveOutput); return true;
    }
    public void SaveTemporary(string path, Func<string,Stream>? createOutput=null, Func<string,Stream>? createArchiveOutput=null)
    {
        var plan=Prepare(path, temporary: true);
        if (plan.Format!=EditorSaveFormat.Dkm) throw new ArgumentException("Temporary recovery uses DKM.",nameof(path));
        Write(plan,true,createOutput,createArchiveOutput);
    }
    public bool TickAutoSave(string recoveryPath, bool locked=false, Func<string,Stream>? createOutput=null, Func<string,Stream>? createArchiveOutput=null)
    {
        Current(); return AutoSave.Tick(locked || writing || Document.IsTemporarySaveLocked,Document.EditorChangeCount,
            ()=>SaveTemporary(recoveryPath,createOutput,createArchiveOutput));
    }
    private void Write(EditorSavePlan plan, bool temporary, Func<string,Stream>? createOutput, Func<string,Stream>? createArchiveOutput)
    {
        Validate(plan,temporary); writing=true;
        try
        {
            if (plan.Format==EditorSaveFormat.Map)
            {
                // The MAP writer handles its own lock, late guard and baseline.
                if (!plan.Text!.Save(plan.Map!,acceptLosses:true,createOutput: path=>new CheckedOutput(Create(path,createOutput),()=>{Current();})))
                    throw new InvalidOperationException("MAP save was refused.");
                TargetPath=plan.Path; AutoSave.Reset();
                Complete(new(plan.Path,false,Document,[])); return;
            }
            var parent=System.IO.Path.GetDirectoryName(plan.Path)!;
            if (!Directory.Exists(parent)) throw new DirectoryNotFoundException(parent);
            if (Directory.Exists(plan.Path)) throw new IOException("The DKM target is a directory.");
            var source=Document; var project=plan.Conversion?.Project ?? source;
            var sourceFingerprint=plan.Fingerprint;
            if (temporary) source.BeginTemporaryProjectSave(); else source.BeginProjectSave();
            var staging=System.IO.Path.Combine(parent,".dk2-unified-save-"+Guid.NewGuid().ToString("N")+".tmp");
            try
            {
                Current(); ValidateBytes(source,sourceFingerprint,plan.Statistics);
                var retained=archives.Concat(plan.Archive is null ? [] : new[]{plan.Archive}).DistinctBy(item=>item.ContentHash).ToArray();
                var published=retained.Select(archive=>archive.Publish(plan.Path,createArchiveOutput)).ToArray();
                Current(); ValidateBytes(source,sourceFingerprint,plan.Statistics);
                var projectBytes=project.ProjectSaveBytes(); byte[] output; byte[]? expectedAfterStatistics;
                using (var stream=Create(staging,createOutput))
                {
                    Current(); ValidateBytes(source,sourceFingerprint,plan.Statistics);
                    // Original writer effects begin after project file creation.
                    using var statistics=source.BeginProjectStatisticsWrite(temporary);
                    var expectedStatistics=source.ReadActivityStatistics()?.Encode();
                    expectedAfterStatistics=statistics is null ? null : (temporary ? statistics.Output with { SavedTimes=ActivityStatisticsPayload.Decode(ActivityStatisticsPayload.Version,plan.Statistics!)!.SavedTimes } : statistics.Output).Encode();
                    output=project.ProjectSaveActivity(projectBytes,statistics?.Output);
                    stream.Write(output); stream.Flush();
                    if (stream is FileStream file) file.Flush(flushToDisk:true);
                    Current(); ValidateBytes(source,sourceFingerprint,expectedStatistics);
                }
                Current(); ValidateBytes(source,sourceFingerprint,expectedAfterStatistics);
                // Validate replacement before publishing, so projection/adoption
                // failure cannot leave a new target with an old active document.
                var replacement=plan.Conversion is not null && !temporary
                    ? NativeMapDocument.FromSavedProjectBytes(plan.Path,output) : null;
                replacement?.BindNativeSourceArchives(retained);
                replacement?.ContinueActivityStatisticsFrom(source);
                File.Move(staging,plan.Path,overwrite:true);
                if (!temporary)
                {
                    if (replacement is not null) Document=replacement;
                    TargetPath=plan.Path; AutoSave.Reset();
                    archives.Clear(); archives.AddRange(retained);
                    Document.AcceptProjectSave();
                }
                LastSave=new(plan.Path,temporary,Document,Array.AsReadOnly(published));
            }
            finally
            {
                source.EndProjectSave();
                if (File.Exists(staging)) File.Delete(staging);
            }
            Complete(LastSave!);
        }
        finally { writing=false; }
    }
    private static Stream Create(string path,Func<string,Stream>? createOutput)
        =>createOutput is null ? new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None) : createOutput(path);
    private void Complete(EditorSaveResult result) { LastSave=result; Saved?.Invoke(this,result); }
    private void Validate(EditorSavePlan plan, bool temporary=false)
    {
        Current();
        if (writing || !ReferenceEquals(plan.Owner,this) || !ReferenceEquals(plan.Source,Document)
            || (temporary ? Document.IsTemporarySaveLocked : Document.IsSaveLocked) || Document.EditRevision!=plan.Revision)
            throw new InvalidOperationException("Prepare Save again for the current document.");
        ValidateBytes(Document,plan.Fingerprint,plan.Statistics);
    }
    private static void ValidateBytes(NativeMapDocument document,byte[] fingerprint,byte[]? statistics)
    {
        var current=document.ReadActivityStatistics()?.Encode();
        if (!document.SaveWorkingFingerprint().AsSpan().SequenceEqual(fingerprint)
            || !((current is null && statistics is null) || current is not null && statistics is not null && current.AsSpan().SequenceEqual(statistics)))
            throw new InvalidOperationException("Document changed during Save preparation or writing.");
    }
    private static EditorSaveFormat Format(string path)=>System.IO.Path.GetExtension(path).Equals(".map",StringComparison.OrdinalIgnoreCase)?EditorSaveFormat.Map:EditorSaveFormat.Dkm;
    private static string CheckedPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path); var full=System.IO.Path.GetFullPath(path); var extension=System.IO.Path.GetExtension(full);
        if (!extension.Equals(".dkm",StringComparison.OrdinalIgnoreCase) && !extension.Equals(".map",StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Choose a DKM or MAP path.",nameof(path));
        return full;
    }
    private void Current()=>ObjectDisposedException.ThrowIf(closed,this);
    public void Dispose()=>closed=true;
    private sealed class CheckedOutput(Stream inner,Action check) : Stream
    {
        public override bool CanRead=>false;public override bool CanSeek=>false;public override bool CanWrite=>inner.CanWrite;
        public override long Length=>inner.Length;public override long Position {get=>inner.Position;set=>throw new NotSupportedException();}
        public override void Write(byte[] buffer,int offset,int count)=>inner.Write(buffer,offset,count);
        public override void Write(ReadOnlySpan<byte> buffer)=>inner.Write(buffer);
        public override void Flush(){inner.Flush();if(inner is FileStream file)file.Flush(flushToDisk:true);check();}
        protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
        public override int Read(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
        public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
        public override void SetLength(long value)=>throw new NotSupportedException();
    }
}

public sealed partial class NativeMapDocument
{
    internal static NativeMapDocument FromSavedProjectBytes(string path, byte[] output)
        => FromLegacyProject(path,LegacySectionFile.Parse(output));
}
