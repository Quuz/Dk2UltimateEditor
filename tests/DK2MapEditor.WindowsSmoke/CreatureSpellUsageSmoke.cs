using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    // Prepared only; build does not execute this window/bitmap suite.
    private static void CheckCreatureSpellUsage(NativeMapDocument document,string language)
    {
        var spells=document.GlobalDefinitionRows().Where(r=>r.Family=="CrSpells").ToArray();var selected=spells[3];var id=selected.ID.ToString();
        document.SetGlobalDefinitionValues(document.GlobalDefinitionRows().Where(r=>r.Family=="Creatures").SelectMany(r=>new[]{"Shot1ID","Shot2ID","Shot3ID"}.Select(f=>new GlobalDefinitionChange("Creatures",r.Index,f,"0"))));
        document.SetGlobalDefinitionValues([new("Creatures",0,"Shot1ID",id),new("Creatures",0,"Shot2ID",id),new("Creatures",20,"Shot2ID",id),new("Creatures",44,"Shot3ID",id)]);
        var initial=document.Files.ToDictionary(f=>f,f=>f.Data.ToArray());var count=document.EditorChangeCount;var history=document.GetHistory().Undo.Count;
        void Exact()=>Expect(initial.All(p=>p.Key.Data.Span.SequenceEqual(p.Value))&&document.EditorChangeCount==count&&document.GetHistory().Undo.Count==history,"Read-only spell usage UI changed live map/history.");
        var w=new CreatureSpellWindow(document,selected,()=>true,()=>true);ShowManagerCallerWindow(w);
        ListBox List()=>(ListBox)w.FindName("CreatureSpellPropertyUsers");
        try
        {
            Expect(List().ItemTemplate is not null&&List().Items.Cast<object>().Select(row=>Get<int>(row,"Index")).SequenceEqual(new[]{0,20,44}),"Source used-by list absent, duplicated or reordered.");
            foreach(var item in List().Items.Cast<object>())Expect(Get<BitmapSource?>(item,"Icon") is {PixelWidth:32,PixelHeight:32}&&Get<string>(item,"Label").Length>0,"Used-by row lacks source creature bitmap/name.");
            List().SelectedIndex=1;var label=Get<string>(List().SelectedItem!,"Label");LocalizationService.SetLanguage(language=="en"?"ru":"en");
            Expect(Get<int>(List().SelectedItem!,"Index")==20&&Get<string>(List().SelectedItem!,"Label")!=label,"Usage translation lost selection or kept old language.");
            LocalizationService.SetLanguage(language);Exact();Expect(w.ApplySpell(),"Read-only list broke unchanged spell Apply.");Exact();
        }
        finally{w.Close();}Exact();
        var parent=new CreatureGlobalsWindow(document,()=>true,()=>true);ShowManagerCallerWindow(parent);
        try
        {
            Expect(parent.Select("CrSpells",selected.Index)&&parent.ActiveRowEditor is CreatureSpellWindow,"Shared used-by page unavailable.");
            var list=(ListBox)parent.ActiveRowEditor!.FindName("CreatureSpellPropertyUsers");Expect(list.Items.Count==3,"Shared page lost usage catalogue.");
            Expect(parent.Select("Attacks",0)&&parent.Select("CrSpells",selected.Index),"Usage page blocked ordinary private navigation.");Exact();
        }
        finally{parent.Close();}Exact();
    }
}
