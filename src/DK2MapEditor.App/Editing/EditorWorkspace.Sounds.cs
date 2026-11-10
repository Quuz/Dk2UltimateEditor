using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public partial class EditorWorkspace
{
    private EditorSoundPlayer? editorSoundPlayer;
    private bool suppressEditorSounds;
    public EditorSoundCategories SoundCategories {get;set;}=EditorSoundCategories.All;
    public int SoundVolume {get;set;}=EditorSoundRules.DefaultVolume;
    public Exception? LastEditorSoundError=>editorSoundPlayer?.LastError;
    /// <summary>Optional acceptance presenter. When set, does not create or use an audio device.</summary>
    public Action<EditorSoundCue,double>? EditorSoundPresenter {get;set;}
    private void InitializeEditorSounds()
    {
        Canvas.SoundRequested+=PlayEditorSound;
        Unloaded+=(_,_)=>{editorSoundPlayer?.Dispose();editorSoundPlayer=null;};
    }
    private void PlayEditorSound(EditorSoundRequest request)
    {
        if(!IsLoaded||suppressEditorSounds)return;
        var cue=EditorSoundRules.Choose(request,SoundCategories);if(cue is null)return;
        var volume=EditorSoundRules.LinearVolume(SoundVolume);
        if(EditorSoundPresenter is {} present)present(cue.Value,volume);
        else (editorSoundPlayer??=new()).Play(cue.Value,volume);
    }
    private void PlayObjectPaintingSound(ObjectBatchTemplate template)
    {
        var mode=template.Kind switch
        {
            Core.Editing.PlacedThingKind.Creature=>EditorSoundPaintMode.Creature,
            Core.Editing.PlacedThingKind.Object=>EditorSoundPaintMode.Object,
            Core.Editing.PlacedThingKind.Door=>EditorSoundPaintMode.Door,
            Core.Editing.PlacedThingKind.Trap=>EditorSoundPaintMode.Trap,
            _=>EditorSoundPaintMode.SpecialItem
        };
        PlayEditorSound(new(EditorSoundAction.Paint,mode,ObjectID:template.TypeID));
    }
}
