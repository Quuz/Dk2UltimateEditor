namespace DK2MapEditor.Core.Editing;

/// <summary>Source RectEx gate-list icon, from existing site facts rather than
/// whether the currently selected generation options happen to add work.</summary>
public static class GateGenerationImageResources
{
    public static int ForSite(HeroGateSite site)=>ForSite(site.Type,site.CannotDestroy,site.Gate is not null,site.Area is not null,site.HasEvents);
    public static int ForSite(int type,bool cannotDestroy,bool hasGate,bool hasArea,bool hasEvents)
    {
        if(type==3&&!cannotDestroy&&(!hasGate||!hasArea||!hasEvents))return 87;
        if(type is 6 or 7&&!hasGate)return 88;
        return 89;
    }
}
