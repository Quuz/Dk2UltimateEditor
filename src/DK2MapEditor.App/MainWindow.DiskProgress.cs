using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App;

public partial class MainWindow
{
    private FileProgressScope? fileProgress;
    private bool startingFileProgress;
    private readonly List<EditorFileProgress> fileProgressRecords=[];
    public IReadOnlyList<EditorFileProgress> LastFileProgress=>fileProgressRecords.AsReadOnly();
    public Action<DiskProgressWindow>? FileProgressPresenter{get;set;}
    private FileProgressScope? BeginFileProgress(bool saving,string path)
    {
        // Programmatic nested loads keep their existing context invalidation
        // behavior. They do not replace an outer operation's indicator.
        if(fileProgress is not null||startingFileProgress)return null;
        startingFileProgress=true;
        try{fileProgressRecords.Clear();var scope=new FileProgressScope(this,saving,path);fileProgress=scope;return scope;}
        finally{startingFileProgress=false;}
    }
    private sealed class FileProgressScope:IDisposable
    {
        private readonly MainWindow owner;
        private readonly string target;
        private readonly bool saving;
        private readonly DiskProgressWindow? window;
        private readonly Window[] disabled=[];
        private long rendered;
        private EditorFilePhase? lastRenderedPhase;
        private bool done,disposed;
        public FileProgressScope(MainWindow owner,bool saving,string path)
        {
            this.owner=owner;this.saving=saving;target=path;
            try
            {
                if(owner.FileProgressPresenter is {} present){window=new(saving,path);present(window);}
                else if(owner.IsVisible)
                {
                    window=new(saving,path){Owner=owner};window.Show();
                    disabled=Application.Current.Windows.Cast<Window>().Where(w=>w!=window&&w.IsVisible&&w.IsEnabled).ToArray();
                    foreach(var other in disabled)other.IsEnabled=false;
                }
                Report(new(saving?EditorFilePhase.Prepare:EditorFilePhase.Inspect,path));
            }
            catch{window?.CloseForOperation();foreach(var other in disabled)if(other.IsVisible)other.IsEnabled=true;throw;}
        }
        public void Report(EditorFileProgress value)
        {
            if(disposed)return;
            var records=owner.fileProgressRecords;
            // Disk source shows the current phase, not a growing log. Retain
            // phase transitions and coalesce successive byte notifications.
            if(records.Count>0&&records[^1].Phase==value.Phase&&records[^1].Path==value.Path)records[^1]=value;
            else records.Add(value);
            window?.Report(value);
            if(window is not {IsVisible:true})return;
            var now=Stopwatch.GetTimestamp();
            if(value.Phase==lastRenderedPhase&&now-rendered<Stopwatch.Frequency/25)return;
            rendered=now;lastRenderedPhase=value.Phase;window.UpdateLayout();
            // Source disk operations are synchronous too. Paint the indicator
            // without pumping lower-priority mouse/keyboard/timer messages.
            window.Dispatcher.Invoke(()=>{},DispatcherPriority.Render);
        }
        public void ReportLoad(EditorFileProgress value)=>Report(value.Phase==EditorFilePhase.Done?value with{Phase=EditorFilePhase.Prepare}:value);
        public Func<string,Stream> Output(Func<string,Stream>? factory)=>path=>new DiskProgressStream(
            factory is null?new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None):factory(path),target,Report);
        public void Complete(){Report(new(EditorFilePhase.Done,target));done=true;}
        public void Dispose()
        {
            if(disposed)return;disposed=true;if(ReferenceEquals(owner.fileProgress,this))owner.fileProgress=null;
            try
            {
                if(window is not null)
                {
                    if(done&&saving&&window.IsVisible)
                    {
                        var timer=new DispatcherTimer(DispatcherPriority.Background,window.Dispatcher){Interval=TimeSpan.FromMilliseconds(250)};
                        timer.Tick+=(_,_)=>{timer.Stop();window.CloseForOperation();};timer.Start();
                    }
                    else window.CloseForOperation();
                }
            }
            finally{foreach(var other in disabled)if(other.IsVisible)other.IsEnabled=true;}
        }
    }
}
