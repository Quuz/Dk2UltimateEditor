using System.IO;
namespace DK2MapEditor.App.Editing;
public static class EditorHelpRules
{
    public static string? FindGuide(string baseDirectory,string language)
    {
        var file=language=="ru"?"USER_GUIDE_RU.md":"USER_GUIDE_EN.md";var location=Path.GetFullPath(baseDirectory);
        foreach(var directory in new[]{Path.Combine(location,"Documentation"),Path.GetFullPath(Path.Combine(location,"..","Documentation"))})
            if(File.Exists(Path.Combine(directory,file)))return Path.Combine(directory,file);
        var parent=new DirectoryInfo(location);
        for(var depth=0;parent is not null&&depth<8;depth++,parent=parent.Parent)
            if(File.Exists(Path.Combine(parent.FullName,"DK2MapEditor.slnx")))
            {var guide=Path.Combine(parent.FullName,"docs",file);return File.Exists(guide)?guide:null;}
        return null;
    }
    public static string? LocalLink(string root,string current,string target)
    {
        var path=target.Split('#',2)[0].Trim('<','>');if(path.Length==0)return current;
        if(Uri.TryCreate(path,UriKind.Absolute,out _)||!path.EndsWith(".md",StringComparison.OrdinalIgnoreCase))return null;
        var candidate=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(current)!,path));
        var boundary=Path.TrimEndingDirectorySeparator(Path.GetFullPath(root))+Path.DirectorySeparatorChar;
        return candidate.StartsWith(boundary,StringComparison.OrdinalIgnoreCase)&&File.Exists(candidate)?candidate:null;
    }
}
