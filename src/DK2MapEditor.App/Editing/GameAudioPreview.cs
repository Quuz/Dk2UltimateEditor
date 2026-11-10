using System.Diagnostics;
using System.IO;

namespace DK2MapEditor.App.Editing;

public sealed record GameResourceSettings(string? Root = null, string? Player = null);
public sealed record AudioPreviewLaunch(string Mp2Path, string? PlayerPath);

/// <summary>Only files created by this session are candidates for cleanup.</summary>
public sealed class GameAudioPreview : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "DK2AudioPreview-" + Guid.NewGuid().ToString("N"));
    private readonly HashSet<string> owned = new(StringComparer.OrdinalIgnoreCase);
    private bool disposed;
    public string Prepare(byte[] payload)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Directory.CreateDirectory(directory);
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new IOException("Preview directory was replaced.");
        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".mp2");
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { owned.Add(path); stream.Write(payload); }
        return path;
    }
    public static void Launch(AudioPreviewLaunch request)
    {
        if (string.IsNullOrWhiteSpace(request.PlayerPath)) Process.Start(new ProcessStartInfo(request.Mp2Path) { UseShellExecute = true });
        else
        {
            var start = new ProcessStartInfo(request.PlayerPath) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
            start.ArgumentList.Add(request.Mp2Path); Process.Start(start);
        }
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        try { if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) return; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return; }
        foreach (var path in owned)
            try { File.Delete(path); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* An external player may still hold its own preview file. */ }
        try { Directory.Delete(directory, false); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* Never recursively remove unknown or locked files. */ }
    }
}
