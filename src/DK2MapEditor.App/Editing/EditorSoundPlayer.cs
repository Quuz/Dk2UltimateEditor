using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace DK2MapEditor.App.Editing;

/// <summary>One reusable player per original sample: restart that sample, retain other simultaneous samples.</summary>
internal sealed class EditorSoundPlayer : IDisposable
{
    private readonly Dictionary<EditorSoundCue,MediaPlayer> players=[];
    private bool disposed;
    public Exception? LastError {get;private set;}
    public void Play(EditorSoundCue cue,double volume)
    {
        if(disposed)return;
        try
        {
            if(!players.TryGetValue(cue,out var player))
            {
                var path=Path.Combine(AppContext.BaseDirectory,"Sounds",EditorSoundRules.FileName(cue));
                if(!File.Exists(path))throw new FileNotFoundException("Missing original editor sound.",path);
                player=new MediaPlayer();
                player.MediaFailed+=(_,args)=>LastError=args.ErrorException;
                player.Open(new Uri(path,UriKind.Absolute));players.Add(cue,player);
            }
            player.Volume=volume;player.Stop();player.Position=TimeSpan.Zero;player.Play();LastError=null;
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or COMException or NotSupportedException)
        {LastError=error;}
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        foreach(var player in players.Values)
            try{player.Close();}catch(Exception error)when(error is InvalidOperationException or COMException){LastError=error;}
        players.Clear();
    }
}
