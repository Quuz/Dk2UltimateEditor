using System.Windows;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private MapSpeechTextWindow? speechTextWindow;
    private long speechTextContext;
    private bool speechTextOwnerClosed;
    public GameResourceSettings GameResources { get; private set; } = new();
    public Action<MapSpeechTextWindow>? SpeechTextPresenter { get; set; }
    public void ConfigureGameResources(GameResourceSettings? settings)
    { GameResources=settings??new();CloseSpeechTextWindow();CloseScenarioParameterWindow();CloseScenarioCreationWindow(); }
    private void CloseSpeechTextWindow(){speechTextContext++;speechTextWindow?.Close();speechTextWindow=null;}
    public void EndSpeechTextLifetime(){speechTextOwnerClosed=true;CloseSpeechTextWindow();SpeechTextButton.IsEnabled=false;}
    public MapSpeechTextWindow? CreateSpeechTextWindow()
    {
        if(speechTextOwnerClosed || document is null || !CommitFields())return null;
        var source=document;var context=speechTextContext;
        return new(source,()=>!speechTextOwnerClosed && context==speechTextContext && ReferenceEquals(source,document),CommitFields,GameResources,settings=>GameResources=settings);
    }
    private void SpeechText_Click(object sender,RoutedEventArgs args)
    {
        if(speechTextWindow is not null){speechTextWindow.Activate();return;}
        var window=CreateSpeechTextWindow();if(window is null)return;speechTextWindow=window;
        window.Closed+=(_,_)=>{if(ReferenceEquals(speechTextWindow,window))speechTextWindow=null;};
        if(SpeechTextPresenter is {} present)present(window);else{window.Owner=Window.GetWindow(this);window.Show();}
    }
}
