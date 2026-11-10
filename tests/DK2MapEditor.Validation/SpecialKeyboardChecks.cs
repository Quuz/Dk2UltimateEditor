using DK2MapEditor.App.Editing;
using DK2MapEditor.Core.Editing;
internal static class SpecialKeyboardChecks
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    public static void Run()
    {
        Check(EditorSpecialKeyboard.Resolve("Insert",false,false)?.Page==BrushQuickPage.Objective&&EditorSpecialKeyboard.Resolve("Insert",false,true)?.Page==BrushQuickPage.CreatureOptions
            &&EditorSpecialKeyboard.Resolve("Insert",true,false)?.Page==BrushQuickPage.HeroOptions,"Wrong three source Insert routes.");
        foreach(var key in new[]{"Up","Down","Left","Right","PageUp","PageDown","Home","End","Insert"})
        {Check(EditorSpecialKeyboard.Resolve(key,true,true)==null,"Ctrl+Shift special key accepted.");foreach(var ctrl in new[]{false,true})foreach(var shift in new[]{false,true})Check(EditorSpecialKeyboard.Resolve(key,ctrl,shift,true)==null,"Alt special key captured.");}
        foreach(var (key,vertical,back) in new[]{("Up",true,true),("Down",true,false),("Left",false,true),("Right",false,false)})
            foreach(var (ctrl,shift,step) in new[]{(false,false,back?EditorScrollStep.PreviousLine:EditorScrollStep.NextLine),(false,true,back?EditorScrollStep.PreviousPage:EditorScrollStep.NextPage),(true,false,back?EditorScrollStep.Start:EditorScrollStep.End)})
            {var route=EditorSpecialKeyboard.Resolve(key,ctrl,shift)!;Check(route.Vertical==vertical&&route.Scroll==step,"Arrow axis/modifier mismatch.");}
        foreach(var ctrl in new[]{false,true})foreach(var shift in new[]{false,true})if(!ctrl||!shift)
        {
            Check(EditorSpecialKeyboard.Resolve("Home",ctrl,shift) is {Scroll:EditorScrollStep.Start} h&&h.Vertical==ctrl,"Home axis mismatch.");
            Check(EditorSpecialKeyboard.Resolve("End",ctrl,shift) is {Scroll:EditorScrollStep.End} end&&end.Vertical==ctrl,"End axis mismatch.");
            Check(EditorSpecialKeyboard.Resolve("Prior",ctrl,shift) is {Vertical:true,Scroll:EditorScrollStep.PreviousPage}&&EditorSpecialKeyboard.Resolve("Next",ctrl,shift) is {Vertical:true,Scroll:EditorScrollStep.NextPage},"Page keys lost source semantics.");
        }
        Check(EditorSpecialKeyboard.Page(700,64)==11&&EditorSpecialKeyboard.Padding(20,700,64)==60,"Partial-cell source page/padding is wrong.");
        Check(EditorSpecialKeyboard.Target(20,700,64,128,EditorScrollStep.NextLine)==192&&EditorSpecialKeyboard.Target(20,700,64,128,EditorScrollStep.NextPage)==640
            &&EditorSpecialKeyboard.Target(20,700,64,400,EditorScrollStep.PreviousPage)==0&&EditorSpecialKeyboard.Target(20,700,64,128,EditorScrollStep.End)==640,"Literal source scroll geometry is wrong.");
        foreach(var cell in new[]{8d,17,63,64,256})foreach(var viewport in new[]{0d,8,31,64,128,700,1024})foreach(var length in new[]{1,12,128})
        {
            var page=(int)(viewport/cell)+1;var pad=EditorSpecialKeyboard.Padding(length,viewport,cell);
            if(viewport==0||length<page){Check(pad==0&&EditorSpecialKeyboard.Target(length,viewport,cell,0,EditorScrollStep.End)==null,"Fitting/unmeasured map scrolled.");continue;}
            var expectedEnd=(length+1-page)*cell;
            Check(Math.Abs(length*cell+pad-viewport-expectedEnd)<1e-8,"WPF extent padding does not admit the source cell-aligned last origin.");
            foreach(var offset in new[]{0d,cell*2.25,expectedEnd})foreach(var step in Enum.GetValues<EditorScrollStep>())
            {
                var target=EditorSpecialKeyboard.Target(length,viewport,cell,offset,step)!.Value;
                Check(target>=0&&target<=expectedEnd&&target%cell==0,"Source scroll crossed map edge or used fractional tile origin.");
            }
        }
        var original=ObjectBatchTemplate.Default(PlacedThingKind.Creature) with{HeroOptions=0xFFFF,CreatureOptions=0xABCD,Area=42,Enemy=7};
        for(uint value=0;value<256;value++)
        {
            var creature=EditorSpecialKeyboard.CloseQuick(original,BrushQuickPage.CreatureOptions,value|0xFF000000);
            var hero=EditorSpecialKeyboard.CloseQuick(original,BrushQuickPage.HeroOptions,value|0xFF000000);
            Check(creature==original with{CreatureOptions=value&0xF9}&&hero==original with{HeroOptions=value&15},"Quick close changed unrelated fields or retained source-discarded bits.");
        }
        foreach(var id in new uint[]{11,12,13,14,17,18,19,22,23,27})
            Check(EditorSpecialKeyboard.CloseQuick(original,BrushQuickPage.Objective,id,27,uint.MaxValue)==original with{Objective=id,Area=id==23?27u:0,Enemy=uint.MaxValue},"Objective close lost inactive-area clearing or raw no-selection enemy.");
    }
}
