using System.IO;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

/// <summary>Publish an explicitly selected catalogue-copy target after a late
/// owner/input guard. Existing destination survives partial I/O or stale input.</summary>
public static class CreatureCatalogueFileOutput
{
    public static void SaveConfirmed(string path,byte[] catalogue,Func<bool> current,Func<string,Stream>? createStaging=null)
    {
        ArgumentNullException.ThrowIfNull(catalogue);ArgumentNullException.ThrowIfNull(current);
        var snapshot=CreatureLevelModifierCatalogue.Capture("prepared catalogue copy",catalogue).CopyBytes();
        if(!current())throw new InvalidOperationException("The catalogue export context changed.");
        var target=Path.GetFullPath(path);var parent=Path.GetDirectoryName(target)!;
        if(!Directory.Exists(parent))throw new DirectoryNotFoundException(parent);
        if(Directory.Exists(target))throw new IOException("The catalogue destination is a directory.");
        var staging=Path.Combine(parent,".dk2-catalogue-"+Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            using(var stream=createStaging is null?new FileStream(staging,FileMode.CreateNew,FileAccess.Write,FileShare.None):createStaging(staging))
            {stream.Write(snapshot);if(stream is FileStream file)file.Flush(true);else stream.Flush();}
            if(!current())throw new InvalidOperationException("The catalogue export context changed.");
            File.Move(staging,target,overwrite:true);
        }
        finally{if(File.Exists(staging))File.Delete(staging);}
    }
}
