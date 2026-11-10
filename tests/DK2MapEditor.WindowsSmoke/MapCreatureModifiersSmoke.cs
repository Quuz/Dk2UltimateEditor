using System.Windows;
using System.Windows.Controls;
using DK2MapEditor.App.Editing;
using DK2MapEditor.App.Localization;
using DK2MapEditor.Core.Editing;

internal static partial class Program
{
    private static void CheckMapCreatureModifiers(bool native,string language)
    {
        var source=NativeMapDocument.CreateNewMap(new("ModifierUI","ModifierUI",12,12,NativeFormat:native));
        var ordinary=MapRules.Elements(MapRuleKind.CreatureModifier).Where(row=>!MapRules.IsElite(new(MapRuleKind.CreatureModifier,0,row.ID,1))).Take(2).Select(row=>row.ID).ToArray();
        source.SetMapRules([new(new(MapRuleKind.CreatureModifier,0,ordinary[0],1),10),new(new(MapRuleKind.CreatureModifier,0,ordinary[1],1),30)]);
        var before=source.Files.ToDictionary(file=>file,file=>file.Data.ToArray());var history=source.GetHistory().Undo.Count;var counter=source.EditorChangeCount;
        void Exact()=>Expect(before.All(pair=>pair.Key.Data.Span.SequenceEqual(pair.Value))&&source.GetHistory().Undo.Count==history&&source.EditorChangeCount==counter,"Modifier child/parent Cancel changed live map.");
        MapCreatureModifiersWindow? child=null;
        MapPropertiesWindow Open()
        {
            var parent=new MapPropertiesWindow(source,()=>true,()=>true,new());parent.CreatureModifiersPresenter=window=>child=window;
            ((Button)parent.FindName("MapPropertiesCreatureModifiers")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Expect(child is not null,"Actual map-property modifier command missing.");return parent;
        }
        void SelectBoth()
        {
            var list=(ListView)child!.FindName("CreatureModifiersList");list.SelectedItems.Clear();
            foreach(var row in list.Items.Cast<object>().Where(row=>ordinary.Contains(Get<uint>(row,"ID"))).ToArray())list.SelectedItems.Add(row);
        }
        TextBox Input(int number)=>(TextBox)child!.FindName("CreatureModifierParameter"+number);
        var parent=Open();SelectBoth();Expect(Input(1).Text=="20"&&Input(2).Text=="0","Actual four-field source nonzero mean not bound.");
        Input(2).Text="55";Exact();Expect(!parent.ApplyProperties(),"Parent silently ignored pending private modifier inputs.");
        LocalizationService.SetLanguage(language=="ru"?"en":"ru");Expect(Input(2).Text=="55"&&child!.Title==LocalizationService.Get("CreatureModifiers.Title"),"Translation lost private modifier value/title.");LocalizationService.SetLanguage(language);
        child!.Close();Exact();parent.Close();Exact();
        parent=Open();SelectBoth();Input(2).Text="55";Expect(child!.ApplyModifiers(),"Child modifiers not accepted.");Exact();
        Expect(parent.WorkingDocument.MapRuleCells().Where(cell=>cell.Key.Kind==MapRuleKind.CreatureModifier&&cell.Key.Parameter==2&&ordinary.Contains(cell.Key.Element)).All(cell=>cell.Value==55),"Child did not publish into private parent only.");
        parent.Close();Exact();
        parent=Open();SelectBoth();Input(2).Text="65536";Expect(Input(2).Text=="65535","Source modifier input not clamped.");Expect(child!.ApplyModifiers()&&parent.ApplyProperties(),"Composed parent modifiers not accepted.");
        Expect(source.EditorChangeCount==unchecked(counter+1)&&source.GetHistory().Undo.Count==history+1,"Parent modifiers need one original acceptance and one Undo.");source.Undo();Exact();
        parent=Open();SelectBoth();Input(3).Text="4";source.Paint([new(4,4)],new(3,2,1));Expect(!child!.ApplyModifiers(),"Stale parent/child retained modifier authority.");parent.Close();source.Undo();Exact();
        parent=Open();SelectBoth();((TextBox)parent.FindName("MapPropertiesWidth")).Text="121";((TextBox)parent.FindName("MapPropertiesHeight")).Text="121";
        parent.LargeMapPrompt=_=>{Input(2).Text="77";return true;};
        Expect(!parent.ApplyProperties()&&child!.HasPendingInputs&&!child.ApplyModifiers(),"Input during parent consent was silently dropped or reused obsolete approval.");Exact();
        Input(2).Text="78";Expect(child!.ApplyModifiers(),"Fresh private input after rejected consent could not be kept.");parent.Close();Exact();
        Console.WriteLine($"PASS {language} {(native?"native":"project")} actual grouped four modifier fields, means, EN/RU, private/parent Cancel, pending/stale guards and one parent Undo (compiled acceptance)");
    }
}
