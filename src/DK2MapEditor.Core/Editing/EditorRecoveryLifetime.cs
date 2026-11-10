namespace DK2MapEditor.Core.Editing;

public sealed record EditorRecoveryCandidate(string Path, DateTime SavedUtc);

/// <summary>Exclusive per-window crash marker and recovery file. Completed and
/// dismissed sessions retain their DKM and source companions; no input map is deleted.</summary>
public sealed class EditorRecoveryLifetime : IDisposable
{
    private FileStream? marker;
    private readonly string markerPath;
    public string RootDirectory { get; }
    public string RecoveryPath { get; }
    public EditorRecoveryLifetime(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory); RootDirectory=Path.GetFullPath(rootDirectory);
        var owned=Path.Combine(RootDirectory,"session-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(owned); RecoveryPath=Path.Combine(owned,"Autosave.dkm");
        markerPath=Path.Combine(owned,"open.flag");
        marker=new FileStream(markerPath,FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None);
        marker.WriteByte(1); marker.Flush(flushToDisk:true);
    }
    public static IReadOnlyList<EditorRecoveryCandidate> Find(string rootDirectory, bool includeCompleted=false)
    {
        var root=Path.GetFullPath(rootDirectory);if(!Directory.Exists(root))return [];
        var result=new List<EditorRecoveryCandidate>();
        foreach(var directory in Directory.EnumerateDirectories(root))
        {
            try
            {
                var name=Path.GetFileName(directory);
                if(!name.StartsWith("session-",StringComparison.Ordinal) || !Guid.TryParseExact(name[8..],"N",out _)
                    || (File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0)continue;
                var flag=Path.Combine(directory,"open.flag");var path=Path.Combine(directory,"Autosave.dkm");
                if(!File.Exists(path) || (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)continue;
                if(!File.Exists(flag))
                {
                    if(includeCompleted && (File.Exists(flag+".closed") || File.Exists(flag+".dismissed")))
                        result.Add(new(path,File.GetLastWriteTimeUtc(path)));
                    continue;
                }
                if((File.GetAttributes(flag)&FileAttributes.ReparsePoint)!=0)continue;
                using var handle=new FileStream(flag,FileMode.Open,FileAccess.ReadWrite,FileShare.None);
                result.Add(new(path,File.GetLastWriteTimeUtc(path)));
            }
            catch(IOException){ /* Live editor or unavailable file: preserve it. */ }
            catch(UnauthorizedAccessException){ /* Not an owned readable candidate. */ }
        }
        return result.OrderByDescending(item=>item.SavedUtc).ThenBy(item=>item.Path,StringComparer.Ordinal).ToArray();
    }
    public static void Dismiss(string rootDirectory,EditorRecoveryCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var current=Find(rootDirectory).SingleOrDefault(item=>item.Path.Equals(Path.GetFullPath(candidate.Path),StringComparison.OrdinalIgnoreCase));
        if(current is null)throw new InvalidOperationException("The recovery candidate is no longer available.");
        var flag=Path.Combine(Path.GetDirectoryName(current.Path)!,"open.flag");
        using(var handle=new FileStream(flag,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
        { /* Hold exclusive ownership check; the marker bytes need no edit. */ }
        File.Move(flag,flag+".dismissed");
    }
    public void Dispose()
    {
        if(marker is null)return;
        var handle=marker;marker=null;
        try { handle.WriteByte(0);handle.Flush(flushToDisk:true); }
        finally { handle.Dispose(); }
        File.Move(markerPath,markerPath+".closed");
    }
}
