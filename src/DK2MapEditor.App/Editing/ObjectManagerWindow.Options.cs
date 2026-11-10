using DK2MapEditor.Core.Editing;

namespace DK2MapEditor.App.Editing;

public sealed partial class ObjectManagerWindow
{
    private bool TryOptionsKind(bool modifying,out PlacedThingKind kind)
    {
        var familyID=family.SelectedValue as int? ?? 0;
        kind=familyID switch{1=>session.KindFor(PlacedThingKind.Door),2=>session.KindFor(PlacedThingKind.Object),_=>PlacedThingKind.Creature};
        if(!modifying||familyID!=3)return true;
        var selected=rows.Where(row=>table.SelectedItems.Contains(row)).ToArray();
        if(selected.Length==0||selected.Select(row=>ObjectModificationRules.Family(row.Entry.Thing.Kind)).Distinct().Count()!=1)return false;
        kind=selected[0].Entry.Thing.Kind;return true;
    }
    public ObjectManagerOptionsWindow? CreateOptionsWindow(bool modifying)
    {
        if(closed||navigating||managerFlowBusy||requestPending||!ownerCurrent()||!canApply()||closed||!ownerCurrent()||!TryOptionsKind(modifying,out var kind))return null;
        optionsWindow?.Cancel();optionsWindow=null;additionWindow?.Cancel();additionWindow=null;modificationWindow?.Cancel();modificationWindow=null;
        return NewOptionsWindow(kind,modifying,()=>true);
    }
    private ObjectManagerOptionsWindow NewOptionsWindow(PlacedThingKind kind,bool modifying,Func<bool> extraCurrent)
    {
        var revision=document.EditRevision;
        var window=new ObjectManagerOptionsWindow(session,kind,modifying,()=>!closed&&ownerCurrent()&&document.EditRevision==revision&&extraCurrent());
        optionsWindow=window;
        window.Closed+=(_,_)=>{if(ReferenceEquals(optionsWindow,window)){optionsWindow=null;RefreshFlowPreferences();UpdateApply();}};
        UpdateApply();return window;
    }
    private void ShowOptions(bool modifying)
    {
        var window=CreateOptionsWindow(modifying);if(window is null)return;
        window.Owner=this;window.ShowDialog();
    }
}
