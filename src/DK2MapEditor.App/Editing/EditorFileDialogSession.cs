using System.IO;

namespace DK2MapEditor.App.Editing;

public enum EditorFileDialogKind { Map,Area,Thumbnail }
public sealed record EditorFileDialogLocation(string? Directory,string FileName);
/// <summary>Source Config.MapPath/AreaPath/ThumbPath runtime scopes, independent of map bytes and JSON.</summary>
public sealed class EditorFileDialogSession
{
    public string? MapTarget {get;private set;}
    public string? AreaPath {get;private set;}
    public string? ThumbnailPath {get;private set;}
    // Map target follows completed load/save, never merely a chooser result.
    public void SetMapTarget(string? path)=>MapTarget=string.IsNullOrWhiteSpace(path)?null:Path.GetFullPath(path);
    // Source chooser copies AREA/BMP names before the subsequent reader/writer.
    public void AcceptAreaPath(string path)=>AreaPath=Path.GetFullPath(path);
    public void AcceptThumbnailPath(string path)=>ThumbnailPath=Path.GetFullPath(path);
    public EditorFileDialogLocation Location(EditorFileDialogKind kind,string defaultName="",string? fallbackDirectory=null)
    {
        var path=kind switch{EditorFileDialogKind.Map=>MapTarget,EditorFileDialogKind.Area=>AreaPath,
            EditorFileDialogKind.Thumbnail=>ThumbnailPath,_=>throw new ArgumentOutOfRangeException(nameof(kind))};
        var directory=path is null?fallbackDirectory:Path.GetDirectoryName(path);
        return new(directory is not null&&System.IO.Directory.Exists(directory)?directory:null,path is null?defaultName:Path.GetFileName(path));
    }
}
