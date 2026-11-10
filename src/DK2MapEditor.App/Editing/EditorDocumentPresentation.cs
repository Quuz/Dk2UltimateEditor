using System.IO;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed record EditorDocumentMetadata(string FileName,string FilePath,string DisplayName,string? Author,int Width,int Height);
public static class EditorDocumentPresentation
{
    public static EditorDocumentMetadata Read(NativeMapDocument document,string? currentPath=null)
    {
        ArgumentNullException.ThrowIfNull(document);var path=currentPath??document.MainFile.SourcePath;
        return new(Path.GetFileName(path),path,EditorWindowCaption.ReadMapName(document)??Path.GetFileNameWithoutExtension(path),EditorWindowCaption.ReadAuthor(document),document.Width,document.Height);
    }
}
