namespace DK2MapEditor.Core.Automation;

/// <summary>Writes stay in an explicitly selected local workspace; existing junctions are rejected.
/// This prevents accidental writes, not hostile same-user filesystem races.</summary>
public sealed class AutomationPaths
{
    public string Root { get; }
    public AutomationPaths(string root)
    {
        if(!Path.IsPathFullyQualified(root) || root.StartsWith(@"\\",StringComparison.Ordinal))
            throw new ArgumentException("The MCP output root must be an absolute local path.");
        Root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        CheckAncestors(Root);
    }
    public string Check(string path)
    {
        if(!Path.IsPathFullyQualified(path))throw new AutomationException("output_path","Use an absolute output path.");
        var full=Path.GetFullPath(path);
        if(!full.Equals(Root,StringComparison.OrdinalIgnoreCase)
            && !full.StartsWith(Path.TrimEndingDirectorySeparator(Root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
            throw new AutomationException("output_path","Output must stay inside the configured MCP root.");
        CheckAncestors(full);
        return full;
    }
    public string CheckSave(string path,bool overwrite)
    {
        var full=Check(path);
        if(Path.GetExtension(full).ToLowerInvariant() is not (".dkm" or ".map"))
            throw new AutomationException("save_format","Normal Save currently supports .dkm and .map; use Compile for the seven game files.");
        var parent=Path.GetDirectoryName(full)!;
        if(!Directory.Exists(parent))throw new AutomationException("output_path","Create the output parent directory first.");
        // Save can publish source ZIPs and modifiers beside the project. Reject redirected siblings too.
        foreach(var item in Directory.EnumerateFileSystemEntries(parent))CheckAncestors(item);
        if(!overwrite && (File.Exists(full)||Directory.Exists(full)
            ||Directory.EnumerateFileSystemEntries(parent,Path.GetFileName(full)+".*").Any()))
            throw new AutomationException("output_exists","The project or a companion already exists. Choose another path or explicitly allow overwrite.");
        return full;
    }
    private static void CheckAncestors(string path)
    {
        for(var current=path;!string.IsNullOrEmpty(current);current=Path.GetDirectoryName(current))
        {
            try
            {
                if((File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)
                    throw new AutomationException("output_path","MCP outputs cannot pass through a symbolic link or junction.");
            }
            catch(FileNotFoundException){}
            catch(DirectoryNotFoundException){}
        }
    }
}
