using System.Security.Cryptography;
using DK2MapEditor.Core.Editing;

internal static class ProjectCopySourceArchiveChecks
{
    private static void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }

    public static void Run(string projectPath)
    {
        projectPath = Path.GetFullPath(projectPath);
        var input = File.ReadAllBytes(projectPath);
        var archives = Directory.GetFiles(Path.GetDirectoryName(projectPath)!, Path.GetFileName(projectPath) + ".native-source-*.zip")
            .ToDictionary(path => Path.GetFileName(path)[Path.GetFileName(projectPath).Length..], File.ReadAllBytes);
        Check(archives.Count == 1, "This regression requires a native-derived DKM with one source archive.");
        var parent = Path.Combine(Path.GetTempPath(), "DK2ProjectCopy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);
        try
        {
            var document = NativeMapDocument.Load(projectPath);
            Check(document.IsLegacyProject, "The reported input must be DKM.");
            var before = document.GetTile(2, 2);
            var expected = before with { Owner = (byte)(before.Owner == 2 ? 3 : 2) };
            document.Paint([new(2, 2)], expected);
            var exported = document.SaveCopy(Path.Combine(parent, "copy"));
            foreach (var (suffix, bytes) in archives)
            {
                Check(File.Exists(exported + suffix), "Export Copy omitted the native-source ZIP beside its DKM.");
                Check(File.ReadAllBytes(exported + suffix).AsSpan().SequenceEqual(bytes), "Export Copy changed the retained native source bytes.");
            }
            var reopened = NativeMapDocument.Load(exported);
            Check(reopened.GetTile(2, 2) == expected, "Export/reopen lost the current edited tile.");
            using var session = new EditorSaveSession(reopened, exported);
            var normalPath = Path.Combine(parent, "resaved.dkm");
            Check(session.Save(session.PrepareSaveAs(normalPath), acceptLosses: true), "Normal Save after exported DKM reopen failed.");
            Check(session.LastSave!.SourceArchives.Count == archives.Count, "Reopening the exported DKM did not bind its source archive.");
            foreach (var path in session.LastSave.SourceArchives)
                Check(archives.Values.Any(bytes => File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)), "Normal Save lost the original archived payload.");
            Check(File.ReadAllBytes(projectPath).AsSpan().SequenceEqual(input), "Regression changed the input DKM.");
            foreach (var (suffix, bytes) in archives)
                Check(File.ReadAllBytes(projectPath + suffix).AsSpan().SequenceEqual(bytes), "Regression changed an input ZIP.");
            Console.WriteLine($"PROJECT COPY DETAIL input={projectPath}; SHA256={Convert.ToHexString(SHA256.HashData(input))}; source ZIP exact, edited tile retained, copied DKM reopened and normal Save retained archive; installed inputs unchanged; no GUI/game");
        }
        finally
        {
            var temporaryRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) + Path.DirectorySeparatorChar;
            Check(Path.GetFullPath(parent).StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase), "Temporary cleanup escaped its root.");
            Directory.Delete(parent, recursive: true);
        }
    }
}
