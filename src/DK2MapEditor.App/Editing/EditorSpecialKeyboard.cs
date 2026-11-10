using DK2MapEditor.Core.Editing;
namespace DK2MapEditor.App.Editing;
public enum BrushQuickPage{Objective,CreatureOptions,HeroOptions}
public enum EditorScrollStep{PreviousLine,NextLine,PreviousPage,NextPage,Start,End}
public sealed record EditorSpecialCommand(BrushQuickPage? Page=null,bool Vertical=false,EditorScrollStep? Scroll=null);
public static class EditorSpecialKeyboard
{
    public static EditorSpecialCommand? Resolve(string key,bool control,bool shift,bool alt=false)
    {
        if(alt||control&&shift)return null;
        if(key=="Insert")return new(shift?BrushQuickPage.CreatureOptions:control?BrushQuickPage.HeroOptions:BrushQuickPage.Objective);
        var previous=control?EditorScrollStep.Start:shift?EditorScrollStep.PreviousPage:EditorScrollStep.PreviousLine;
        var next=control?EditorScrollStep.End:shift?EditorScrollStep.NextPage:EditorScrollStep.NextLine;
        return key switch{
            "Up"=>new(Vertical:true,Scroll:previous),"Down"=>new(Vertical:true,Scroll:next),
            "Left"=>new(Scroll:previous),"Right"=>new(Scroll:next),
            "Prior" or "PageUp"=>new(Vertical:true,Scroll:EditorScrollStep.PreviousPage),
            "Next" or "PageDown"=>new(Vertical:true,Scroll:EditorScrollStep.NextPage),
            "Home"=>new(Vertical:control,Scroll:EditorScrollStep.Start),"End"=>new(Vertical:control,Scroll:EditorScrollStep.End),_=>null};
    }
    public static int Page(double viewport,double cell)
    {
        if(!double.IsFinite(viewport)||viewport<0||!double.IsFinite(cell)||cell<=0||viewport/cell>int.MaxValue-1)throw new ArgumentOutOfRangeException(nameof(viewport));
        return (int)(viewport/cell)+1;
    }
    public static double Padding(int mapLength,double viewport,double cell)=>!double.IsFinite(viewport)||viewport<=0?0:mapLength<Page(viewport,cell)?0:viewport-(Page(viewport,cell)-1)*cell;
    public static double? Target(int mapLength,double viewport,double cell,double offset,EditorScrollStep step)
    {
        if(!Enum.IsDefined(step)||mapLength<1||!double.IsFinite(offset)||offset<0)throw new ArgumentOutOfRangeException(nameof(offset));
        var page=Page(viewport,cell);if(viewport==0||mapLength<page)return null;
        var start=Math.Floor(offset/cell);var end=mapLength+1d-page;
        var target=step switch{EditorScrollStep.PreviousLine=>start-1,EditorScrollStep.NextLine=>start+1,
            EditorScrollStep.PreviousPage=>start-page,EditorScrollStep.NextPage=>start+page,EditorScrollStep.Start=>0,_=>end};
        return Math.Clamp(target,0,end)*cell;
    }
    public static ObjectBatchTemplate CloseQuick(ObjectBatchTemplate original,BrushQuickPage page,uint value,uint area=0,uint enemy=0)
    {
        if(original.Kind!=PlacedThingKind.Creature)throw new ArgumentException("Creature template required.");
        return page switch{
            BrushQuickPage.CreatureOptions=>original with{CreatureOptions=value&0xF9},
            BrushQuickPage.HeroOptions=>original with{HeroOptions=value&0x0F},
            BrushQuickPage.Objective=>original with{Objective=value,Area=value==23?area:0,Enemy=enemy},
            _=>throw new ArgumentOutOfRangeException(nameof(page))};
    }
}
