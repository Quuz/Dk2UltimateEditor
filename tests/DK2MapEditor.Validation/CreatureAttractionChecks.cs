using System.Text.Json;
using DK2MapEditor.Core.Editing;

internal static class CreatureAttractionChecks
{
    private static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    private static Dictionary<NativeFile,byte[]> Snapshot(NativeMapDocument document)=>document.Files.ToDictionary(file=>file,file=>file.Data.ToArray());
    private static void Exact(NativeMapDocument document,Dictionary<NativeFile,byte[]> bytes)=>Expect(document.Files.Count==bytes.Count&&bytes.All(item=>item.Key.Data.Span.SequenceEqual(item.Value)),"Attraction callback changed document/canceled/undo bytes.");
    public static void Run(NativeMapDocument document,JsonElement oracle,string root)
    {
        var source=File.ReadAllText(Path.Combine(root,"Source/Sources/Properties/GlobalVariables/Properties_Global_Creatures.c"));
        var begin=source.IndexOf("void Properties_GlobalSetAttractions(",StringComparison.Ordinal);
        var end=source.IndexOf("void Properties_GlobalModifyValue(",begin,StringComparison.Ordinal);
        var callback=source[begin..end];
        var compact=string.Concat(callback.Where(character=>!char.IsWhiteSpace(character)));
        var amount="SetDlgItemInt(hDlg,129,Context->Variables.Creatures[Context->CurrentCreature].Attract2Data,FALSE);";
        Expect(compact.Split(amount,StringSplitOptions.None).Length==4,"Original second attraction does not reload private quantity for room/creature/amount forms.");
        Expect(compact.Contains("if(!GARooms[i].Id)SendDlgItemMessage(hDlg,125,CB_SETCURSEL,(WPARAM)0,(LPARAM)0);")
            &&compact.Contains("if(!GARooms[i].Id)SendDlgItemMessage(hDlg,128,CB_SETCURSEL,(WPARAM)0,(LPARAM)0);")
            &&compact.Contains("if(!GCreatures[i].Id)SendDlgItemMessage(hDlg,128,CB_SETCURSEL,(WPARAM)0,(LPARAM)0);")
            &&!System.Text.RegularExpressions.Regex.IsMatch(compact,@"\.Attract[12]ID=(?!=)"),
            "Original attraction fallback source contract changed.");
        var firstRoom=oracle.GetProperty("Catalogues").GetProperty("GARooms")[0].GetProperty("ID").GetUInt32().ToString();
        var firstCreature=oracle.GetProperty("Catalogues").GetProperty("GCreatures")[0].GetProperty("ID").GetUInt32().ToString();
        var before=Snapshot(document);var history=document.GetHistory().Undo.Count;var count=document.EditorChangeCount;
        using(var canceled=document.CreateCreatureGlobalsSession())
        {
            var working=canceled.WorkingDocument;
            working.SetGlobalDefinitionValues([new("Creatures",0,"Attract1Type","1"),new("Creatures",0,"Attract1ID","255"),
                new("Creatures",0,"Attract2Type","1"),new("Creatures",0,"Attract2ID","255"),new("Creatures",0,"Attract2Data","37")]);
            var row=working.GlobalDefinitionRows().Single(item=>item.Family=="Creatures"&&item.Index==0);
            var baseline=working.CreateCreaturePageDraft(row,"Parameters");var privateBefore=Snapshot(working);var privateHistory=working.GetHistory().Undo.Count;
            var inputs=CreaturePageControls.Fields("Parameters").Select(field=>new CreaturePageInput(field,field=="Attract2Data"?"123":baseline.Display(field))).ToArray();
            foreach(var firstType in new[]{"0","1"})foreach(var secondType in new[]{"0","1","2","3","4"})
            {
                var first=CreaturePageControls.ChangeAttractionType(baseline,inputs,"Attract1Type",firstType);
                Expect(first.Single(input=>input.Field=="Attract2Data").Value=="123","First attraction erased unrelated unsaved quantity.");
                var second=CreaturePageControls.ChangeAttractionType(baseline,first,"Attract2Type",secondType);
                Expect(second.Single(input=>input.Field=="Attract2Data").Value=="37","Second attraction retained unsaved quantity from another form.");
                Expect(second.Single(input=>input.Field=="Attract1ID").Value=="255"&&second.Single(input=>input.Field=="Attract2ID").Value=="255","Displayed fallback repaired private stored IDs.");
                Expect(CreaturePageControls.AttractionSelection("Attract1ID","255",firstType)==(firstType=="0"?null:firstRoom),"First attraction fallback differs from source room list.");
                Expect(CreaturePageControls.AttractionSelection("Attract2ID","255",secondType)==(secondType=="1"?firstRoom:secondType=="4"?firstCreature:null),"Second attraction fallback/inactive list differs from source.");
                Expect(CreaturePageControls.AttractionAmountText("37",secondType)==(secondType=="0"?"":"37"),"None form displays a retained number instead of empty source text.");
                var prepared=working.UpdateCreaturePageDraft(baseline,second);
                Expect(prepared.Raw("Attract1ID")== (firstType=="0"?"0":"255")
                    &&prepared.Raw("Attract2ID")== (secondType is "0" or "2" or "3"?"0":"255")
                    &&prepared.Raw("Attract2Data")== (secondType=="0"?"0":"37"),"Explicit source Save cleanup changed fallback/pending quantity semantics.");
                Exact(working,privateBefore);Exact(document,before);
            }
            Expect(working.GetHistory().Undo.Count==privateHistory&&document.GetHistory().Undo.Count==history&&document.EditorChangeCount==count,"Private type/list callbacks consumed history or live counters.");
            var final=CreaturePageControls.ChangeAttractionType(baseline,inputs,"Attract2Type","4");
            working.ApplyCreaturePageDraft(working.UpdateCreaturePageDraft(baseline,final));
            var refreshed=working.CreateCreaturePageDraft(row,"Parameters");
            var again=CreaturePageControls.ChangeAttractionType(refreshed,CreaturePageControls.Fields("Parameters").Select(field=>new CreaturePageInput(field,field=="Attract2Data"?"999":refreshed.Display(field))),"Attract2Type","2");
            Expect(again.Single(input=>input.Field=="Attract2Data").Value=="37","After private Apply type callback used a stale quantity baseline.");
            Exact(document,before);
            document.ApplyCreatureGlobals(document.PrepareCreatureGlobals(canceled),true);
            Expect(document.GetHistory().Undo.Count==history+1&&document.EditorChangeCount==unchecked(count+1),"Common attraction Apply split history/counters.");
            var applied=document.CreateCreaturePageDraft(document.GlobalDefinitionRows().Single(item=>item.Family=="Creatures"&&item.Index==0),"Parameters");
            Expect(applied.Raw("Attract2Type")=="4"&&applied.Raw("Attract2ID")=="255"&&applied.Raw("Attract2Data")=="37","Accepted fallback/type callback lost private values.");
            document.Undo();Exact(document,before);
        }
        using(var canceled=document.CreateCreatureGlobalsSession())
        {
            var working=canceled.WorkingDocument;var row=working.GlobalDefinitionRows().First(item=>item.Family=="Creatures");
            var draft=working.CreateCreaturePageDraft(row,"Parameters");
            var input=CreaturePageControls.Fields("Parameters").Select(field=>new CreaturePageInput(field,field=="Attract2Data"?"not a number":draft.Display(field)));
            var changed=CreaturePageControls.ChangeAttractionType(draft,input,"Attract2Type","3");
            Expect(changed.Single(item=>item.Field=="Attract2Data").Value==draft.Display("Attract2Data"),"Source list rebuild validated stale discarded quantity text.");
        }
        Exact(document,before);Expect(document.GetHistory().Undo.Count==history&&document.EditorChangeCount==count,"Canceled attraction form changed live history/count.");
        Expect(CreaturePageControls.AttractionSelection("Attract2ID","253","254")=="253"&&CreaturePageControls.AttractionAmountText("100000","254")=="100000","Unknown import layout was guessed/repaired.");
        Console.WriteLine($"ATTRACTION CALLBACK DETAIL native={!document.IsLegacyProject} source-reset=10 source-first-fallback blank-none retained-IDs private-apply common-one-undo cancel unknown-layout");
    }
}
